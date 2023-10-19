namespace CESMII.Marketplace.DAL.ExternalSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.DAL.ExternalSources.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;

    using Opc.Ua.Cloud.Library.Client;

    public class AdminCloudLibDAL : CloudLibBaseDAL<ExternalAbstractEntity, AdminMarketplaceItemModel>, IAdminExternalDAL<AdminMarketplaceItemModel>
    {

        public AdminCloudLibDAL(ExternalSourceModel config,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IHttpApiFactory httpApiFactory,
            IMongoRepository<ImageItem> repoImages,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ProfileItem> repoExternalItem
            ) : base(config, dalExternalSource, httpApiFactory, repoImages, dalLookup, repoMarketplace, repoExternalItem)
        {
        }

        //------------------------------------------------------
        //Most of the base get methods are executed in the base dal
        //------------------------------------------------------
        public override async Task<DALResultWithSource<AdminMarketplaceItemModel>> Where(string query,
            SearchCursor cursor, List<string> processes = null, List<string> verticals = null)
        {
            throw new NotSupportedException();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
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
            ProfileItem entity = _repoExternalItem.FindByCondition(x => 
                x.ExternalSource.ID == model.ExternalSource.ID && x.ExternalSource.SourceId == model.ExternalSource.SourceId).FirstOrDefault();
            bool isAdd = entity == null;
            if (entity == null)
            {
                entity = new ProfileItem()
                {
                    ID = "",
                    ExternalSource = model.ExternalSource,
                    IsActive = true,
                    Created = DateTime.UtcNow,
                    CreatedById = MongoDB.Bson.ObjectId.Parse(userId)
                };
            }
            this.MapToEntity(ref entity, model);
            entity.Updated = DateTime.UtcNow;
            entity.UpdatedById = MongoDB.Bson.ObjectId.Parse(userId);

            if (isAdd)
                await _repoExternalItem.AddAsync(entity);
            else
                await _repoExternalItem.UpdateAsync(entity);
            return 1;
        }

        public async Task<int> Delete(ExternalSourceSimple source, string userId)
        {
            ProfileItem entity = _repoExternalItem.FindByCondition(x =>
                x.ExternalSource.ID == source.ID && x.ExternalSource.SourceId == source.SourceId).FirstOrDefault();
            if (entity == null) return 0;
            await _repoExternalItem.Delete(entity);
            return 1;
        }

        /// <summary>
        /// This is called when searching a collection of items. 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override AdminMarketplaceItemModel MapToModelNodesetResult(GraphQlNodeAndCursor<Nodeset> entity)
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
                    Type = _config.ItemType,
                    Version = entity.Node.Version,
                    IsFeatured = false,
                    ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImagePortrait.ID)),
                    ImageBanner = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageBanner.ID)),
                    ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageLandscape.ID)),
                    Cursor = entity.Cursor,
                    //we expect this is unique - per source
                    ExternalSource = MapToModelExternalSource(entity.Node.Identifier.ToString())
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
        protected override AdminMarketplaceItemModel MapToModelNamespace(UANameSpace entity, ProfileItem entityLocal)
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
                    Description = MapToModelDescription(entity),
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
                    //we expect this is unique - per source
                    ExternalSource = MapToModelExternalSource(entity.Nodeset.Identifier.ToString())
                };

                //get related data - if any
                if (entityLocal != null)
                {
                    //go get related items if any
                    //get list of marketplace items associated with this list of ids, map to return object
                    var relatedItems = MapToModelRelatedItems(entityLocal?.RelatedItems).Result;

                    //get related profiles from CloudLib
                    var relatedProfiles = MapToModelRelatedExternalItems(entityLocal?.RelatedExternalItems);

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
            entity.RelatedExternalItems = model.RelatedItemsExternal
                .Where(x => x.RelatedType != null) //only include selected rows
                .Select(x => new RelatedExternalItem()
                {
                    //ID = x.ID,
                    ExternalSource = x.ExternalSource,
                    RelatedTypeId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.RelatedType.ID)),
                }).ToList();
        }

    }
}