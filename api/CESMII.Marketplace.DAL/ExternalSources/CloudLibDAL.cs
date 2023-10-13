namespace CESMII.Marketplace.DAL.ExternalSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Configuration;
    using NLog;

    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.Common.Models;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.DAL.ExternalSources.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using CESMII.Common.CloudLibClient;

    using Opc.Ua.Cloud.Library.Client;

    /// <summary>
    /// Most lookup data is contained in this single entity and differntiated by a lookup type. 
    /// </summary>
    public class CloudLibDAL : ExternalBaseDAL<ExternalAbstractEntity, MarketplaceItemModel>, IExternalDAL<MarketplaceItemModel>
    {
        private ICloudLibWrapper _cloudLib;
        protected IMongoRepository<ProfileItem> _repoExternalItem;
        protected IMongoRepository<MarketplaceItem> _repoMarketplace;
        protected List<LookupItemModel> _lookupItemsRelatedType;
        protected List<ImageItemModel> _images;

        public CloudLibDAL(ExternalSourceModel config,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IHttpApiFactory httpApiFactory,
            IMongoRepository<ImageItem> repoImages,
            IConfiguration configuration,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ProfileItem> repoExternalItem
            ) : base(dalExternalSource, config, httpApiFactory, repoImages)
        {
            this.Init(configuration, dalLookup, repoMarketplace, repoExternalItem);
        }

        public CloudLibDAL(
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IHttpApiFactory httpApiFactory,
            IMongoRepository<ImageItem> repoImages,
            IConfiguration configuration,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ProfileItem> repoExternalItem
            ) : base(dalExternalSource, "cloudlib", httpApiFactory, repoImages)
        {
            this.Init(configuration, dalLookup, repoMarketplace, repoExternalItem);
        }

        protected void Init(
            IConfiguration configuration,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ProfileItem> repoExternalItem
            )
        {
            //instantiate this way rather than through DI so that the factory does not need to know 
            //about specifics of each external data source
            var settings = new UACloudLibClient.Options();
            configuration.GetSection("CloudLibrary").Bind(settings);
            Microsoft.Extensions.Options.IOptions<UACloudLibClient.Options> options =
                Microsoft.Extensions.Options.Options.Create(settings);
            //Microsoft.Extensions.Logging.ILogger<CloudLibWrapper> log = 
            //    Microsoft.Extensions.Logging.LoggerFactoryExtensions.CreateLogger<CloudLibWrapper>();
            _cloudLib = new CloudLibWrapper(options, null); //logger not used in CloudLibWrapper

            //set some default settings specific for this external source. 
            _config.Publisher.DisplayViewAllLink = false;

            //init objects to use for related items
            _repoExternalItem = repoExternalItem;
            _repoMarketplace = repoMarketplace;
            //_dalMarkteplace = dalMarkteplace;

            //get default images
            _images = GetImagesByIdList(new List<string>() {
                _config.DefaultImageBanner?.ID,
                _config.DefaultImageLandscape?.ID,
                _config.DefaultImagePortrait?.ID
            }).Result;

            //get related type lookup items
            _lookupItemsRelatedType = dalLookup.Where(
                x => x.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)
                , null, null, false, false).Data;
        }

        public async Task<MarketplaceItemModel> GetById(string id) {
            var entity = await _cloudLib.DownloadAsync(id);
            //var entity = await _cloudLib.GetById(id);
            if (entity == null) return null;
            //get related items from local db
            var entityLocal = _repoExternalItem.FindByCondition(x => x.ExternalId.Equals(id)).FirstOrDefault();

            return MapToModelNamespace(entity, entityLocal);
        }

        public async Task<ProfileItemExportModel> Export(string id)
        {
            var entity = await _cloudLib.DownloadAsync(id);
            if (entity == null) return null;

            //return the whole thing because we also email some info to request info and use
            //other data in this entity.
            var result = new ProfileItemExportModel()
            {
                Item = MapToModelNamespace(entity, null),
                NodesetXml = entity.Nodeset?.NodesetXml
            };
            return result;
        }

        public async Task<DALResultWithSource<MarketplaceItemModel>> GetAll() {
            //setting to very high to get all...this is called by admin which needs full list right now for dropdown selection
            var result = await this.Where(null, new SearchCursor() { PageIndex = 0, Skip = 0, Take = 999 }); 
            return result;
        }

        public async Task<DALResultWithSource<MarketplaceItemModel>> Where(string query,
            SearchCursor cursor, 
            List<string> ids = null, List<string> processes = null, List<string> verticals = null, 
            List<string> exclude = null)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            //Note - splitting out each word in query into a separate string in the list
            //Per team, don't split out query into multiple keyword items
            //var keywords = string.IsNullOrEmpty(query) ? new List<string>() : query.Split(" ").ToList();
            var keywords = new List<string>();

            //append list of ids, processes, verticals
            if (ids != null)
            {
                keywords = keywords.Union(ids).ToList();
            }
            if (processes != null)
            {
                keywords = keywords.Union(processes).ToList();
            }
            if (verticals != null)
            {
                keywords = keywords.Union(verticals).ToList();
            }

            //inject wildcard to get all if keywords count == 0 or inject query
            if (string.IsNullOrEmpty(query) && keywords.Count == 0) keywords.Add("*");
            if (!string.IsNullOrEmpty(query)) keywords.Add(query);

            var result = new DALResultWithSource<MarketplaceItemModel>() { 
                Data = new List<MarketplaceItemModel>(),
                Cursor = cursor, 
                SourceId = _config.ID
            };
            GraphQlResult<Nodeset> matches;

            //limits max amount to 100, if our take is larger, make multiple calls in batches of 100
            string currentCursor = cursor != null ? cursor.StartCursor ?? cursor.EndCursor : null;
            bool backwards = cursor?.EndCursor != null;
            int actualTake;
            bool bMore;
            do
            {
                actualTake = Math.Min((cursor.Skip + cursor.Take ?? 100) - (int)result.Count, 100);
                bMore = false;
                matches = await _cloudLib.SearchAsync(actualTake, currentCursor, backwards, keywords, exclude, cursor.HasTotalCount,
                    order:
                        new { metadata = new { title = OrderEnum.ASC }, modelUri = OrderEnum.ASC, publicationDate = OrderEnum.DESC });// "{metadata: {title: ASC}, modelUri: ASC, publicationDate: DESC}");
                if (matches == null || matches.Edges == null) return result;
                result.Data.AddRange(MapToModelsNodesetResult(matches.Edges));
                result.Count += matches.Edges.Count;
                if (!backwards && matches.PageInfo.HasNextPage)
                {
                    currentCursor = matches.PageInfo.EndCursor;
                    bMore = true;
                }
                if (backwards && matches.PageInfo.HasPreviousPage)
                {
                    currentCursor = matches.PageInfo.StartCursor;
                    bMore = true;
                }
            } while (bMore && (!cursor.Take.HasValue || result.Count < cursor.Skip + cursor.Take.Value));

            if (matches?.TotalCount > 0)
            {
                result.Count = matches.TotalCount;
                result.Cursor.TotalCount = matches.TotalCount;
            }
            //TBD - exclude some nodesets which are core nodesets - list defined in appSettings

            if (cursor.Skip > 0)
            {
                result.Data = result.Data.Skip(cursor.Skip).ToList();
            }
            _logger.Log(NLog.LogLevel.Warn, $"CloudLibDAL|Where|Duration: { timer.ElapsedMilliseconds}ms.");
            return result;
        }

        internal enum OrderEnum
        {
            ASC,
            DESC,
        };


        public async Task<DALResultWithSource<MarketplaceItemModel>> GetManyById(List<string> ids)
        {
            var matches = await _cloudLib.GetManyAsync(ids);

            return new DALResultWithSource<MarketplaceItemModel>()
            {
                //Count = matches.Count,
                Data = (matches == null || matches.Edges == null) ? 
                    new List<MarketplaceItemModel>() :
                    MapToModelsNodesetResult(matches.Edges),
                SummaryData = null,
                SourceId = _config.ID
            };
        }

        protected List<MarketplaceItemModel> MapToModelsNodesetResult(List<GraphQlNodeAndCursor<Nodeset>> entities)
        {
            var result = new List<MarketplaceItemModel>();

            foreach (var item in entities)
            {
                result.Add(MapToModelNodesetResult(item));
            }
            return result;
        }

        /// <summary>
        /// This is called when searching a collection of items. 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected MarketplaceItemModel MapToModelNodesetResult(GraphQlNodeAndCursor<Nodeset> entity)
        {
            if (entity != null && entity.Node != null)
            {
                //map results to a format that is common with marketplace items
                return new MarketplaceItemModel()
                {
                    ID = entity.Node.Identifier.ToString(),
                    Name = entity.Node.Identifier.ToString(),  //in marketplace items, name is used for navigation in friendly url
                    ExternalAuthor = entity.Node.Metadata.Contributor.Name,
                    Publisher = new PublisherModel() { DisplayName = entity.Node.Metadata.Contributor.Name, Name = entity.Node.Metadata.Contributor.Name },
                    //TBD
                    Description = entity.Node.Metadata.Description,
                    DisplayName = !string.IsNullOrEmpty(entity.Node.Metadata.Title) ? entity.Node.Metadata.Title : entity.Node.NamespaceUri.AbsolutePath,
                    Namespace = entity.Node.NamespaceUri.ToString(),
                    PublishDate = entity.Node.PublicationDate,
                    Type = _config.ItemType,
                    Version = entity.Node.Version,
                    IsFeatured = false,
                    ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImagePortrait.ID)),
                    ImageBanner = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageBanner.ID)),
                    ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageLandscape.ID)),
                    Cursor = entity.Cursor,
                    ExternalSourceId = _config.ID
                };
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// This is called when getting one nodeset.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected MarketplaceItemModel MapToModelNamespace(UANameSpace entity, ProfileItem entityLocal)
        {
            if (entity != null)
            {
                //metatags
                var metatags = entity.Keywords?.ToList();
                //if (entity.Category != null) metatags.Add(entity.Category.Name);

                //map results to a format that is common with marketplace items
                var result = new MarketplaceItemModel()
                {
                    ID = entity.Nodeset.Identifier.ToString(),
                    Name = entity.Nodeset.Identifier.ToString(),  //in marketplace items, name is used for navigation in friendly url
                    //Abstract = ns.Title,
                    ExternalAuthor = entity.Contributor?.Name,
                    Publisher = new PublisherModel()
                    {
                        DisplayName = entity.Contributor?.Name,
                        Name = entity.Contributor?.Name,
                        CompanyUrl = entity.Contributor?.Website?.ToString(),
                        Description = entity.Contributor?.Description,
                    },
                    //TBD
                    Description =
                        (string.IsNullOrEmpty(entity.Description) ? "" : $"<p>{entity.Description}</p>") +
                        (entity.DocumentationUrl == null ? "" : $"<p><a href='{entity.DocumentationUrl.ToString()}' target='_blank' rel='noreferrer' >Documentation: {entity.DocumentationUrl.ToString()}</a></p>") +
                        (entity.ReleaseNotesUrl == null ? "" : $"<p><a href='{entity.ReleaseNotesUrl.ToString()}' target='_blank' rel='noreferrer' >Release Notes: {entity.ReleaseNotesUrl.ToString()}</a></p>") +
                        (entity.LicenseUrl == null ? "" : $"<p><a href='{entity.LicenseUrl.ToString()}' target='_blank' rel='noreferrer' >License Information: {entity.LicenseUrl.ToString()}</a></p>") +
                        (entity.TestSpecificationUrl == null ? "" : $"<p><a href='{entity.TestSpecificationUrl.ToString()}' target='_blank' rel='noreferrer' >Test Specification: {entity.TestSpecificationUrl.ToString()}</a></p>") +
                        (entity.PurchasingInformationUrl == null ? "" : $"<p><a href='{entity.PurchasingInformationUrl.ToString()}' target='_blank' rel='noreferrer' >Purchasing Information: {entity.PurchasingInformationUrl.ToString()}</a></p>") +
                        (string.IsNullOrEmpty(entity.CopyrightText) ? "" : $"<p>{entity.CopyrightText}</p>"),
                    DisplayName = entity.Title,
                    Namespace = entity.Nodeset?.NamespaceUri?.ToString(),
                    MetaTags = metatags,
                    PublishDate = entity.Nodeset?.PublicationDate,
                    Type = _config.ItemType,
                    Version = entity.Nodeset?.Version,
                    IsFeatured = false,
                    ImagePortrait = entity.IconUrl == null ?
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImagePortrait.ID)) :
                        new ImageItemModel() { Src = entity.IconUrl.ToString() },
                    ImageLandscape = entity.IconUrl == null ?
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageLandscape.ID)) :
                        new ImageItemModel() { Src = entity.IconUrl.ToString() },
                    Updated = entity.Nodeset?.LastModifiedDate,
                    ExternalSourceId = "cloudlib"  //TBD - update this once we fold this into external source approach
                };

                //get related data - if any
                if (entityLocal != null)
                {
                    //go get related items if any
                    //get list of marketplace items associated with this list of ids, map to return object
                    var relatedItems = MapToModelRelatedItems(entityLocal?.RelatedItems).Result;

                    //get related profiles from CloudLib
                    var relatedProfiles = MapToModelRelatedProfiles(entityLocal?.RelatedExternalItems);

                    //map related items into specific buckets - required, recommended
                    result.RelatedItemsGrouped = base.GroupAndMergeRelatedItems(relatedItems, relatedProfiles);
                }
                return result;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Get related items from DB, filter out each group based on required/recommended/related flag
        /// assume all related items in same collection and a type id distinguishes between the types. 
        /// </summary>
        protected async Task<List<MarketplaceItemRelatedModel>> MapToModelRelatedItems(List<RelatedItem> items)
        {
            if (items == null)
            {
                return new List<MarketplaceItemRelatedModel>();
            }

            //get list of marketplace items associated with this list of ids, map to return object
            var filterRelated = MongoDB.Driver.Builders<MarketplaceItem>.Filter.In(x => x.ID, items.Select(y => y.MarketplaceItemId.ToString()));
            var matches = await _repoMarketplace.AggregateMatchAsync(filterRelated);

            if (!matches.Any()) return new List<MarketplaceItemRelatedModel>();

            //get all images associated with the list of related marketplace items
            var images = await GetImagesByMarketplaceIdList(
                matches.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList());

            return !matches.Any() ? new List<MarketplaceItemRelatedModel>() :
                matches.Select(x => new MarketplaceItemRelatedModel()
                {
                    RelatedId = x.ID,
                    Abstract = x.Abstract,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    Name = x.Name,
                    //Type = new LookupItemModel() {  }, // x.Type,
                    Version = x.Version,
                    ImagePortrait = MapToModelImageSimple(x.ImagePortraitId, images),
                    ImageLandscape = MapToModelImageSimple(x.ImageLandscapeId, images),
                    //assumes only one related item per type
                    RelatedType = MapToModelLookupItem(
                        items.Find(z => z.MarketplaceItemId.ToString().Equals(x.ID))?.RelatedTypeId,
                        _lookupItemsRelatedType.Where(z => z.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)).ToList())
                }).ToList();
        }

        /// <summary>
        /// Get related items from DB, filter out each group based on required/recommended/related flag
        /// assume all related items in same collection and a type id distinguishes between the types. 
        /// </summary>
        protected List<MarketplaceItemRelatedModel> MapToModelRelatedProfiles(List<RelatedProfileItem> items)
        {
            if (items == null)
            { 
                return new List<MarketplaceItemRelatedModel>();
            }

            //get list of profile items associated with this list of ids, call CloudLib to get the supporting info for these
            var matches = this.GetManyById(items.Select(x => x.ExternalId).ToList()).Result.Data;
            return !matches.Any() ? new List<MarketplaceItemRelatedModel>() :
                matches.Select(x => new MarketplaceItemRelatedModel()
                {
                    RelatedId = x.ID,
                    Abstract = x.Abstract,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    Name = x.Name,
                    Namespace = x.Namespace,
                    Type = x.Type,
                    Version = x.Version,
                    ImagePortrait = x.ImagePortrait,
                    ImageLandscape = x.ImageLandscape,
                    //assumes only one related item per type
                    RelatedType = //items.Find(x => x.ProfileId.Equals(x.ID)) == null ? null :
                        MapToModelLookupItem(
                        items.Find(z => z.ExternalId.Equals(x.ID)).RelatedTypeId,
                        _lookupItemsRelatedType.Where(z => z.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)).ToList())
                }).ToList();
        }

        protected ImageItemSimpleModel MapToModelImageSimple(MongoDB.Bson.BsonObjectId id, List<ImageItemModel> images)
        {
            var match = images.Find(x => id.Equals(new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))));
            if (match == null) return null;
            return new ImageItemSimpleModel()
            {
                ID = match.ID,
                FileName = match.FileName,
                MarketplaceItemId = match.MarketplaceItemId,
                Type = match.Type
            };
        }

        protected LookupItemModel MapToModelLookupItem(MongoDB.Bson.BsonObjectId lookupId, List<LookupItemModel> allItems)
        {
            if (lookupId == null) return null;

            var match = allItems.FirstOrDefault(x => x.ID == lookupId.ToString());
            if (match == null) return null;
            return new LookupItemModel()
            {
                ID = match.ID,
                Code = match.Code,
                DisplayOrder = match.DisplayOrder,
                LookupType = new LookupTypeModel() { EnumValue = match.LookupType.EnumValue, Name = match.Name },
                Name = match.Name
            };
        }

    }
}