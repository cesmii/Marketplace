namespace CESMII.Marketplace.Data.Entities
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Mongo DB Repo - Get collection (TEntity) from Mongo DB and interact with 
    /// that collection. This uses a similar structure to our EF core implementation.
    /// </summary>
    /// <remarks>
    ///TBD - should we return IFindFluent or List<TEntity>
    ///TBD - should we pass into this params for order by, paging so we reduce churn on DB
    /// </remarks>
    /// <typeparam name="TEntity"></typeparam>
    public class OrderByExpression<TEntity> where TEntity : AbstractEntity
    {
        public Expression<Func<TEntity, object>> Expression { get; set; }
        public bool IsDescending { get; set; } = false;
    }

}