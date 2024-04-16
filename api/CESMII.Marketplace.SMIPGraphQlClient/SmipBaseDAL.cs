using Microsoft.Extensions.Logging;
using CESMII.Marketplace.SmipGraphQlClient.Models;

namespace CESMII.Marketplace.SmipGraphQlClient
{

    public class SmipBaseDAL<TModel> where TModel : SmipAbstractModel 
    {
        protected readonly ILogger _logger;
        protected readonly ISmipRepo<TModel> _repo;

        public SmipBaseDAL(SmipAuthenticatorSettings settings, ILogger logger)
        {
            _repo = new SmipRepo<TModel>(settings, logger);
            _logger = logger;
        }

        public virtual async Task Authenticate()
        {
            await _repo.Authenticate();
        }

        public virtual async Task<List<TModel>> SearchAsync(string query)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<TModel> AddAsync(TModel item)
        {
            throw new NotImplementedException();
        }
    }

}
