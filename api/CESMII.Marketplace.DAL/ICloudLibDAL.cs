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
        Task<string> Export(string id);

        Task<List<TModel>> GetAll();

        Task<List<TModel>> Where(string query);

    }


}