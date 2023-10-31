using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL.ExternalSources.Models;
using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.DAL.ExternalSources
{
    public interface IAdminExternalDAL<TModel> : IExternalDAL<TModel> where TModel : AbstractModel
    {
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
        Task<int> Delete(ExternalSourceSimple source, string userId);
    }
}