using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Data.Repositories;

namespace CESMII.Marketplace.DAL.ExternalSources
{
    public interface IExternalSourceFactory<TModel> where TModel : AbstractModel
    {
        Task<IExternalDAL<TModel>> InitializeSource(ExternalSourceModel model);
        Task<IAdminExternalDAL<TModel>> InitializeAdminSource(ExternalSourceModel model);
    }


    public class ExternalSourceFactory<TModel> : IExternalSourceFactory<TModel> where TModel : AbstractModel
    {
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger<ExternalSourceFactory<TModel>> _logger;
        //protected readonly ILogger<SourceFactory> _logger;
        protected readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;

        public ExternalSourceFactory(
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration,
            //ILogger<SourceFactory> logger,
            ILogger<ExternalSourceFactory<TModel>> logger,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource
        )
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _logger = logger;
            _dalExternalSource = dalExternalSource;
        }

        /// <summary>
        /// Return the external source so that we can call the methods to get data
        /// </summary>
        public async Task<IExternalDAL<TModel>> InitializeSource(ExternalSourceModel model)
        {
            //wrap in scope so that we don't lose the scope of the dependency injected objects once the 
            //web api request completes and disposes of the import service object (and its module vars)
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                //initialize scoped services for DI
                var logger = scope.ServiceProvider.GetService<ILogger<IExternalDAL<TModel>>>();
                var httpFactory = scope.ServiceProvider.GetService<IHttpApiFactory>();
                var repoImages = scope.ServiceProvider.GetService<IMongoRepository<ImageItem>>();
                var dalLookup = scope.ServiceProvider.GetService<IDal<LookupItem, LookupItemModel>>();
                var repoMarketplace = scope.ServiceProvider.GetService<IMongoRepository<MarketplaceItem>>();
                var repoExternalItem = scope.ServiceProvider.GetService<IMongoRepository<ProfileItem>>();

                try
                {
                    //instantiate external dal class
                    return InstantiateItem(model.TypeName,
                        model,
                        _dalExternalSource,
                        httpFactory,
                        repoImages,
                        dalLookup,
                        repoMarketplace,
                        repoExternalItem);
                }
                catch (Exception e)
                {
                    //log complete message to logger and abbreviated message to user. 
                    _logger.LogCritical(e, $"SourceFactory|InstantiateSource|ExternalSourceId:{model.ID}||ExternalSourceType:{model.TypeName}|Error|{e.Message}");
                    throw;
                }
            }
        }

        public async Task<IAdminExternalDAL<TModel>> InitializeAdminSource(ExternalSourceModel model)
        {
            //wrap in scope so that we don't lose the scope of the dependency injected objects once the 
            //web api request completes and disposes of the import service object (and its module vars)
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                //initialize scoped services for DI
                var logger = scope.ServiceProvider.GetService<ILogger<IExternalDAL<TModel>>>();
                var httpFactory = scope.ServiceProvider.GetService<IHttpApiFactory>();
                var repoImages = scope.ServiceProvider.GetService<IMongoRepository<ImageItem>>();
                var dalLookup = scope.ServiceProvider.GetService<IDal<LookupItem, LookupItemModel>>();
                var repoMarketplace = scope.ServiceProvider.GetService<IMongoRepository<MarketplaceItem>>();
                var repoExternalItem = scope.ServiceProvider.GetService<IMongoRepository<ProfileItem>>();

                try
                {
                    //instantiate external dal class
                    return InstantiateAdminItem(model.AdminTypeName,
                        model,
                        _dalExternalSource,
                        httpFactory,
                        repoImages,
                        dalLookup,
                        repoMarketplace,
                        repoExternalItem);
                }
                catch (Exception e)
                {
                    //log complete message to logger and abbreviated message to user. 
                    _logger.LogCritical(e, $"SourceFactory|InstantiateSource|ExternalSourceId:{model.ID}||ExternalSourceType:{model.TypeName}|Error|{e.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Instantiate an external dal source. Expect the type name to result in a IExternalDAL type. 
        /// </summary>
        private IExternalDAL<TModel> InstantiateItem(string typeName, params object?[]? args)
        {
            //instantiate item.
            var itemType = Type.GetType(typeName);
            if (itemType == null)
            {
                _logger.LogWarning($"SourceFactory|InstantiateItem|Could not find external source class: {typeName}");
                throw new ArgumentException($"Invalid external source instance class {typeName}");
            }

            return (IExternalDAL<TModel>)Activator.CreateInstance(itemType, args);
        }

        /// <summary>
        /// Instantiate an external dal source. Expect the type name to result in a IExternalDAL type. 
        /// </summary>
        private IAdminExternalDAL<TModel> InstantiateAdminItem(string typeName, params object?[]? args)
        {
            //instantiate item.
            var itemType = Type.GetType(typeName);
            if (itemType == null)
            {
                _logger.LogWarning($"SourceFactory|InstantiateAdminItem|Could not find external source class: {typeName}");
                throw new ArgumentException($"Invalid external source instance class {typeName}");
            }

            return (IAdminExternalDAL<TModel>)Activator.CreateInstance(itemType, args);
        }
    }

}
