namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;

    public interface ICloudLibDAL<TModel> : IDisposable where TModel : AbstractModel
    {
        Task<TModel> GetById(string id);

        /// <summary>
        /// Download the nodeset xml portion of the CloudLib profile
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ProfileItemExportModel> Export(string id);

        /// <summary>
        /// Get a list of profiles by passing a list of profile ids to CloudLib.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<List<TModel>> GetManyById(List<string> id);

        Task<List<TModel>> GetAll();

        /// <summary>
        /// Query is from a free form input box. - This will be appended to a single keywords list
        /// Processes is from a checkboxlist - This will be appended to a single keywords list
        /// Verticals is from a checkboxlist - This will be appended to a single keywords list
        /// </summary>
        /// <remarks>There is no concept in CloudLib of industry verts or process categories so all of these items
        /// are being sent into the generic keywords search which searches on lots of stuff. 
        /// </remarks>
        /// <param name="query"></param>
        /// <param name="ids"></param>
        /// <param name="processes"></param>
        /// <param name="verticals"></param>
        /// <param name="exclude">List of namespace uris to exclude from results</param>
        /// <returns></returns>
        Task<DALResult<TModel>> Where(string query, int? skip =null, int? take = null, string startCursor = null, string endCursor = null, bool noTotalCount = false,
            List<string> ids = null, List<string> processes = null, List<string> verticals = null, List<string> exclude = null);

    }

    /// <summary>
    /// Admin version of CloudLib DAL
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface IAdminCloudLibDAL<TModel> : IDisposable where TModel : AbstractModel
    {
        Task<TModel> GetById(string id);

        /// <summary>
        /// Get a list of profiles by passing a list of profile ids to CloudLib.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<List<TModel>> GetManyById(List<string> id);

        Task<List<TModel>> GetAll();

        /// <summary>
        /// Query is from a free form input box. - This will be appended to a single keywords list
        /// Processes is from a checkboxlist - This will be appended to a single keywords list
        /// Verticals is from a checkboxlist - This will be appended to a single keywords list
        /// </summary>
        /// <remarks>There is no concept in CloudLib of industry verts or process categories so all of these items
        /// are being sent into the generic keywords search which searches on lots of stuff. 
        /// </remarks>
        /// <param name="query"></param>
        /// <param name="ids"></param>
        /// <param name="processes"></param>
        /// <param name="verticals"></param>
        /// <param name="exclude">List of namespace uris to exclude from results</param>
        /// <returns></returns>
        Task<List<TModel>> Where(string query, int? skip = null, int? take = null, string startCursor = null, string endCursor = null, bool noTotalCount = false,
            List<string> ids = null, List<string> processes = null, List<string> verticals = null, List<string> exclude = null);

        Task<string> Add(TModel model, string userId);
        /// <summary>
        /// Add/Update an entity asynchronously
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<int> Upsert(TModel model, string userId);

        /// <summary>
        /// Remove all relationships for this item.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<int> Delete(string id, string userId);
    }

}