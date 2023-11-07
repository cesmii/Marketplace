namespace CESMII.Marketplace.DAL.ExternalSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.DAL.ExternalSources.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using CESMII.Common.CloudLibClient;

    using Opc.Ua.Cloud.Library.Client;

    /// <summary>
    /// Shared DAL Class for CloudLib DAL and Admin Cloud Lib DAL. The admin version is more of a hybrid which 
    /// gets data from CloudLib and tries to marry it up with data stored locally (like related items)
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    public class CloudLibBaseDAL<TEntity, TModel> : ExternalBaseDAL<TEntity, TModel> where TEntity : ExternalAbstractEntity where TModel : MarketplaceItemModelBase
    {
        protected class CloudLibConfigData
        {
            public UACloudLibClient.Options Settings { get; set; }
            public List<string> ExcludedNodeSets { get; set; }
        }

        protected ICloudLibWrapper _cloudLib;
        //TBD - update this to use ExternalItem
        protected IMongoRepository<ExternalItem> _repoExternalItem;
        protected IMongoRepository<MarketplaceItem> _repoMarketplace;
        protected List<LookupItemModel> _lookupItemsRelatedType;
        protected List<ImageItemModel> _images;
        // Custom implementation of the Data property in the DB. 
        // This can be unique for each source.
        protected CloudLibConfigData _configCustom;


        public CloudLibBaseDAL(IConfiguration configuration, 
            ExternalSourceModel config,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IHttpApiFactory httpApiFactory,
            IMongoRepository<ImageItem> repoImages,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ExternalItem> repoExternalItem
            ) : base(configuration, dalExternalSource, config, httpApiFactory, repoImages)
        {
            this.Init(dalLookup, repoMarketplace, repoExternalItem);
        }

        public CloudLibBaseDAL(IConfiguration configuration,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IHttpApiFactory httpApiFactory,
            IMongoRepository<ImageItem> repoImages,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ExternalItem> repoExternalItem
            ) : base(configuration, dalExternalSource, "cloudlib", httpApiFactory, repoImages)
        {
            this.Init(dalLookup, repoMarketplace, repoExternalItem);
        }

        protected void Init(
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ExternalItem> repoExternalItem
            )
        {
            //grab Cloud lib url, user name and password and excluded nodeset
            //data already decrypted in DAL, just convert to object.
            _configCustom = JsonConvert.DeserializeObject<CloudLibConfigData>(_config.Data);
            _configCustom.Settings.EndPoint = _config.BaseUrl;

            //instantiate this way rather than through DI so that the factory does not need to know 
            //about specifics of each external data source
            Microsoft.Extensions.Options.IOptions<UACloudLibClient.Options> options =
                Microsoft.Extensions.Options.Options.Create(_configCustom.Settings);
            //Microsoft.Extensions.Logging.ILogger<CloudLibWrapper> log = 
            //    Microsoft.Extensions.Logging.LoggerFactoryExtensions.CreateLogger<CloudLibWrapper>();
            _cloudLib = new CloudLibWrapper(options, null); //logger not used in CloudLibWrapper

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

        public virtual async Task<TModel> GetById(string id) {
            var entity = await _cloudLib.DownloadAsync(id);
            //var entity = await _cloudLib.GetById(id);
            if (entity == null) return null;
            //get related items from local db
            var entityLocal = _repoExternalItem.FindByCondition(x => x.ExternalSource != null && x.ExternalSource.ID.Equals(id)).FirstOrDefault();

            return MapToModelNamespace(entity, entityLocal);
        }

        public virtual async Task<ExternalItemExportModel> Export(string id)
        {
            throw new NotImplementedException("Implement in derived class.");
        }

        public virtual async Task<DALResultWithSource<TModel>> GetAll() {
            //setting to very high to get all...this is called by admin which needs full list right now for dropdown selection
            var result = await this.Where(null, new SearchCursor() { PageIndex = 0, Skip = 0, Take = 999 }); 
            return result;
        }

        public virtual async Task<DALResultWithSource<TModel>> Where(string query,
            SearchCursor cursor, List<string> processes = null, List<string> verticals = null)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            //Note - splitting out each word in query into a separate string in the list
            //Per team, don't split out query into multiple keyword items
            //var keywords = string.IsNullOrEmpty(query) ? new List<string>() : query.Split(" ").ToList();
            var keywords = new List<string>();

            //append list of ids, processes, verticals
            //if (ids != null)
            //{
            //    keywords = keywords.Union(ids).ToList();
            //}
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

            var result = new DALResultWithSource<TModel>() { 
                Data = new List<TModel>(),
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

                //wrap in try/catch so we can isolate a failed API call
                try
                {
                    matches = await _cloudLib.SearchAsync(actualTake, currentCursor, backwards, keywords, _configCustom.ExcludedNodeSets, cursor.HasTotalCount,
                        order:
                            new { metadata = new { title = OrderEnum.ASC }, modelUri = OrderEnum.ASC, publicationDate = OrderEnum.DESC });// "{metadata: {title: ASC}, modelUri: ASC, publicationDate: DESC}");
                }
                catch (Exception ex)
                {
                    //log the details of the query for easier troubleshooting
                    var msg = $"CloudLibDAL.Where||{_configCustom.Settings.EndPoint}||Query:{query}||Skip:{cursor.Skip}||Take:{cursor.Take}||{ex.Message}";
                    _logger.Log(NLog.LogLevel.Error, ex, msg);
                    //return an empty result if we should not fail the whole search because this one area had issue.
                    if (!_config.FailOnException)
                    {
                        return new DALResultWithSource<TModel>()
                        { Count = 0, Data = new List<TModel>(), SourceId = _config.ID, Cursor = cursor };
                    }
                    //else we should stop the whole search and throw the caught exception 
                    throw;
                }

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

        protected enum OrderEnum
        {
            ASC,
            DESC,
        };


        public virtual async Task<DALResultWithSource<TModel>> GetRelatedItems()
        {
            //get the list of items in the local db with related items. If the item isn't in there, then we won't 
            //return the profile even from Cloud Lib. The admin ui is meant to only show the profile list items that have related data.  
            var idsLocal = _repoExternalItem
                .FindByCondition(x => x.ExternalSource != null && x.ExternalSource.SourceId.Equals(_config.ID))
                .ToList();
            if (idsLocal == null || idsLocal.Count == 0) return new DALResultWithSource<TModel>();

            //when we grab ids, let's get all by id and then apply page, take on our side. widen the page size because the trimming out by ids happens AFTER we get data. 
            return await this.GetManyById(idsLocal.Select(x => x.ExternalSource.ID).ToList());
        }

        public virtual async Task<DALResultWithSource<TModel>> GetManyById(List<string> ids = null)
        {
            if (ids == null || ids.Count == 0) return new DALResultWithSource<TModel>()
            {
                Count = ids.Count,
                Data = new List<TModel>(),
                SummaryData = null,
                SourceId = _config.ID
            };

            var matches = await _cloudLib.GetManyAsync(ids);

            var data = (matches == null || matches.Edges == null) ?
                    new List<TModel>() :
                    MapToModelsNodesetResult(matches.Edges);

            return new DALResultWithSource<TModel>()
            {
                Count = ids.Count,
                Data = data,
                SummaryData = null,
                SourceId = _config.ID
            };
        }

        protected List<TModel> MapToModelsNodesetResult(List<GraphQlNodeAndCursor<Nodeset>> entities)
        {
            var result = new List<TModel>();

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
        protected virtual TModel MapToModelNodesetResult(GraphQlNodeAndCursor<Nodeset> entity)
        {
            //implement in derived classes
            return null;
        }

        /// <summary>
        /// This is called when getting one nodeset.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual TModel MapToModelNamespace(UANameSpace entity, ExternalItem entityLocal)
        {
            //implement in derived classes
            return null;
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


        protected string MapToModelDescription(UANameSpace entity)
        {
            return (string.IsNullOrEmpty(entity.Description) ? "" : $"<p>{entity.Description}</p>") +
                        (entity.DocumentationUrl == null ? "" : $"<p><a href='{entity.DocumentationUrl.ToString()}' target='_blank' rel='noreferrer' >Documentation: {entity.DocumentationUrl.ToString()}</a></p>") +
                        (entity.ReleaseNotesUrl == null ? "" : $"<p><a href='{entity.ReleaseNotesUrl.ToString()}' target='_blank' rel='noreferrer' >Release Notes: {entity.ReleaseNotesUrl.ToString()}</a></p>") +
                        (entity.LicenseUrl == null ? "" : $"<p><a href='{entity.LicenseUrl.ToString()}' target='_blank' rel='noreferrer' >License Information: {entity.LicenseUrl.ToString()}</a></p>") +
                        (entity.TestSpecificationUrl == null ? "" : $"<p><a href='{entity.TestSpecificationUrl.ToString()}' target='_blank' rel='noreferrer' >Test Specification: {entity.TestSpecificationUrl.ToString()}</a></p>") +
                        (entity.PurchasingInformationUrl == null ? "" : $"<p><a href='{entity.PurchasingInformationUrl.ToString()}' target='_blank' rel='noreferrer' >Purchasing Information: {entity.PurchasingInformationUrl.ToString()}</a></p>") +
                        (string.IsNullOrEmpty(entity.CopyrightText) ? "" : $"<p>{entity.CopyrightText}</p>");
        }

        protected ExternalSourceSimple MapToModelExternalSource(string nodeId)
        {
            return new ExternalSourceSimple() { ID = nodeId, SourceId = _config.ID, Code = _config.Code };
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