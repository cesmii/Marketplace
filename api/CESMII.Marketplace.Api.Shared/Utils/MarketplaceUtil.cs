namespace CESMII.Marketplace.Api.Shared.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.DAL;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Extensions;
    using CESMII.Marketplace.Common.Enums;

    public class MarketplaceUtil
    {
        private readonly IDal<MarketplaceItem, MarketplaceItemModel> _dalMarketplace;
        private readonly ICloudLibDAL<MarketplaceItemModel> _dalCloudLib;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;

        public MarketplaceUtil(IDal<MarketplaceItem, MarketplaceItemModel> dalMarketplace,
            ICloudLibDAL<MarketplaceItemModel> dalCloudLib,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            IDal<LookupItem, LookupItemModel> dalLookup
            )
        {
            _dalMarketplace = dalMarketplace;
            _dalCloudLib = dalCloudLib;
            _dalAnalytics = dalAnalytics;
            _dalLookup = dalLookup;
        }

        public MarketplaceUtil(IDal<LookupItem, LookupItemModel> dalLookup)
        {
            _dalLookup = dalLookup;
        }

        public List<MarketplaceItemModel> SimilarItems(MarketplaceItemModel item)
        {
            //union passed in list w/ lookup list.
            var cats = item.Categories.Select(x => x.ID).ToArray();
            var verts = item.IndustryVerticals.Select(x => x.ID).ToArray();

            //build list of where clauses all combined into one predicate, 
            //using expression extension to allow for .Or or .And expression
            var predicates = new List<Func<MarketplaceItem, bool>>();

            //build list of where clauses - one for each cat 
            Func<MarketplaceItem, bool> predicateCat = null;
            if (cats != null && cats.Length > 0)//model.Categories != null)
            {
                foreach (var cat in cats)
                {
                    if (predicateCat == null) predicateCat = x => x.Categories.Any(c => c.ToString().Equals(cat));
                    else predicateCat = predicateCat.Or(x => x.Categories.Any(c => c.ToString().Equals(cat)));
                }
                predicates.Add(predicateCat);
            }
            //build list of where clauses - one for each industry vertical 
            Func<MarketplaceItem, bool> predicateVert = null;
            if (verts != null && verts.Length > 0)
            {
                foreach (var vert in verts)
                {
                    if (predicateVert == null) predicateVert = x => x.IndustryVerticals.Any(c => c.ToString().Equals(vert));
                    else predicateVert = predicateVert.Or(x => x.IndustryVerticals.Any(c => c.ToString().Equals(vert)));
                }
                predicates.Add(predicateVert);
            }

            //build where clause - publisher id 
            predicates.Add(x => x.PublisherId.ToString().Equals(item.Publisher.ID));

            //now combine all predicates into one predicate and use the OR extension so that we cast a wider net. 
            Func<MarketplaceItem, bool> predFinal = null;
            foreach (var pred in predicates)
            {
                if (predFinal == null) predFinal = pred;
                else predFinal = predFinal.Or(pred);
            }

            //limit to isActive
            predFinal = predFinal.And(x => x.IsActive);
            //remove self
            predFinal = predFinal.And(x => !x.ID.Equals(item.ID));
            //limit to publish status of live
            predFinal = predFinal.And(this.BuildStatusFilterPredicate());

            //TBD - future - weight certain factors more than others and potentially sort by most similar.
            //now execute the search 
            var result = _dalMarketplace.Where(predFinal, null, 30, false, false, 
                    new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
                    new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName }).Data;
            return result;
        }

        public async Task<List<MarketplaceItemModel>> PopularItems()
        {
            //get list of popular items in analytics collection
            //build list of order bys to sort result by most important factors first - this is essentially giving us most popular 
            var orderBys = new List<OrderByExpression<MarketplaceItemAnalytics>>
            {
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.DownloadCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.PageVisitCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.LikeCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.ShareCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.MoreInfoCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.DislikeCount, IsDescending = false }
            };

            var popularMarketplace = _dalAnalytics.Where(x => string.IsNullOrEmpty(x.CloudLibId), null, 4, false, false, orderBys.ToArray()).Data
                .Select(x => x.MarketplaceItemId).ToArray();
            var popularCloudLib = _dalAnalytics.Where(x => !string.IsNullOrEmpty(x.CloudLibId), null, 4, false, false, orderBys.ToArray()).Data
                .Select(x => x.CloudLibId).ToList();

            //now get the marketplace items with popular rankings
            //filter our inactive and non-live items
            var predicatesMarketplace = new List<Func<MarketplaceItem, bool>>
            {
                //limit to isActive
                x => x.IsActive,
                //limit to publish status of live
                this.BuildStatusFilterPredicate(),
                //only get items which are most popular based on previous query of analytics
                x => popularMarketplace.Any(m => m == x.ID.ToString())
            };

            var itemsMarketplace = _dalMarketplace.Where(predicatesMarketplace, null, 4, false, false).Data;

            //now get the cloudlib items with popular rankings
            var itemsCloudLib = await _dalCloudLib.Where(null, popularCloudLib, null, null, null);

            return itemsMarketplace.Union(itemsCloudLib).ToList();

        }

        public async Task<List<MarketplaceItemModel>> PopularItemsAsync()
        {
            //get list of popular items in analytics collection
            //build list of order bys to sort result by most important factors first - this is essentially giving us most popular 
            var orderBys = new List<OrderByExpression<MarketplaceItemAnalytics>>
            {
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.DownloadCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.PageVisitCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.LikeCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.ShareCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.MoreInfoCount, IsDescending = true },
                new OrderByExpression<MarketplaceItemAnalytics>() { Expression = x => x.DislikeCount, IsDescending = false }
            };

            //run in parallel
            //var itemsMarketplace = await PopularItemsMarketplace(orderBys);
            //var itemsCloudLib = await PopularItemsCloudLib(orderBys);
            
            var matchesMarketplaceTask = PopularItemsMarketplace(orderBys);
            var matchesCloudLibTask = PopularItemsCloudLib(orderBys);
            await Task.WhenAll(matchesMarketplaceTask, matchesCloudLibTask);

            //get the tasks results into format we can use
            var itemsMarketplace = await matchesMarketplaceTask;
            var itemsCloudLib = await matchesCloudLibTask;

            return itemsMarketplace.Union(itemsCloudLib).ToList();

        }

        private Task<List<MarketplaceItemModel>> PopularItemsMarketplace(List<OrderByExpression<MarketplaceItemAnalytics>> orderBys)
        {
            //run in parallel
            var popularMarketplace = _dalAnalytics.Where(x => string.IsNullOrEmpty(x.CloudLibId), null, 4, false, false, orderBys.ToArray()).Data
                .Select(x => x.MarketplaceItemId).ToArray();

            //now get the marketplace items with popular rankings
            //filter our inactive and non-live items
            var predicatesMarketplace = new List<Func<MarketplaceItem, bool>>
            {
                //limit to isActive
                x => x.IsActive,
                //limit to publish status of live
                this.BuildStatusFilterPredicate(),
                //only get items which are most popular based on previous query of analytics
                x => popularMarketplace.Any(m => m == x.ID.ToString())
            };

            //run in parallel
            var itemsMarketplace = _dalMarketplace.Where(predicatesMarketplace, null, 4, false, false).Data;
            return Task.FromResult(itemsMarketplace.ToList());
        }

        private async Task<List<MarketplaceItemModel>> PopularItemsCloudLib(List<OrderByExpression<MarketplaceItemAnalytics>> orderBys)
        {
            //run in parallel
            var popularCloudLib = _dalAnalytics.Where(x => !string.IsNullOrEmpty(x.CloudLibId), null, 4, false, false, orderBys.ToArray()).Data
                .Select(x => x.CloudLibId).ToList();
            //now get the cloudlib items with popular rankings
            return await _dalCloudLib.Where(null, popularCloudLib, null, null, null);
        }

        /// <summary>
        /// Generate a filter predicate which filters marketplace item on a status
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public Func<MarketplaceItem, bool> BuildStatusFilterPredicate(string code = "live")
        {
                var luStatusLive = _dalLookup.GetAll().Where(x => x.LookupType.EnumValue == LookupTypeEnum.MarketplaceStatus &&
                    x.Code.ToLower() == code.ToLower()).Select(x => x.ID).ToList();
                return x => luStatusLive.Any(y => y.Equals(x.StatusId.ToString()));
        }
    }
}