namespace CESMII.Marketplace.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations.Schema; 
    using Microsoft.Extensions.Logging;

    using MongoDB.Driver;
    using MongoDB.Driver.Core.Events;

    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Data.Entities;

    /// <summary>
    /// Global class to maintain the Mongo DB client during the app lifetime
    /// </summary>
    public class MongoClientGlobal 
    {
        public readonly MongoClient Client;
        public readonly IMongoDatabase Database; 
        public MongoClientGlobal(ConfigUtil configUtil, ILogger<MongoClient> logger)
        {
            //https://stackoverflow.com/questions/30333925/how-do-i-log-my-queries-in-mongodb-c-sharp-driver-2-0
            //var client = new MongoClient(configUtil.MongoDBSettings.ConnectionString);
            var mongoConnectionUrl = new MongoUrl(configUtil.MongoDBSettings.ConnectionString);
            var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);
            mongoClientSettings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e =>
                {
                    if (!e.CommandName.Equals("isMaster") && !e.CommandName.Equals("buildInfo")
                     && !e.CommandName.Equals("saslStart") && !e.CommandName.Equals("saslContinue"))
                    {
                        logger.LogInformation($"{e.Command.ToString()}");
                    }
                });
            };
            //set public property
            Client = new MongoClient(mongoClientSettings);
            Database = Client.GetDatabase(configUtil.MongoDBSettings.DatabaseName);
        }
    }

    public class MongoRepository<TEntity>: IMongoRepository<TEntity> where TEntity : AbstractEntity
    {
        protected readonly ILogger<MongoRepository<TEntity>> _logger; 
        private readonly IMongoCollection<TEntity> _collection;
        private readonly string _collectionName;
        protected bool _disposed = false;

        //get path of the executing data dll and the hardcoded files live in a folder called mock data 
        //string _path = System.Reflection.Assembly.GetExecutingAssembly().AssemblyDirectory();
        //string _collectionName = typeof(TEntity).ToString();

        public MongoRepository(MongoClientGlobal gClient, ILogger<MongoRepository<TEntity>> logger) 
        //public MongoRepository(MongoClientGlobal gClient, ConfigUtil configUtil, ILogger<MongoRepository<TEntity>> logger) 
        {
            _logger = logger;

            /* moved initialization of client and DB to a global client that lasts lifetime of app
            //https://stackoverflow.com/questions/30333925/how-do-i-log-my-queries-in-mongodb-c-sharp-driver-2-0
            //var client = new MongoClient(configUtil.MongoDBSettings.ConnectionString);
            var mongoConnectionUrl = new MongoUrl(configUtil.MongoDBSettings.ConnectionString);
            var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);
            mongoClientSettings.ClusterConfigurator = cb => {
                cb.Subscribe<CommandStartedEvent>(e => {
                    if (!e.CommandName.Equals("isMaster") && !e.CommandName.Equals("buildInfo") 
                     && !e.CommandName.Equals("saslStart") && !e.CommandName.Equals("saslContinue"))
                    {
                        _logger.LogInformation($"{e.Command.ToString()}");
                        Console.WriteLine(e.Command.ToString());
                    }
                });
            };
            
            var client = new MongoClient(mongoClientSettings);
            var database = client.GetDatabase(configUtil.MongoDBSettings.DatabaseName);
            */

            //generate the collection name based on TEntity OR the Table Attribute.
            //Using table attribute allows us to have a simple version of an entity and a more extensive version
            //of entity. Think of imageItem. sometimes I don't want the src which is large. I just want id, content type, etc. 
            var tAttribute = typeof(TEntity).CustomAttributes.Where(x => x.AttributeType.Equals(typeof(TableAttribute))).FirstOrDefault();
            if (tAttribute != null)
            {
                _collectionName = tAttribute.ConstructorArguments[0].Value.ToString();
            }
            else
            {
                string[] entityNameParts = typeof(TEntity).ToString().Split(".");  //ie: CESMII.Marketplace.Data.Entities.MarketplaceItem
                _collectionName = entityNameParts[entityNameParts.Length - 1];  //get the last item and use that as the collection name
            }

            //get the collection for use downstream
            //_collection = database.GetCollection<TEntity>(_collectionName); 
            _collection = gClient.Database.GetCollection<TEntity>(_collectionName);
        }

        public List<TEntity> GetAll()
        {
            return _collection.Find(TEntity => true).ToList();
        }

        public List<TEntity> GetAll(int? skip, int? take, params Expression<Func<TEntity, object>>[] orderByExpressions)
        {
            var result = _collection.Find(TEntity => true);
            //append n sort by expressions (could be none)
            //apply sorts prior to applying pageing
            foreach (var expr in orderByExpressions)
            {
                result = result.SortBy(expr);
            }
            return result.Skip(skip).Limit(take).ToList();
        }

        public List<TEntity> AggregateMatch(FilterDefinition<TEntity> filter)
        //public List<TEntity> FindByCondition(Expression<Func<TEntity, bool>> predicate)
        {
            //calling it this way so that the repo will accept .Any syntax. The find syntax commented out operates 
            //slightly different in how it forms the query
            return _collection.Aggregate().Match(filter).ToList();
        }

        public List<TEntity> FindByCondition(Func<TEntity, bool> predicate)
        //public List<TEntity> FindByCondition(Expression<Func<TEntity, bool>> predicate)
        {
            //calling it this way so that the repo will accept .Any syntax. The find syntax commented out operates 
            //slightly different in how it forms the query
            IQueryable<TEntity> query = _collection.AsQueryable();
            return query.Where(predicate).ToList();
            //return _collection.Find(predicate).ToList();
        }

        public List<TEntity> FindByCondition(Func<TEntity, bool> predicate, int? skip, int? take,
            params Expression<Func<TEntity, object>>[] orderByExpressions)
        {
            var predicates = new List<Func<TEntity, bool>>();
            predicates.Add(predicate);
            return FindByCondition(predicates, skip, take, orderByExpressions);

        }
        public List<TEntity> FindByCondition(Func<TEntity, bool> predicate, int? skip, int? take,
            params OrderByExpression<TEntity>[] orderByExpressions)
        {
            var predicates = new List<Func<TEntity, bool>>();
            predicates.Add(predicate);
            return FindByCondition(predicates, skip, take, orderByExpressions);
        }


        public List<TEntity> FindByCondition(List<Func<TEntity, bool>> predicates, int? skip, int? take, 
            params Expression<Func<TEntity, object>>[] orderByExpressions)
        {
            var exprs = new List<OrderByExpression<TEntity>>();
            if (orderByExpressions.Count() > 0)
            {
                foreach (var expr in orderByExpressions)
                {
                    exprs.Add( new OrderByExpression<TEntity>() { Expression = expr, IsDescending = false }  );
                }
            }
            return FindByCondition(predicates, skip, take, exprs.ToArray());
        }

        public List<TEntity> FindByCondition(List<Func<TEntity, bool>> predicates, int? skip, int? take,
            params OrderByExpression<TEntity>[] orderByExpressions)
        {
            //IEnumerable<TEntity> query = _collection.AsQueryable();
            IQueryable<TEntity> query = _collection.AsQueryable();

            //append n where clauses go through the list of expressions and build up a query
            if (predicates != null)
            {
                foreach (var predicate in predicates)
                {
                    query = query.Where(predicate).AsQueryable();
                }
            }

            //append n sort by expressions (could be none)
            //apply sorts prior to applying paging
            //IOrderedEnumerable<TEntity> result = (IOrderedEnumerable<TEntity>)query;
            ApplyOrderByExpressions(ref query, orderByExpressions);

            //apply paging
            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }
            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }
            return query.ToList();
        }

        public TEntity GetByID(string id)
        {
            return _collection.Find(x => x.ID.Equals(id)).FirstOrDefault();
        }

        //TBD - how do we get the id of the inserted value?
        public async Task<string> AddAsync(TEntity entity)
        {
            await _collection.InsertOneAsync(entity);
            return entity.ID;
        }

        //TBD - how do we get the id of the inserted value?
        public string Add(TEntity entity)
        {
            _collection.InsertOne(entity);
            return entity.ID;
        }

        public async Task UpdateAsync(TEntity entity)
        {
            await _collection.ReplaceOneAsync(TEntity => TEntity.ID.Equals(entity.ID), entity);
        }

        public void Update(TEntity entity)
        {
            _collection.ReplaceOne(TEntity => TEntity.ID.Equals(entity.ID), entity);
        }

        //TBD - return value?
        public async Task<int> Delete(TEntity entity)
        {
            await _collection.DeleteOneAsync(TEntity => TEntity.ID.Equals(entity.ID));
            return 1;
        }

        public long Count()
        {
            return _collection.EstimatedDocumentCount();
        }

        public long Count(Func<TEntity, bool> predicate)
        {
            var predicates = new List<Func<TEntity, bool>>();
            predicates.Add(predicate);
            return Count(predicates);
            //return _collection.CountDocuments(predicate);
        }

        public long Count(List<Func<TEntity, bool>> predicates)
        {
            IQueryable<TEntity> query = _collection.AsQueryable();

            //append n where clauses go through the list of expressions and build up a query
            if (predicates != null)
            {
                foreach (var predicate in predicates)
                {
                    query = query.Where(predicate).AsQueryable();
                }
            }

            return query.Count();
        }

        #region Helper functions
        protected void ApplyOrderByExpressions(ref IQueryable<TEntity> query, params OrderByExpression<TEntity>[] orderByExpressions)
        {
            //append order bys
            if (orderByExpressions == null) return;

            IOrderedQueryable<TEntity> queryOrdered = null;
            var isFirstExpr = true;
            foreach (var obe in orderByExpressions)
            {
                if (isFirstExpr)
                {
                    queryOrdered = obe.IsDescending ?
                            query.OrderByDescending(obe.Expression) :
                            query.OrderBy(obe.Expression);
                    isFirstExpr = false;
                }
                else
                {
                    queryOrdered = obe.IsDescending ?
                            queryOrdered.ThenByDescending(obe.Expression) :
                            queryOrdered.ThenBy(obe.Expression);
                }
            }
            //now convert it back to iqueryable
            query = queryOrdered == null ? query : queryOrdered.AsQueryable<TEntity>();
        }

        #endregion
        
        public virtual void Dispose()
        {
            if (_disposed) return;
            
            //clean up resources

            //set flag so we only run dispose once.
            _disposed = true;
        }


    }
}