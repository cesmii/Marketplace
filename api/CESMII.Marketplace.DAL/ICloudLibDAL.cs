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
        /// <param name="verbose">Optional. If false, this can provide the option for the implementing class to return a subset of data with less
        ///         relational tables being loaded. For my lists, I typically don't need all the child collections, lookup table info, etc. 
        ///         This can speed stuff up when getting lists of data. </param>
        /// <returns></returns>
        Task<List<TModel>> GetAll();

        /// <summary>
        /// Provide flexibility to filter on the repo before it is converted to list so that the executed query is performant.
        /// Return List<typeparamref name="TModel"/> and the count of rows in case paging is used
        /// </summary>
        /// <param name="predicate">Linq expression</param>
        /// <param name="skip">Optional. Paging support. Start index. Note this is 0-based so first record is 0.</param>
        /// <param name="take">Optional. Paging support. Page length. </param>
        /// <param name="verbose">Optional. If false, this can provide the option for the implementing class to return a subset of data with less
        ///         relational tables being loaded. For my lists, I typically don't need all the child collections, lookup table info, etc. 
        ///         This can speed stuff up when getting lists of data. </param>
        /// <returns></returns>
        Task<List<TModel>> Where(string query);

    }


}