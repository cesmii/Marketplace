namespace CESMII.Marketplace.Api.Shared.Utils
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.DAL;
    using CESMII.Marketplace.DAL.ExternalSources;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Extensions;
    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.Api.Shared.Models;

    public class MarketplaceUtil
    {
        private readonly IDal<MarketplaceItem, MarketplaceItemModel> _dalMarketplace;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly IExternalSourceFactory<MarketplaceItemModel> _sourceFactory;
        private readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;

        public MarketplaceUtil(IDal<MarketplaceItem, MarketplaceItemModel> dalMarketplace,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IExternalSourceFactory<MarketplaceItemModel> sourceFactory,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource
            )
        {
            _dalMarketplace = dalMarketplace;
            _dalAnalytics = dalAnalytics;
            _dalLookup = dalLookup;
            _sourceFactory = sourceFactory;
            _dalExternalSource = dalExternalSource;
        }

        public MarketplaceUtil(IDal<LookupItem, LookupItemModel> dalLookup)
        {
            _dalLookup = dalLookup;
        }

        /// <summary>
        /// Take the existing related group by and append in the automated similar items 
        /// discovered here.
        /// </summary>
        /// <param name="item"></param>
        public void AppendSimilarItems(ref MarketplaceItemModel item)
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
            var pubId = item.Publisher.ID;
            predicates.Add(x => x.PublisherId.ToString().Equals(pubId));

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
            var id = item.ID;
            predFinal = predFinal.And(x => !x.ID.Equals(id));
            //limit to publish status of live
            predFinal = predFinal.And(this.BuildStatusFilterPredicate());

            //TBD - future - weight certain factors more than others and potentially sort by most similar.
            //now execute the search 
            var result = _dalMarketplace.Where(predFinal, null, 30, false, false, 
                    new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
                    new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName }).Data;
            //filter out any items already represented in related items collection
            var relatedItems = item.RelatedItemsGrouped.SelectMany(x => x.Items).Select(y => y.RelatedId);

            //convert to a simplified version of the marketplace item object
            var autoMatches = result
                .Where(x => !relatedItems.Contains(x.ID)) //filter out items already represented
                .Select(x => new MarketplaceItemRelatedModel() {
                //ID = x.ID,
                RelatedId = x.ID,
                Abstract = x.Abstract,
                DisplayName = x.DisplayName,
                Description = x.Description,
                Name = x.Name,
                Type = x.Type,
                Version = x.Version,
                ImagePortrait = x.ImagePortrait,
                ImageLandscape = x.ImageLandscape
            }).ToList();


            //now combine auto matches with manual matches (if there are any manual matches)
            RelatedItemsGroupBy similarGroup = item.RelatedItemsGrouped.Find(x => x.RelatedType.Code.ToLower().Equals("similar"));
            if (similarGroup != null)
            {
                similarGroup.Items = similarGroup.Items == null ? autoMatches : similarGroup.Items.Union(autoMatches).ToList();
            }
            else
            {
                var similarType = _dalLookup.Where(x => x.LookupType.EnumValue == LookupTypeEnum.RelatedType, null, null, false).Data
                                        .Find(x => (x.Code == null ? "" : x.Code).ToLower().Equals("similar"));
                //should not happen but data could be removed
                if (similarType == null)
                {
                    throw new InvalidOperationException("Missing lookup data - Related Type - Similar");
                }
                //create new similar group
                similarGroup = new RelatedItemsGroupBy()
                {
                    RelatedType = similarType,
                    Items = autoMatches
                };
                item.RelatedItemsGrouped.Add(similarGroup);
            }

            //final sort
            similarGroup.Items = similarGroup.Items
                .OrderBy(x => x.DisplayName)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Namespace)
                .ThenBy(x => x.Version)
                .ToList();
        }

        /// <summary>
        /// Parallel code execution - get top 10 items in analytics based on various internal analytics tallies.
        /// loop over the marketplace items and each external source in a parallel processing manner.
        /// Make a parallel call to get the data related to the highest ranking items for each source.
        /// </summary>
        /// <returns></returns>
        public async Task<List<MarketplaceItemModel>> PopularItemsAsync()
        {
            //var timer = Stopwatch.StartNew();

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

            //get top 10 vote getters - regardless of source, then call sources as needed to 
            //pull back items that made the list. 
            var popularItems = _dalAnalytics.Where(x => !string.IsNullOrEmpty(x.ID), 
                null, 10, false, false, orderBys.ToArray()).Data;

            //run in parallel
            var listPopularItemSources = new List<Task>();
            //add native marketplace task to list for downstream parallel execution
            listPopularItemSources.Add(PopularItemsMarketplace(popularItems.Where(x => x.ExternalSource == null).ToList()));

            var sources = _dalExternalSource.Where(x => x.Enabled && x.IsActive, null, null, false, false).Data;
            foreach (var src in sources)
            {
                if (!src.Enabled) continue;

                var externalTask = PopularItemsExternalSource(src, 
                    popularItems.Where(x => x.ExternalSource != null && x.ExternalSource.SourceId == src.ID).ToList());
                //_ = externalTask.ContinueWith(t => AdvancedSearchLogDurationTime(src.Code, timer.ElapsedMilliseconds - swExternalStart));
                listPopularItemSources.Add(externalTask);
            }

            await Task.WhenAll(listPopularItemSources);

            //get the tasks results into format we can use
            var allTasks = Task.WhenAll(listPopularItemSources);
            //wrap exception handling around the tasks execution so no task exception gets lost
            try
            {
                await allTasks;
                //AdvancedSearchLogDurationTime("When All", timer.ElapsedMilliseconds - swWhenAllStart);
            }
            catch //(Exception ex)
            {
                //_logger.LogCritical(ex, $"MarketplaceUtil|PopularItemsAsync|All Tasks Exception|{ex.Message}.");
                throw allTasks.Exception;
            }

            //loop results and combine into single result
            var result = new List<MarketplaceItemModel>();
            foreach (Task<List<MarketplaceItemModel>> t in listPopularItemSources)
            {
                var r = await t;
                result = result.Union(r).ToList();
            }

            return result.OrderBy(x => x.DisplayName).ToList();
        }

        private async Task<List<MarketplaceItemModel>> PopularItemsMarketplace(List<MarketplaceItemAnalyticsModel> matches)
        {
            var idList = matches.Select(x => x.MarketplaceItemId.ToString()).ToList();
            //now get the marketplace items with popular rankings
            //filter our inactive and non-live items
            var predicates = new List<Func<MarketplaceItem, bool>>
            {
                //limit to isActive
                x => x.IsActive,
                //limit to publish status of live
                this.BuildStatusFilterPredicate(),
                //only get items which are most popular based on previous query of analytics
                x => idList.Any(m => m == x.ID.ToString())
            };

            //run as async task
            var result = await Task.Run(() =>
            {
                return _dalMarketplace.Where(predicates, null, 4, false, false).Data;
            });
            return result;
        }

        private async Task<List<MarketplaceItemModel>> PopularItemsExternalSource(ExternalSourceModel src, List<MarketplaceItemAnalyticsModel> matches)
        {
            var idList = matches.Select(x => x.ExternalSource.ID).ToList();
            //now get the external items for this source with popular rankings
            //the external source DAL implementation may not support the external call of a list of ids,
            //check the dal if data is not coming back
            //run in parallel
            var dalSource = await _sourceFactory.InitializeSource(src);
            var result = await dalSource.GetManyById(idList);
            return result.Data;
        }

        /// <summary>
        /// Generate a filter predicate which filters marketplace item on a status
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public Func<MarketplaceItem, bool> BuildStatusFilterPredicate(List<string> statuses = null)
        {
            if (statuses == null)
            { 
                statuses = new List<string>() { "live"};
            }

            //var luStatusLive = _dalLookup.GetAll().Where(x => x.LookupType.EnumValue == LookupTypeEnum.MarketplaceStatus &&
            //        statuses.Any(y => y.Equals(x.Code.ToLower()))).Select(x => x.ID).ToList();
            var luStatusLive = _dalLookup
                .Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.MarketplaceStatus), null, null, false, false)
                .Data.Where(x => statuses.Any(y => y.Equals(x.Code.ToLower()))).Select(x => x.ID).ToList();
            return x => luStatusLive.Any(y => y.Equals(x.StatusId.ToString()));
        }

        /// <summary>
        /// For the next query we execute against each source, we need a search cursor to 
        /// help the source only pull back the necessary data. When a user pages through the 
        /// result, the starting point and ending point for the data is determined by the sort order
        /// but also by the other sources contributions. A page may contain data from multiple sources
        /// and we need to know how many rows from each source contributed to this page of data. 
        /// Also, if we have visited this page of data (go from page 2 and back to page 1), re-use that info 
        /// so that we can limit the extra processing time. 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="sourceId"></param>
        public static SearchCursor PrepareSearchCursor(MarketplaceSearchModel model, string sourceId)
        {
            var pageIndex = model.Skip / model.Take;

            //scenario - new search - no previous cached info
            //if we enter on page 4 of a new search, we still need to get pages 0-4 because
            //we won't yet know whether 1st 10 records of any source would contribute to the merged output.
            if (model.CachedCursors == null)
                return new SearchCursor()
                {
                    Skip = 0,
                    Take = model.Skip + model.Take,
                    PageIndex = pageIndex
                };

            //if source is not present, return new cursor
            var match = model.CachedCursors.Find(x => x.SourceId == sourceId);
            if (match == null || match.Cursors == null)
                return new SearchCursor()
                {
                    Skip = 0,
                    Take = model.Skip + model.Take,
                    PageIndex = pageIndex
                };

            //find current page cached cursor (ie pageIndex is same). if present, use that cursor.
            var result = match.Cursors.Find(x => x.PageIndex == pageIndex);
            if (result != null) return result;

            //else find cached cursor with previous page (ie pageIndex - 1).
            //If present, use endIndex + 1 (or endCursor) as starting point
            result = match.Cursors.Find(x => x.PageIndex == pageIndex - 1);
            if (result != null)
            {
                return new SearchCursor()
                {
                    StartCursor = result.EndCursor,
                    EndCursor = null,
                    Skip = result.Take.Value + 1,
                    Take = result.Take + 1 + model.Take,
                    TotalCount = result.TotalCount
                };
            }

            //if we get here, return new cursor
            return new SearchCursor()
            {
                Skip = 0,
                Take = model.Skip + model.Take,
                PageIndex = pageIndex
            };
        }

    }
}