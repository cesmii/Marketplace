namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using NLog;
    using Opc.Ua.Cloud.Library.Client;

    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.Common.Models;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Common.CloudLibClient;
    using CESMII.Marketplace.Data.Repositories;

    /// <summary>
    /// This is a hybrid DAL. It gets data from the local Mongo DB for the related items and related profiles. Then gets 
    /// the profile info from the cloud library
    /// </summary>
    public class AdminCloudLibDAL : IAdminCloudLibDAL<AdminMarketplaceItemModel>
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ICloudLibWrapper _cloudLib;
        protected readonly IMongoRepository<ProfileItem> _repo;
        protected readonly IMongoRepository<MarketplaceItem> _repoMarketplace;
        protected readonly IDal<ImageItem, ImageItemModel> _dalImages;
        private readonly MarketplaceItemConfig _config;
        private readonly LookupItemModel _smItemType;
        //protected readonly IDal<MarketplaceItem, MarketplaceItemModel> _dalMarkteplace;

        //supporting data
        protected readonly List<LookupItemModel> _lookupItemsRelatedType;
        protected readonly List<ImageItemModel> _images;

        public AdminCloudLibDAL(ICloudLibWrapper cloudLib,
            IMongoRepository<ProfileItem> repo,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<ImageItem, ImageItemModel> dalImages,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            //IDal<MarketplaceItem, MarketplaceItemModel> dalMarkteplace,
            ConfigUtil configUtil)
        {
            _cloudLib = cloudLib;

            //init objects to use for related items
            _repo = repo;
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

        public async Task<string> Add(AdminMarketplaceItemModel model, string userId)
        {
            throw new NotImplementedException("Use Update method instead");
        }

        /// <summary>
        /// This is only updating or adding data into the local Mongo DB. The bulk of the profile data is 
        /// read only and comes from the Cloud Lib. The only data being udpated is related marketplace items
        /// and related profile items.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<int> Upsert(AdminMarketplaceItemModel model, string userId)
        {
            ProfileItem entity = _repo.FindByCondition(x => x.ProfileId == model.ID).FirstOrDefault();
            bool isAdd = entity == null;
            if (entity == null)
            {
                entity = new ProfileItem() {
                    ID = "",
                    ProfileId = model.ID,
                    IsActive = true,
                    Created = DateTime.UtcNow,
                    CreatedById = MongoDB.Bson.ObjectId.Parse(userId)
                };
            }
            this.MapToEntity(ref entity, model);
            entity.Updated = DateTime.UtcNow;
            entity.UpdatedById = MongoDB.Bson.ObjectId.Parse(userId);

            if (isAdd)
                await _repo.AddAsync(entity);
            else
                await _repo.UpdateAsync(entity);
            return 1;
        }

        public async Task<AdminMarketplaceItemModel> GetById(string id) {
            var entity = await _cloudLib.DownloadAsync(id);
            //var entity = await _cloudLib.GetById(id);
            if (entity == null) return null;
            //get related items from local db
            var entityLocal = _repo.FindByCondition(x => x.ProfileId.Equals(id)).FirstOrDefault();

            return MapToModelNamespace(entity, entityLocal);
        }

        public async Task<List<AdminMarketplaceItemModel>> GetAll() {
            var result = await this.Where(null);
            return result;
        }

        public async Task<List<AdminMarketplaceItemModel>> Where(string query,
            List<string> ids = null, List<string> processes = null, List<string> verticals = null, List<string> exclude = null)
        {
            //get the list of profiles in the local db with related items. If the item isn't in there, then we won't 
            //return the profile even from Cloud Lib. The admin ui is meant to only show the profile list items that have related data.  
            var idsLocal = _repo.FindByCondition(x => 
                ids == null || ids.Any(y => y.Equals(x.ProfileId))).Select(x => x.ProfileId).ToList();
            if (idsLocal == null || idsLocal.Count == 0) return new List<AdminMarketplaceItemModel>();

            //Note - splitting out each word in query into a separate string in the list
            //Per team, don't split out query into multiple keyword items
            //var keywords = string.IsNullOrEmpty(query) ? new List<string>() : query.Split(" ").ToList();
            var keywords = new List<string>();

            //append list of ids, processes, verticals
            //if (idsLocal != null)
            //{
            //    keywords = keywords.Union(idsLocal).ToList();
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

            var matches = await _cloudLib.SearchAsync(null, null, false, keywords, exclude, true);
            if (matches == null || matches.Edges == null) return new List<AdminMarketplaceItemModel>();

            //TBD - exclude some nodesets which are core nodesets - list defined in appSettings

            //only get items who have a local id represented
            var matchesFiltered = matches.Edges.Where(x =>
                idsLocal.Any(y => x.Node != null && y.Equals(x.Node.Identifier.ToString()))).ToList();
            return MapToModelsNodesetResult(matchesFiltered);
        }

        public async Task<List<AdminMarketplaceItemModel>> GetManyById(List<string> ids)
        {
            //get the list of profiles in the local db with related items. If the item isn't in there, then we won't 
            //return the profile even from Cloud Lib. The admin ui is meant to only show the profile list items that have related data.  
            var idsLocal = _repo.FindByCondition(x =>
                ids == null || ids.Any(y => y.Equals(x.ProfileId))).Select(x => x.ProfileId).ToList();
            if (idsLocal == null || idsLocal.Count == 0) return new List<AdminMarketplaceItemModel>();

            var matches = await _cloudLib.GetManyAsync(ids);
            if (matches == null || matches.Edges == null) return new List<AdminMarketplaceItemModel>();

            return MapToModelsNodesetResult(matches.Edges);
        }

        public async Task<int> Delete(string id, string userId)
        {
            ProfileItem entity = _repo.FindByCondition(x => x.ProfileId == id).FirstOrDefault();
            if (entity == null) return 0;
            await _repo.Delete(entity);
            return 1;
        }

        protected List<AdminMarketplaceItemModel> MapToModelsNodesetResult(List<GraphQlNodeAndCursor<Nodeset>> entities)
        {
            var result = new List<AdminMarketplaceItemModel>();

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
        protected AdminMarketplaceItemModel MapToModelNodesetResult(GraphQlNodeAndCursor<Nodeset> entity)
        {
            if (entity != null && entity.Node != null)
            {
                //map results to a format that is common with marketplace items
                return new AdminMarketplaceItemModel()
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
                    ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape))
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
        protected AdminMarketplaceItemModel MapToModelNamespace(UANameSpace entity, ProfileItem entityLocal)
        {
            if (entity != null)
            {
                //metatags
                var metatags = entity.Keywords?.ToList();
                //if (entity.Category != null) metatags.Add(entity.Category.Name);

                //map results to a format that is common with marketplace items
                var result = new AdminMarketplaceItemModel()
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
                //get list of marketplace items associated with this list of ids, map to return object
                result.RelatedItems = MapToModelRelatedItems(entityLocal?.RelatedItems);
                result.RelatedProfiles = MapToModelRelatedProfiles(entityLocal?.RelatedProfiles);

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
        protected List<MarketplaceItemRelatedModel> MapToModelRelatedItems(List<RelatedItem> items)
        {
            if (items == null)
            {
                return new List<MarketplaceItemRelatedModel>();
            }

            //get list of marketplace items associated with this list of ids, map to return object
            var matches = _repoMarketplace.FindByCondition(x =>
                items.Any(y => y.MarketplaceItemId.Equals(
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))))).ToList();

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
        protected List<ProfileItemRelatedModel> MapToModelRelatedProfiles(List<RelatedProfileItem> items)
        {
            if (items == null)
            { 
                return new List<ProfileItemRelatedModel>();
            }

            //get list of profile items associated with this list of ids, call CloudLib to get the supporting info for these
            var profiles = _cloudLib.GetManyAsync(items.Select(x => x.ProfileId).ToList()).Result;
            var matches = (profiles == null || profiles.Edges == null) ? 
                new List<AdminMarketplaceItemModel>() :
                MapToModelsNodesetResult(profiles.Edges);

            return !matches.Any() ? new List<ProfileItemRelatedModel>() :
                matches.Select(x => new ProfileItemRelatedModel()
                {
                    RelatedId = x.ID,
                    Description = x.Description,
                    DisplayName = x.DisplayName,
                    Name = x.Name,
                    Namespace = x.Namespace,
                    Version = x.Version,
                    //assumes only one related item per type
                    RelatedType = MapToModelLookupItem(
                        items.Find(z => z.ProfileId.ToString().Equals(x.ID))?.RelatedTypeId,
                        _lookupItemsRelatedType.Where(z => z.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)).ToList())
                })
                .OrderBy(x => x.RelatedType.DisplayOrder)
                .ThenBy(x => x.RelatedType.Name)
                .ThenBy(x => x.DisplayName)
                .ToList();
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

        protected void MapToEntity(ref ProfileItem entity, AdminMarketplaceItemModel model)
        {
            //ensure this value is always without spaces and is lowercase. 
            //replace child collection of items - ids are preserved
            entity.RelatedItems = model.RelatedItems
                .Where(x => x.RelatedType != null) //only include selected rows
                .Select(x => new RelatedItem()
                {
                    MarketplaceItemId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.RelatedId)),
                    RelatedTypeId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.RelatedType.ID)),
                }).ToList();
            //replace child collection of items - ids are preserved
            entity.RelatedProfiles = model.RelatedProfiles
                .Where(x => x.RelatedType != null) //only include selected rows
                .Select(x => new RelatedProfileItem()
                {
                    //ID = x.ID,
                    ProfileId = x.RelatedId,
                    RelatedTypeId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.RelatedType.ID)),
                }).ToList();
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