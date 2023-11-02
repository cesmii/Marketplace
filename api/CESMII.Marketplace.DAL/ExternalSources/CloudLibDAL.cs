namespace CESMII.Marketplace.DAL.ExternalSources
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Opc.Ua.Cloud.Library.Client;

    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.DAL.ExternalSources.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using CESMII.Marketplace.Common.Enums;

    public class CloudLibDAL : CloudLibBaseDAL<ExternalAbstractEntity, MarketplaceItemModel>, IExternalDAL<MarketplaceItemModel>
    {
        //do nothing, all stuff handled in CloudLibBaseDAL for now. 
        //Adding wrapper around it so that generic types can be used for each DAL using this same base. 

        public CloudLibDAL(ExternalSourceModel config,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IHttpApiFactory httpApiFactory,
            IMongoRepository<ImageItem> repoImages,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ExternalItem> repoExternalItem
            ) : base(config, dalExternalSource, httpApiFactory, repoImages, dalLookup, repoMarketplace, repoExternalItem)
        {
        }

        public CloudLibDAL(
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IHttpApiFactory httpApiFactory,
            IMongoRepository<ImageItem> repoImages,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ExternalItem> repoExternalItem
            ) : base(dalExternalSource, httpApiFactory, repoImages, dalLookup, repoMarketplace, repoExternalItem)
        {
        }

        public override async Task<ExternalItemExportModel> Export(string id)
        {
            var entity = await _cloudLib.DownloadAsync(id);
            if (entity == null) return null;

            //return the whole thing because we also email some info to request info and use
            //other data in this entity.
            var result = new ExternalItemExportModel()
            {
                Item = MapToModelNamespace(entity, null),
                Data = entity.Nodeset?.NodesetXml
            };
            return result;
        }

        /// <summary>
        /// This is called when searching a collection of items. 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override MarketplaceItemModel MapToModelNodesetResult(GraphQlNodeAndCursor<Nodeset> entity)
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
        protected override MarketplaceItemModel MapToModelNamespace(UANameSpace entity, ExternalItem entityLocal)
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
                    var relatedItemsExternal = MapToModelRelatedItemsExternal(entityLocal?.RelatedItemsExternal);

                    //map related items into specific buckets - required, recommended
                    result.RelatedItemsGrouped = base.GroupAndMergeRelatedItems(relatedItems, relatedItemsExternal);
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
        protected List<MarketplaceItemRelatedModel> MapToModelRelatedItemsExternal(List<RelatedExternalItem> items)
        {
            if (items == null)
            {
                return new List<MarketplaceItemRelatedModel>();
            }

            //get list of profile items associated with this list of ids, call CloudLib to get the supporting info for these
            var matches = this.GetManyById(items.Select(x => x.ExternalSource?.ID).ToList()).Result.Data;
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
                        items.Find(z => z.ExternalSource.ID.Equals(x.ID)).RelatedTypeId,
                        _lookupItemsRelatedType.Where(z => z.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)).ToList()),
                    ExternalSource = x.ExternalSource
                }).ToList();
        }

    }

}