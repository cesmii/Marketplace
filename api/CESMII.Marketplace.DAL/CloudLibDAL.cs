namespace CESMII.Marketplace.DAL
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
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Common.CloudLibClient;

    using Opc.Ua.Cloud.Library.Client;
    using CESMII.Marketplace.Data.Repositories;

    /// <summary>
    /// Most lookup data is contained in this single entity and differntiated by a lookup type. 
    /// </summary>
    public class CloudLibDAL : ICloudLibDAL<MarketplaceItemModelWithCursor>
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ICloudLibWrapper _cloudLib;
        protected readonly IMongoRepository<ProfileItem> _repoProfile;
        protected readonly IMongoRepository<MarketplaceItem> _repoMarketplace;
        protected readonly IDal<ImageItem, ImageItemModel> _dalImages;
        private readonly MarketplaceItemConfig _config;
        private readonly LookupItemModel _smItemType;
        private readonly List<LookupItemModel> _lookupItemsRelatedType;
        //protected readonly IDal<MarketplaceItem, MarketplaceItemModelWithCursor> _dalMarkteplace;

        //supporting data
        protected List<ImageItemModel> _images;

        public CloudLibDAL(ICloudLibWrapper cloudLib,
            IMongoRepository<ProfileItem> repoProfile,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<ImageItem, ImageItemModel> dalImages,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            //IDal<MarketplaceItem, MarketplaceItemModelWithCursor> dalMarkteplace,
            ConfigUtil configUtil)
        {
            _cloudLib = cloudLib;

            //init objects to use for related items
            _repoProfile = repoProfile;
            _repoMarketplace = repoMarketplace;
            _dalImages = dalImages;
            //_dalMarkteplace = dalMarkteplace;

            //init some stuff we will use during the mapping methods
            _config = configUtil.MarketplaceSettings.SmProfile;

            //get SM Profile type
            _smItemType = dalLookup.Where(
                x => x.LookupType.EnumValue.Equals(LookupTypeEnum.SmItemType) &&
                x.ID.Equals(_config.TypeId)
                , null, null, false, false).Data.FirstOrDefault();

            //get related type lookup items
            _lookupItemsRelatedType = dalLookup.Where(
                x => x.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)
                , null, null, false, false).Data;

            //get default images
            _images = dalImages.Where(
                x => x.ID.Equals(_config.DefaultImageIdLandscape) ||
                x.ID.Equals(_config.DefaultImageIdPortrait)
                //|| x.ID.Equals(_config.DefaultImageIdSquare)
                , null, null, false, false).Data;

        }

        public async Task<MarketplaceItemModelWithCursor> GetById(string id) {
            var entity = await _cloudLib.DownloadAsync(id);
            //var entity = await _cloudLib.GetById(id);
            if (entity == null) return null;
            //get related items from local db
            var entityLocal = _repoProfile.FindByCondition(x => x.ProfileId.Equals(id)).FirstOrDefault();

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

        public async Task<List<MarketplaceItemModelWithCursor>> GetAll() {
            //setting to very high to get all...this is called by admin which needs full list right now for dropdown selection
            var result = await this.Where(null, 0, 999); 
            return result.Data;
        }

        public async Task<DALResult<MarketplaceItemModelWithCursor>> Where(string query, int? skip = null, int? take = null, string? startCursor = null, string? endCursor = null, bool noTotalCount = false,
            List<string> ids = null, List<string> processes = null, List<string> verticals = null, List<string> exclude = null)
        {
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

            DALResult<MarketplaceItemModelWithCursor> result = new DALResult<MarketplaceItemModelWithCursor>();
            result.Data = new List<MarketplaceItemModelWithCursor>();
            GraphQlResult<Nodeset> matches;

            string currentCursor = startCursor ?? endCursor;
            bool backwards = endCursor != null;
            int actualTake;
            bool bMore;
            do
            {
                actualTake = Math.Min((skip + take ?? 100) - (int)result.Count, 100);
                bMore = false;
                matches = await _cloudLib.SearchAsync(actualTake, currentCursor, backwards, keywords, exclude, noTotalCount,
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
            } while (bMore && (take == null || result.Count < skip + take));

            if (matches?.TotalCount > 0)
            {
                result.Count = matches.TotalCount;
            }
            if (!backwards)
            {
                result.StartCursor = startCursor;
                result.EndCursor = currentCursor;
            }
            else
            {
                result.StartCursor = currentCursor;
                result.EndCursor = endCursor;
            }
            //TBD - exclude some nodesets which are core nodesets - list defined in appSettings

            if (skip > 0)
            {
                result.Data = result.Data.Skip(skip.Value).ToList();
            }
            return result;
        }

        internal enum OrderEnum
        {
            ASC,
            DESC,
        };


        public async Task<List<MarketplaceItemModelWithCursor>> GetManyById(List<string> ids)
        {
            var matches = await _cloudLib.GetManyAsync(ids);
            if (matches == null || matches.Edges == null) return new List<MarketplaceItemModelWithCursor>();

            return MapToModelsNodesetResult(matches.Edges);
        }

        protected List<MarketplaceItemModelWithCursor> MapToModelsNodesetResult(List<GraphQlNodeAndCursor<Nodeset>> entities)
        {
            var result = new List<MarketplaceItemModelWithCursor>();

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
        protected MarketplaceItemModelWithCursor MapToModelNodesetResult(GraphQlNodeAndCursor<Nodeset> entity)
        {
            if (entity != null && entity.Node != null)
            {
                //map results to a format that is common with marketplace items
                return new MarketplaceItemModelWithCursor()
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
                    Type = _smItemType,
                    Version = entity.Node.Version,
                    IsFeatured = false,
                    ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)),
                    //ImageSquare = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdSquare)),
                    ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape)),
                    Cursor = entity.Cursor,
                    
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
        protected MarketplaceItemModelWithCursor MapToModelNamespace(UANameSpace entity, ProfileItem entityLocal)
        {
            if (entity != null)
            {
                //metatags
                var metatags = entity.Keywords?.ToList();
                //if (entity.Category != null) metatags.Add(entity.Category.Name);

                //map results to a format that is common with marketplace items
                var result = new MarketplaceItemModelWithCursor()
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
                    Type = _smItemType,
                    Version = entity.Nodeset?.Version,
                    IsFeatured = false,
                    ImagePortrait = entity.IconUrl == null ?
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)) :
                        new ImageItemModel() { Src = entity.IconUrl.ToString() },
                    ImageLandscape = entity.IconUrl == null ?
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape)) :
                        new ImageItemModel() { Src = entity.IconUrl.ToString() },
                    Updated = entity.Nodeset?.LastModifiedDate
                };

                //get related data - if any
                if (entityLocal != null)
                {
                    //go get related items if any
                    //get list of marketplace items associated with this list of ids, map to return object
                    var relatedItems = MapToModelRelatedItems(entityLocal?.RelatedItems).Result;

                    //get related profiles from CloudLib
                    var relatedProfiles = MapToModelRelatedProfiles(entityLocal?.RelatedProfiles);

                    //map related items into specific buckets - required, recommended
                    result.RelatedItemsGrouped = GroupAndMergeRelatedItems(relatedItems, relatedProfiles);
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
            var images = _dalImages.Where(x => matches.Any(y =>
                y.ImageLandscapeId.Equals(new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))) ||
                y.ImagePortraitId.Equals(new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID)))),
                null, null, false, false).Data;

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
            var matches = this.GetManyById(items.Select(x => x.ProfileId).ToList()).Result;
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
                        items.Find(z => z.ProfileId.Equals(x.ID)).RelatedTypeId,
                        _lookupItemsRelatedType.Where(z => z.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)).ToList())
                }).ToList();
        }

        /// <summary>
        /// Take two related sets, group them by type and union them, filter them by related type and order them
        /// </summary>
        /// <param name="items"></param>
        /// <param name="itemsProfile"></param>
        /// <param name="relatedType"></param>
        /// <returns></returns>
        protected List<RelatedItemsGroupBy> GroupAndMergeRelatedItems(
            List<MarketplaceItemRelatedModel> items,
            List<MarketplaceItemRelatedModel> itemsProfile)
        {
            if (items == null && itemsProfile == null)
            {
                return new List<RelatedItemsGroupBy>();
            }
            //group by both sets and then merge
            var result = new List<RelatedItemsGroupBy>();

            //convert group to return type
            if (items?.Count > 0)
            {
                var grpItems = items.GroupBy(x => new { ID = x.RelatedType.ID });
                foreach (var item in grpItems)
                {
                    result.Add(new RelatedItemsGroupBy()
                    {
                        RelatedType = items.Where(x => x.RelatedType.ID.Equals(item.Key.ID)).FirstOrDefault()?.RelatedType,
                        Items = items.Where(x => x.RelatedType.ID.Equals(item.Key.ID)).ToList() // item.ToList()
                    });
                }
            }

            //append profiles group to existing group (if present)
            if (itemsProfile?.Count > 0)
            {
                var grpProfile = itemsProfile.GroupBy(x => new { ID = x.RelatedType.ID });
                foreach (var item in grpProfile)
                {
                    var matches = itemsProfile.Where(x => x.RelatedType.ID.Equals(item.Key.ID)).ToList();

                    var existingGroup = result.Find(x => x.RelatedType.ID.Equals(item.Key.ID));
                    if (existingGroup == null)
                    {
                        result.Add(new RelatedItemsGroupBy()
                        {
                            RelatedType = matches.FirstOrDefault()?.RelatedType,
                            Items = matches
                        });
                    }
                    else
                    {
                        existingGroup.Items = existingGroup.Items.Union(matches).ToList();
                    }
                }
            }

            //do some ordering
            result = result
                .OrderBy(x => x.RelatedType.DisplayOrder)
                .ThenBy(x => x.RelatedType.Name).ToList();
            foreach (var g in result)
            {
                g.Items = g.Items
                    .OrderBy(x => x.DisplayName)
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.Namespace)
                    .ThenBy(x => x.Version)
                    .ToList();
            }

            return result;
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


        public virtual void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            //set flag so we only run dispose once.
            _disposed = true;
        }
    }
}