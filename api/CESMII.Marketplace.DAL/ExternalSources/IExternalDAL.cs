using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL.ExternalSources.Models;

namespace CESMII.Marketplace.DAL.ExternalSources
{
    public interface IExternalDAL<TModel> : IDisposable where TModel : AbstractModel 
    {
        Task<TModel> GetById(string id);

        /// <summary>
        /// Download the nodeset xml portion of the CloudLib profile
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ProfileItemExportModel> Export(string id);

        /// <summary>
        /// Get a list of items by passing a list of ids to requestor.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<DALResultWithSource<TModel>> GetManyById(List<string> id);

        Task<DALResultWithSource<TModel>> GetAll();

        /// <summary>
        /// Query is from a free form input box. - This will be appended to a single keywords list
        /// Processes is from a checkboxlist - This will be appended to a single keywords list
        /// Verticals is from a checkboxlist - This will be appended to a single keywords list
        /// </summary>
        /// <remarks>There is no concept in http endpoint of industry verts or process categories so all of these items
        /// are being sent into the generic keywords search which searches on lots of stuff. 
        /// </remarks>
        /// <param name="query"></param>
        /// <param name="ids"></param>
        /// <param name="processes"></param>
        /// <param name="verticals"></param>
        /// <returns></returns>
        Task<DALResultWithSource<TModel>> Where(string query, SearchCursor cursor, 
            List<string> ids = null, List<string> processes = null, List<string> verticals = null, List<string> exclude = null);
    }
}