﻿namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using NLog;

    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.Common.Models;
    using CESMII.Marketplace.Data.Repositories;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;

    using Opc.Ua.CloudLib.Client;

    /// <summary>
    /// Most lookup data is contained in this single entity and differntiated by a lookup type. 
    /// </summary>
    public class CloudLibDAL : ICloudLibDAL<MarketplaceItemModel>
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly CloudLibClient.ICloudLibWrapper _cloudLib;
        private readonly MarketplaceItemConfig _config;
        private readonly LookupItemModel _smItemType;

        //supporting data
        protected List<ImageItemModel> _images;

        public CloudLibDAL(CloudLibClient.ICloudLibWrapper cloudLib,
            IMongoRepository<LookupItem> repoLookup,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<ImageItem, ImageItemModel> dalImages,
            ConfigUtil configUtil)
        {
            _cloudLib = cloudLib;

            //init some stuff we will use during the mapping methods
            _config = configUtil.MarketplaceSettings.SmProfile;

            //get SM Profile type
            _smItemType = dalLookup.Where(
                x => x.LookupType.EnumValue.Equals(LookupTypeEnum.SmItemType) &&
                x.ID.Equals(_config.TypeId)
                , null, null, false, false).Data.FirstOrDefault();

            //get default images
            _images = dalImages.Where(
                x => x.ID.Equals(_config.DefaultImageIdLandscape) ||
                x.ID.Equals(_config.DefaultImageIdPortrait) ||
                x.ID.Equals(_config.DefaultImageIdSquare)
                , null, null, false, false).Data;
        }

        public async Task<MarketplaceItemModel> GetById(string id) {
            var entity = await _cloudLib.GetById(id);
            if (entity == null) return null;
            //manually assign id
            return MapToModelNamespace(entity, id);
        }

        public async Task<List<MarketplaceItemModel>> GetAll() {
            var result = await this.Where(null);
            return result;
        }

        public async Task<List<MarketplaceItemModel>> Where(string query)
        {
            //inject wildcard to get all if null string
            query = string.IsNullOrEmpty(query) ? "*" : query;
            //Note - splitting out each word in query into a separate string in the list
            var matches = await _cloudLib.Search(string.IsNullOrEmpty(query) ? new List<string>() : query.Split(" ").ToList());
            if (matches.Count == 0) return new List<MarketplaceItemModel>();
            return MapToModelsNodesetResult(matches);
        }

        protected List<MarketplaceItemModel> MapToModelsNodesetResult(List<UANodesetResult> entities)
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
        protected MarketplaceItemModel MapToModelNodesetResult(UANodesetResult entity)
        {
            if (entity != null)
            {
                //map results to a format that is common with marketplace items
                return new MarketplaceItemModel()
                {
                    ID = entity.Id.ToString(),
                    Name = entity.Id.ToString(),  //in marketplace items, name is used for navigation in friendly url
                    ExternalAuthor = entity.Contributor,
                    Publisher = new PublisherModel() { DisplayName = entity.Contributor, Name = entity.Contributor },
                    //TBD
                    //Description = "Description..." + entity.Title,
                    DisplayName = entity.Title,
                    Namespace = entity.NameSpaceUri.ToString(),
                    PublishDate = entity.CreationTime,
                    Type = _smItemType,
                    Version = entity.Version,
                    ImagePortrait = _images.Where(x => x.ID.Equals(_config.DefaultImageIdPortrait)).FirstOrDefault(),
                    ImageSquare = _images.Where(x => x.ID.Equals(_config.DefaultImageIdSquare)).FirstOrDefault(),
                    ImageLandscape = _images.Where(x => x.ID.Equals(_config.DefaultImageIdLandscape)).FirstOrDefault()
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
        protected MarketplaceItemModel MapToModelNamespace(UANameSpace entity, string id)
        {
            if (entity != null)
            {
                //map results to a format that is common with marketplace items
                return new MarketplaceItemModel()
                {
                    ID = id,
                    Name = id,  //in marketplace items, name is used for navigation in friendly url
                    //Abstract = ns.Title,
                    ExternalAuthor = entity.Contributor.Name,
                    Publisher = new PublisherModel()
                    {
                        DisplayName = entity.Contributor.Name,
                        Name = entity.Contributor.Name,
                        CompanyUrl = entity.Contributor.Website?.PathAndQuery,
                        Description = entity.Contributor.Description
                    },
                    //TBD
                    Description = entity.Description,
                    DisplayName = entity.Title,
                    Namespace = entity.Nodeset.NamespaceUri.ToString(),
                    MetaTags = entity.Keywords.ToList(),
                    Categories = entity.Category == null ? null : new System.Collections.Generic.List<LookupItemModel>() {
                    new LookupItemModel() {Name = entity.Category.Name}},
                    //PublishDate = ns.,
                    Type = _smItemType,
                    //Version = ns.Version,
                    ImagePortrait = _images.Where(x => x.ID.Equals(_config.DefaultImageIdPortrait)).FirstOrDefault(),
                    ImageSquare = _images.Where(x => x.ID.Equals(_config.DefaultImageIdSquare)).FirstOrDefault(),
                    ImageLandscape = _images.Where(x => x.ID.Equals(_config.DefaultImageIdLandscape)).FirstOrDefault()
                };
            }
            else
            {
                return null;
            }

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