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
            var mongoConnectionUrl = new MongoUrl(configUtil.MongoDBSettings.ConnectionString);
            var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);
            mongoClientSettings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e =>
                {
                    if (!e.CommandName.Equals("isMaster") && !e.CommandName.Equals("buildInfo")
                     && !e.CommandName.Equals("saslStart") && !e.CommandName.Equals("saslContinue"))
                    {
                        logger.LogInformation($"{e.Command}");
                    }
                });
            };
            //set public property
            Client = new MongoClient(mongoClientSettings);
            Database = Client.GetDatabase(configUtil.MongoDBSettings.DatabaseName);
        }
    }

    public class MongoRepository<TEntity> : IMongoRepository<TEntity> where TEntity : AbstractEntity
    {
        protected readonly ILogger<MongoRepository<TEntity>> _logger;
        private readonly IMongoCollection<TEntity> _collection;
        private readonly string _collectionName;
        protected bool _disposed = false;

        public MongoRepository(MongoClientGlobal gClient, ILogger<MongoRepository<TEntity>> logger)
        //public MongoRepository(MongoClientGlobal gClient, ConfigUtil configUtil, ILogger<MongoRepository<TEntity>> logger) 
        {
            _logger = logger;

            //generate the collection name based on TEntity OR the Table Attribute.
            //Using table attribute allows us to have a simple version of an entity and a more extensive version
            //of entity. Think of imageItem. sometimes I don't want the src which is large. I just want id, content type, etc. 
            var tAttribute = typeof(TEntity).CustomAttributes.FirstOrDefault(x => x.AttributeType.Equals(typeof(TableAttribute)));
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

        public async Task<List<TEntity>> AggregateMatchAsync(FilterDefinition<TEntity> filter, List<string> fieldList = null)
        {
            //calling it this way so that the repo will accept .Any syntax. The find syntax commented out operates 
            //slightly different in how it forms the query
            if (fieldList == null)
            {
                return await _collection.Aggregate().Match(filter).ToListAsync();
            }
            else
            {
                //performance improvement - limit columns being queried
                string fieldListString = BuildProjectionFieldList(fieldList);
                return await _collection.Aggregate().Match(filter).Project<TEntity>(fieldListString).ToListAsync();
            }

        }

        public List<TEntity> FindByCondition(Func<TEntity, bool> predicate)
        {
            //calling it this way so that the repo will accept .Any syntax. The find syntax commented out operates 
            //slightly different in how it forms the query
            IQueryable<TEntity> query = _collection.AsQueryable();
            return query.Where(predicate).ToList();
        }

        public List<TEntity> FindByCondition(Func<TEntity, bool> predicate, int? skip, int? take,
            params Expression<Func<TEntity, object>>[] orderByExpressions)
        {
            var predicates = new List<Func<TEntity, bool>>
            {
                predicate
            };
            return FindByCondition(predicates, skip, take, orderByExpressions);

        }
        public List<TEntity> FindByCondition(Func<TEntity, bool> predicate, int? skip, int? take,
            params OrderByExpression<TEntity>[] orderByExpressions)
        {
            var predicates = new List<Func<TEntity, bool>>
            {
                predicate
            };
            return FindByCondition(predicates, skip, take, orderByExpressions);
        }


        public List<TEntity> FindByCondition(List<Func<TEntity, bool>> predicates, int? skip, int? take,
            params Expression<Func<TEntity, object>>[] orderByExpressions)
        {
            var exprs = new List<OrderByExpression<TEntity>>();
            if (orderByExpressions.Any())
            {
                foreach (var expr in orderByExpressions)
                {
                    exprs.Add(new OrderByExpression<TEntity>() { Expression = expr, IsDescending = false });
                }
            }
            return FindByCondition(predicates, skip, take, exprs.ToArray());
        }

        public List<TEntity> FindByCondition(List<Func<TEntity, bool>> predicates, int? skip, int? take,
            params OrderByExpression<TEntity>[] orderByExpressions)
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

            //append n sort by expressions (could be none)
            //apply sorts prior to applying paging
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

        public async Task BulkUpsertAsync(List<TEntity> entities)
        {
            var listWrites = new List<WriteModel<TEntity>>();

            //cue up list of items
            foreach (var entity in entities)
            {
                if (string.IsNullOrEmpty(entity.ID))
                {
                    //insert statements
                    listWrites.Add(new InsertOneModel<TEntity>(entity));
                }
                else
                {
                    //update statements
                    var filterDefinition = Builders<TEntity>.Filter.Eq(x => x.ID, entity.ID.ToString());
                    listWrites.Add(new ReplaceOneModel<TEntity>(filterDefinition, entity));
                }
            }

            await _collection.BulkWriteAsync(listWrites);
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
            var predicates = new List<Func<TEntity, bool>>
            {
                predicate
            };
            return Count(predicates);
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

        private static string BuildProjectionFieldList(List<string> fieldList)
        {
            return "{" + string.Join(",", fieldList.Select(f => f + ":1").ToList()) + "}";
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