namespace CESMII.Marketplace.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using CESMII.Marketplace.Data.Entities;
    /// <summary>
    /// Mongo DB Repo - Get collection (TEntity) from Mongo DB and interact with 
    /// that collection. This uses a similar structure to our EF core implementation.
    /// </summary>
    /// <remarks>
    ///TBD - should we return IFindFluent or List<TEntity>
    ///TBD - should we pass into this params for order by, paging so we reduce churn on DB
    /// </remarks>
    /// <typeparam name="TEntity"></typeparam>
    public interface IMongoRepository<TEntity> : IDisposable where TEntity : AbstractEntity
    {
        TEntity GetByID(string id);

        /// <summary>
        /// Get all entities in a given collection. 
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>A list of the type passed in.</returns>
        List<TEntity> GetAll();

        /// <summary>
        /// Get all entities in a given collection with paging. 
        /// </summary>
        /// <remarks>
        /// Note the orderByExpressions are an array of epxressions that would allow the collection to be sorted
        /// prior to applying paging. 
        /// </remarks>
        /// <returns>A list of the type passed in.</returns>
        List<TEntity> GetAll(int? skip, int? take, params Expression<Func<TEntity, object>>[] orderByExpressions);

        /// <summary>
        /// Find entities in a given collection matching the criteria passed in the expression. 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        List<TEntity> AggregateMatch(MongoDB.Driver.FilterDefinition<TEntity> filter, MongoDB.Driver.ProjectionDefinition<TEntity> fieldList = null);

        /// <summary>
        /// Find entities in a given collection matching the criteria passed in the expression. 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        List<TEntity> FindByCondition(Func<TEntity, bool> predicate);

        /// <summary>
        /// Find entities in a given collection matching the criteria passed in the expression. 
        /// Note this also allows for paging. The order by expressions are used to sort the results prior to applying the paging.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        List<TEntity> FindByCondition(Func<TEntity, bool> predicate, int? skip, int? take, params Expression<Func<TEntity, object>>[] orderByExpressions);

        List<TEntity> FindByCondition(Func<TEntity, bool> predicate, int? skip, int? take,
            params OrderByExpression<TEntity>[] orderByExpressions);

        /// <summary>
        /// Find entities in a given collection matching the collection of criteria passed in the list of filters. 
        /// Note this also allows for paging. The order by expressions are used to sort the results prior to applying the paging.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        List<TEntity> FindByCondition(List<Func<TEntity, bool>> predicates, int? skip, int? take, params Expression<Func<TEntity, object>>[] orderByExpressions);

        List<TEntity> FindByCondition(List<Func<TEntity, bool>> predicates, int? skip, int? take,
            params OrderByExpression<TEntity>[] orderByExpressions);

        /// <summary>
        /// Add an entry to the database set of <T>MongoAbstractEntity</T>
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        /// <returns>ID of the new item</returns>
        Task<string> AddAsync(TEntity entity);

        /// <summary>
        /// Add an entry to the database set of <T>MongoAbstractEntity</T>
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        /// <returns>ID of the new item</returns>
        string Add(TEntity entity);

        /// <summary>
        /// Update an entry.
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        /// <returns>Task for update. Return the number of records.</returns>
        void Update(TEntity entity);

        /// <summary>
        /// Update an entry async.
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        /// <returns>Task for update. Imagine this should return the number of records.</returns>
        Task UpdateAsync(TEntity entity);

        Task<int> Delete(TEntity entity);

        /// <summary>
        /// Get an estimated count of all documents. 
        /// </summary>
        /// <returns></returns>
        long Count();

        /// <summary>
        /// Get a count of documents matching the expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        long Count(Func<TEntity, bool> predicate);

        /// <summary>
        /// Get a count of documents matching the collection of expressions
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        long Count(List<Func<TEntity, bool>> predicates);


    }

}