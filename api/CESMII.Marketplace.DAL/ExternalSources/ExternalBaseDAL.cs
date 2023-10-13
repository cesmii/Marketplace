using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

using NLog;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Models;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL.ExternalSources.Models;
using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.DAL.ExternalSources
{
    public class ExternalBaseDAL<TEntity, TModel> where TEntity : ExternalAbstractEntity where TModel : AbstractModel
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        protected readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;
        protected readonly ExternalSourceModel _config;
        protected readonly IHttpApiFactory _httpFactory;
        protected readonly string _baseAddress;

        public ExternalBaseDAL(
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource, 
            string externalSourceCodeName,
            IHttpApiFactory httpFactory)
        {
            _dalExternalSource = dalExternalSource;
            //go get the config for this source
            _config = dalExternalSource.Where(x => x.Code.ToLower().Equals(externalSourceCodeName.ToLower())
                , null, null, false, true).Data?.FirstOrDefault();
            if (_config == null)
            {
                throw new ArgumentNullException($"External Source Config: {externalSourceCodeName}");
            }

            _httpFactory = httpFactory;
        }

        public ExternalBaseDAL(
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            ExternalSourceModel config,
            IHttpApiFactory httpFactory)
        {
            //go get the config for this source
            _dalExternalSource = dalExternalSource;
            _config = config;
            _httpFactory = httpFactory;
        }

        protected async Task<HttpResponseModel> ExecuteApiCall(HttpApiConfig config)
        {
            //execute the https call
            string responseRaw = await _httpFactory.Run(config);
            return new HttpResponseModel() { Data = responseRaw, IsSuccess = true };
        }

        /// <summary>
        /// Create the API config to direct the API how to execute
        /// </summary>
        /// <remarks>Implement in descendant classes</remarks>
        /// <param name="url"></param>
        /// <param name="formData"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual HttpApiConfig PrepareApiConfig(string url, MultipartFormDataContent formData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prepare a list of headers to include in the API call
        /// </summary>
        /// <remarks>Implement in descendant classes if needed.</remarks>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual List<KeyValuePair<string, string>> PrepareHeaders()
        {
            return null;
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        /// <remarks>Verbose is intended to map more of the related data. Each DAL 
        /// can determine how much is enough</remarks>
        protected virtual TModel MapToModel(TEntity entity, bool verbose = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        /// <remarks>Verbose is intended to map more of the related data. Each DAL 
        /// can determine how much is enough. Other DALs may choose to not use and keep the 
        /// mapping the same between getById and GetAll/Where calls.</remarks>
        protected virtual List<TModel> MapToModels(List<TEntity> entities, bool verbose = false)
        {
            var result = new List<TModel>();

            foreach (var item in entities)
            {
                result.Add(MapToModel(item, verbose));
            }
            return result;
        }

        /// <summary>
        /// Take n related sets, group them by type and union them, filter them by related type and order them
        /// </summary>
        /// <param name="items">one or many lists of related items</param>
        /// <returns></returns>
        protected List<RelatedItemsGroupBy> GroupAndMergeRelatedItems(params List<MarketplaceItemRelatedModel>[] items)
        {
            if (items == null) return new List<RelatedItemsGroupBy>();

            //group by each set and then merge
            var result = new List<RelatedItemsGroupBy>();

            //convert group to return type
            foreach (var list in items)
            {
                if (list?.Count > 0)
                {
                    var grpItems = list.GroupBy(x => new { ID = x.RelatedType.ID });
                    foreach (var item in grpItems)
                    {
                        var matches = list.Where(x => x.RelatedType.ID.Equals(item.Key.ID)).ToList();

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

        public virtual void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            //set flag so we only run dispose once.
            _disposed = true;
        }
    }
}