namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;

    public interface IDal<TEntity, TModel> : IDisposable where TEntity : AbstractEntity where TModel : AbstractModel
    {
        TModel GetById(string id);
        /// <param name="verbose">Optional. If false, this can provide the option for the implementing class to return a subset of data with less
        ///         relational tables being loaded. For my lists, I typically don't need all the child collections, lookup table info, etc. 
        ///         This can speed stuff up when getting lists of data. </param>
        /// <returns></returns>
        List<TModel> GetAll(bool verbose = false);
        /// <summary>
        /// Provide flexibility to page the query on the repo before it is converted to list so that the executed query is performant.
        /// Return List<typeparamref name="TModel"/> and the count of rows in case paging is used
        /// </summary>
        /// <param name="skip">Optional. Paging support. Start index. Note this is 0-based so first record is 0.</param>
        /// <param name="take">Optional. Paging support. Page length. </param>
        /// <param name="returnCount">Optional. Should this also execute a separate count query to get # of rows of non-paged data.  </param>
        /// <param name="verbose">Optional. If false, this can provide the option for the implementing class to return a subset of data with less
        ///         relational tables being loaded. For my lists, I typically don't need all the child collections, lookup table info, etc. 
        ///         This can speed stuff up when getting lists of data. </param>
        /// <returns></returns>
        DALResult<TModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false);
        DALResult<TModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false,
            params OrderByExpression<TEntity>[] orderByExpressions);

        Task<string> Add(TModel model, string userId);
        /// <summary>
        /// Update an entity asynchronously
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<int> Update(TModel model, string userId);

        Task<int> Delete(string id, string userId);

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
        DALResult<TModel> Where(Func<TEntity, bool> predicate, int? skip, int? take, bool returnCount = false, bool verbose = false);

        DALResult<TModel> Where(Func<TEntity, bool> predicate, int? skip, int? take, bool returnCount = false, bool verbose = false,
            params OrderByExpression<TEntity>[] orderByExpressions);

        /// <summary>
        /// Build a list of where clauses and a list of order by expressions and pass those into DAL and then to repo. 
        /// Repo will build up a query and then execute with all where clauses and order by conditions included. 
        /// Return List<typeparamref name="TModel"/> and the count of rows in case paging is used
        /// </summary>
        /// <param name="predicates">Collection of Linq expressions</param>
        /// <param name="skip">Optional. Paging support. Start index. Note this is 0-based so first record is 0.</param>
        /// <param name="take">Optional. Paging support. Page length. </param>
        /// <param name="verbose">Optional. If false, this can provide the option for the implementing class to return a subset of data with less
        ///         relational tables being loaded. For my lists, I typically don't need all the child collections, lookup table info, etc. 
        ///         This can speed stuff up when getting lists of data. </param>
        /// <returns></returns>
        DALResult<TModel> Where(List<Func<TEntity, bool>> predicates, int? skip, int? take, bool returnCount = false, bool verbose = false,
            params OrderByExpression<TEntity>[] orderByExpressions);

        /// <summary>
        /// Get a count - if predicate is null, get all count. Otherwise, get count of mathcing items
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        long Count(Func<TEntity, bool> predicate);
        long Count(List<Func<TEntity, bool>> predicates);
        long Count();
    }


}