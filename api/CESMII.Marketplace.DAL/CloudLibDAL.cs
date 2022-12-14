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

    /// <summary>
    /// Most lookup data is contained in this single entity and differntiated by a lookup type. 
    /// </summary>
    public class CloudLibDAL : ICloudLibDAL<MarketplaceItemModel>
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ICloudLibWrapper _cloudLib;
        private readonly MarketplaceItemConfig _config;
        private readonly LookupItemModel _smItemType;

        //supporting data
        protected List<ImageItemModel> _images;

        public CloudLibDAL(ICloudLibWrapper cloudLib,
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
                x.ID.Equals(_config.DefaultImageIdPortrait) 
                //|| x.ID.Equals(_config.DefaultImageIdSquare)
                , null, null, false, false).Data;
        }

        public async Task<MarketplaceItemModel> GetById(string id) {
            var entity = await _cloudLib.DownloadAsync(id);
            //var entity = await _cloudLib.GetById(id);
            if (entity == null) return null;
            return MapToModelNamespace(entity);
        }

        public async Task<ProfileItemExportModel> Export(string id)
        {
            var entity = await _cloudLib.DownloadAsync(id);
            if (entity == null) return null;
            //return the whole thing because we also email some info to request info and use
            //other data in this entity.
            var result = new ProfileItemExportModel()
            {
                Item = MapToModelNamespace(entity),
                NodesetXml = entity.Nodeset?.NodesetXml
            };
            return result;
        }

        public async Task<List<MarketplaceItemModel>> GetAll() {
            var result = await this.Where(null);
            return result;
        }

        public async Task<List<MarketplaceItemModel>> Where(string query,
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

            var matches = await _cloudLib.SearchAsync(null, null, false, keywords, exclude);
            if (matches ==null || matches.Edges == null) return new List<MarketplaceItemModel>();

            //TBD - exclude some nodesets which are core nodesets - list defined in appSettings


            return MapToModelsNodesetResult(matches.Edges);
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
                    Type = _smItemType,
                    Version = entity.Node.Version,
                    IsFeatured = false,
                    ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)),
                    //ImageSquare = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdSquare)),
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
        protected MarketplaceItemModel MapToModelNamespace(UANameSpace entity)
        {
            if (entity != null)
            {
                //metatags
                var metatags = entity.Keywords.ToList();
                //if (entity.Category != null) metatags.Add(entity.Category.Name);

                //map results to a format that is common with marketplace items
                return new MarketplaceItemModel()
                {
                    ID = entity.Nodeset.Identifier.ToString(),
                    Name = entity.Nodeset.Identifier.ToString(),  //in marketplace items, name is used for navigation in friendly url
                    //Abstract = ns.Title,
                    ExternalAuthor = entity.Contributor.Name,
                    Publisher = new PublisherModel()
                    {
                        DisplayName = entity.Contributor.Name,
                        Name = entity.Contributor.Name,
                        CompanyUrl = entity.Contributor.Website?.ToString(),
                        Description = entity.Contributor.Description,
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
                    Namespace = entity.Nodeset.NamespaceUri?.ToString(),
                    MetaTags = metatags,
                    PublishDate = entity.Nodeset.PublicationDate,
                    Type = _smItemType,
                    Version = entity.Nodeset.Version,
                    IsFeatured = false,
                    ImagePortrait = entity.IconUrl == null ? 
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)) :
                        new ImageItemModel() { Src= entity.IconUrl.ToString()},
                    //ImageSquare = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdSquare)),
                    ImageLandscape = entity.IconUrl == null ?
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape)) :
                        new ImageItemModel() { Src = entity.IconUrl.ToString() },
                    Updated = entity.Nodeset.LastModifiedDate
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