using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Data.Extensions;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.Api.Shared.Utils;
using CESMII.Marketplace.DAL.ExternalSources;


namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class MarketplaceController : BaseController<MarketplaceController>
    {
        private readonly IDal<MarketplaceItem, MarketplaceItemModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly IDal<Publisher, PublisherModel> _dalPublisher;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;
        private readonly IExternalDAL<MarketplaceItemModel> _dalCloudLib;
        private readonly IDal<SearchKeyword, SearchKeywordModel> _dalSearchKeyword;
        private readonly IExternalSourceFactory<MarketplaceItemModel> _sourceFactory;
        private readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;

        public MarketplaceController(IDal<MarketplaceItem, MarketplaceItemModel> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<Publisher, PublisherModel> dalPublisher,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            IExternalDAL<MarketplaceItemModel> dalCloudLib,
            IDal<SearchKeyword, SearchKeywordModel> dalSearchKeyword,
            UserDAL dalUser,
            IExternalSourceFactory<MarketplaceItemModel> sourceFactory,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            ConfigUtil config, ILogger<MarketplaceController> logger)
            : base(config, logger, dalUser)
        {
            _dal = dal;
            _dalPublisher = dalPublisher;
            _dalLookup = dalLookup;
            _dalAnalytics = dalAnalytics;
            _dalCloudLib = dalCloudLib;
            _dalSearchKeyword = dalSearchKeyword;

            _sourceFactory = sourceFactory;
            _dalExternalSource = dalExternalSource; 
        }

        [HttpGet, Route("All")]
        [ProducesResponseType(200, Type = typeof(List<MarketplaceItemModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetAll()
        {
            var result = _dal.GetAll();
            if (result == null)
            {
                _logger.LogWarning("MarketplaceController|GetAll|No records found.");
                return BadRequest($"No records found.");
            }
            return Ok(result);
        }


        #region Home Page

        [HttpPost, Route("home")]
        [ProducesResponseType(200, Type = typeof(MarketplaceHomeModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Home()
        {
            var result = new MarketplaceHomeModel
            {
                FeaturedItems = _dal.Where(x => x.IsFeatured && x.IsActive, null, null, false, false).Data,
                //trim down to 4 most recent 
                NewItems = _dal.Where(x => x.IsActive, null, 4, false, false,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.PublishDate, IsDescending = true }).Data
            };
            //calculate most popular based on analytics counts
            var util = new MarketplaceUtil(_dal, _dalCloudLib, _dalAnalytics, _dalLookup);
            result.PopularItems = await util.PopularItems();

            return Ok(result);
        }

        [HttpPost, Route("featured")]
        [ProducesResponseType(200, Type = typeof(List<MarketplaceItemModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetFeatured()
        {
            var predicates = new List<Func<MarketplaceItem, bool>>
            {
                //limit to featured and isActive
                x => x.IsFeatured && x.IsActive
            };
            //limit to publish status of live
            var util = new MarketplaceUtil(_dal, _dalCloudLib, _dalAnalytics, _dalLookup);
            predicates.Add(util.BuildStatusFilterPredicate());

            var result = _dal.Where(predicates, null, null, false, false,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.PublishDate, IsDescending = true }).Data;
            return Ok(result);
        }

        [HttpPost, Route("recent")]
        [ProducesResponseType(200, Type = typeof(List<MarketplaceItemModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetRecentItems()
        {
            var predicates = new List<Func<MarketplaceItem, bool>>
            {
                //limit to isActive
                x => x.IsActive
            };
            //limit to publish status of live
            var util = new MarketplaceUtil(_dal, _dalCloudLib, _dalAnalytics, _dalLookup);
            predicates.Add(util.BuildStatusFilterPredicate());

            //trim down to 4 most recent 
            var result = _dal.Where(predicates, null, 4, false, false,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.PublishDate, IsDescending = true }).Data;
            return Ok(result);
        }

        [HttpPost, Route("popular")]
        [ProducesResponseType(200, Type = typeof(List<MarketplaceItemModel>))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetPopular()
        {
            //calculate most popular based on analytics counts
            var util = new MarketplaceUtil(_dal, _dalCloudLib, _dalAnalytics, _dalLookup);
            var result = await util.PopularItems();
            return Ok(result);
        }
        #endregion

        [HttpPost, Route("GetByID")]
        [ProducesResponseType(200, Type = typeof(MarketplaceItemModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdStringWithTrackingModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"MarketplaceController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            MarketplaceItemAnalyticsModel analytic = null;
            if (result == null)
            {
                _logger.LogWarning($"MarketplaceController|GetById|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            if (model.IsTracking)
            {
                //Increment Page Count
                //Check if MpItem is there if not add a new one then increment count and save
                analytic = _dalAnalytics.Where(x => x.MarketplaceItemId.ToString() == model.ID, null, null, false).Data.FirstOrDefault();

                if (analytic == null)
                {
                    analytic = new MarketplaceItemAnalyticsModel() { MarketplaceItemId = model.ID, PageVisitCount = 1 };
                    _dalAnalytics.Add(analytic, null);

                }
                else
                {
                    analytic.PageVisitCount += 1;
                    _dalAnalytics.Update(analytic, model.ID);
                }
                result.Analytics = analytic;
            }
            //get related items
            var util = new MarketplaceUtil(_dal, _dalCloudLib, _dalAnalytics, _dalLookup);
            util.AppendSimilarItems(ref result);

            return Ok(result);
        }

        [HttpPost, Route("GetByName")]
        [ProducesResponseType(200, Type = typeof(MarketplaceItemModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByName([FromBody] IdStringWithTrackingModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"MarketplaceController|GetByName|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //name is supposed to be unique. Note name is different than display name.
            //if we get more than one match, throw exception
            var matches = _dal.Where(x => x.Name.ToLower().Equals(model.ID.ToLower()), null, null, false, true).Data;

            if (!matches.Any())
            {
                _logger.LogWarning($"MarketplaceController|GetByName|No records found matching this name: {model.ID}");
                return BadRequest($"No records found matching this name: {model.ID}");
            }
            if (matches.Count > 1)
            {
                _logger.LogWarning($"MarketplaceController|GetByName|Multiple records found matching this name: {model.ID}");
                return BadRequest($"Multiple records found matching this name: {model.ID}");
            }

            var result = matches[0];
            MarketplaceItemAnalyticsModel analytic = null;
            if (model.IsTracking)
            {
                //Increment Page Count
                //Check if MpItem is there if not add a new one then increment count and save
                analytic = _dalAnalytics.Where(x => x.MarketplaceItemId.ToString() == result.ID, null, null, false).Data.FirstOrDefault();

                if (analytic == null)
                {
                    analytic = new MarketplaceItemAnalyticsModel() { MarketplaceItemId = result.ID, PageVisitCount = 1 };
                    _dalAnalytics.Add(analytic, null);
                }
                else
                {
                    analytic.PageVisitCount += 1;
                    _dalAnalytics.Update(analytic, model.ID);
                }
                result.Analytics = analytic;
            }
            //get related items
            var util = new MarketplaceUtil(_dal, _dalCloudLib, _dalAnalytics, _dalLookup);
            util.AppendSimilarItems(ref result);

            return Ok(result);
        }


        [HttpPost, Route("GetByCategories")]
        [ProducesResponseType(200, Type = typeof(List<MarketplaceItemModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetByCategories([FromBody] List<string> model)
        {
            if (model == null)
            {
                _logger.LogWarning($"MarketplaceController|GetByCategories|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //build list of where clauses - one for each cat passed in
            var predicates = new List<Func<MarketplaceItem, bool>>();
            foreach (var cat in model)
            {
                predicates.Add(x => x.Categories.Any(c => c.ToString().Equals(cat)));
            }

            var result = _dal.Where(predicates, null, null, true, false,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.Name });

            if (result == null)
            {
                _logger.LogWarning($"MarketplaceController|GetByCategories|No records found matching these ID's: {model}");
                return BadRequest($"No records found matching this ID: {model}");
            }
            return Ok(result);
        }


        /// <summary>
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Search/Advanced")]
        [ProducesResponseType(200, Type = typeof(DALResult<MarketplaceItemModel>))]
        public async Task<IActionResult> AdvancedSearch([FromBody] MarketplaceSearchModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"MarketplaceController|AdvancedSearch|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            return await AdvancedSearch(model, true, true);
        }

        /// <summary>
        /// Admin Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// The admin difference is that it will not include CloudLib profiles in the search
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Search/Admin")]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped))]
        [ProducesResponseType(200, Type = typeof(DALResult<MarketplaceItemModel>))]
        public async Task<IActionResult> AdminSearch([FromBody] MarketplaceSearchModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"MarketplaceController|AdminSearch|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            return await AdvancedSearch(model, false, false);
        }


        /// <summary>
        /// Return a list of marketplace items and profiles to use for the admin ui. This is intended to be used for
        /// setting related items.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("admin/lookup/related")]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped))]
        [ProducesResponseType(200, Type = typeof(DALResult<MarketplaceItemModel>))]
        public async Task<IActionResult> AdminLookupProfiles([FromBody] MarketplaceSearchModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"MarketplaceController|AdminLookup|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var resultItems = _dal.GetAll()
                .Select(x => new { ID = x.ID, DisplayName = x.DisplayName, Version = x.Version, Namespace = x.Namespace });
            var resultProfiles = (await _dalCloudLib.GetAll()).Data
                .Select(x => new { ID = x.ID, DisplayName = x.DisplayName, Version = x.Version, Namespace = x.Namespace });
            return Ok(new { LookupItems = resultItems, LookupProfiles = resultProfiles });
        }


        /// <summary>
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<IActionResult> AdvancedSearch(MarketplaceSearchModel model
            , bool includeExternalSources = true, bool liveOnly = true)
        {
            var timer = Stopwatch.StartNew();
            //init and then flags set by user or system will determine which of the following get applied

            //extract selected items within a list of items
            var cats = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.FirstOrDefault(x => x.EnumValue == LookupTypeEnum.Process).Items.Where(x => x.Selected).ToList();
            var verts = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.FirstOrDefault(x => x.EnumValue == LookupTypeEnum.IndustryVertical).Items.Where(x => x.Selected).ToList();
            var pubs = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.FirstOrDefault(x => x.EnumValue == LookupTypeEnum.Publisher).Items.Where(x => x.Selected).ToList();
            var types = model.ItemTypes.Count == 0 ? new List<LookupItemFilterModel>() : model.ItemTypes.Where(x => x.Selected).ToList();
            //Special handling for types - if model.query value has specially designated terms,
            //then alter the item type filters or the model.filters for those items
            var keywordTypes = PrepareKeywordTypeSelections(model);

            //setup marketplace search task - native search of our data
            _logger.LogInformation($"MarketplaceController|AdvancedSearchExecuteTasks|Setting up tasks.");
            var mtkplCursor = PrepareSearchCursor(model, null);

            long swMarketPlaceStarted = timer.ElapsedMilliseconds;
            var searchMarketplaceTask = Task.Run(() =>
            {
                return AdvancedSearchMarketplace(model, mtkplCursor, types, keywordTypes, cats, verts, pubs, liveOnly);
            });
            long swMarketPlaceFinished = 0;
            _ = searchMarketplaceTask.ContinueWith(t => swMarketPlaceFinished = swMarketPlaceFinished == 0 ? timer.ElapsedMilliseconds : swMarketPlaceFinished);

            long swCloudLibStarted = 0;
            long swCloudLibFinished = 0;
            var listSearchExternalSources = new List<Task>();
            //add native marketplace task to list for downstream parallel execution
            listSearchExternalSources.Add(searchMarketplaceTask);
            
            //add external sources tasks (except when calling from admin ui)
            if (includeExternalSources)
            {
                var sources = _dalExternalSource.Where(x => x.Enabled && x.IsActive, null, null, false, false).Data;

                foreach (var src in sources)
                {
                    if (!src.Enabled) continue;

                    //see if there are selections for this external source that warrant a search
                    // User driven flag to select only a certain type. Determine if none are selected or if item type of sm service is selected.
                    if (!IsTypeIncluded(src.ItemType.Code, types, keywordTypes)) continue;

                    //cursor for this specific source - look at cached cursors and return the best cursor 
                    //option or new cursor for new searches
                    var nextCursor = PrepareSearchCursor(model, src.ID);

                    //TBD - temp logic - if the external source is CloudLib, then we still call a somewhat customized 
                    //flow. When time allows, come back and refine so we don't need the custom aspect of the flow. 
                    if (src.Name.ToLower().Equals("cloudlib"))
                    {
                        swCloudLibStarted = timer.ElapsedMilliseconds;
                        var searchCloudLibTask = AdvancedSearchCloudLib(model, nextCursor, keywordTypes, cats, verts, pubs);
                        _ = searchCloudLibTask.ContinueWith(t => swCloudLibFinished = swCloudLibFinished == 0 ? timer.ElapsedMilliseconds : swCloudLibFinished);
                        listSearchExternalSources.Add(searchCloudLibTask);
                    }
                    else
                    {
                        //now perform the search(es)
                        var dalSource = await _sourceFactory.InitializeSource(src);
                        listSearchExternalSources.Add(dalSource.Where(model.Query, nextCursor, null,
                            processes: cats.Count == 0 ? null : cats.Select(x => x.Name.ToLower()).ToList(),
                            verticals: verts.Count == 0 ? null : verts.Select(x => x.Name.ToLower()).ToList(),
                            null));
                    }
                }
            }

            //run query calls in parallel
            long swWaitStarted = timer.ElapsedMilliseconds;
            var allTasks = Task.WhenAll(listSearchExternalSources);
            //wrap exception handling around the tasks execution so no task exception gets lost
            try
            {
                _logger.LogInformation($"MarketplaceController|AdvancedSearch|Await outcome of .whenAll");
                await allTasks;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"MarketplaceController|AdvancedSearch|All Tasks Exception|{ex.Message}.");
                throw allTasks.Exception;
            }

            long swWaitFinished = timer.ElapsedMilliseconds;
            swMarketPlaceFinished = swMarketPlaceFinished == 0 ? timer.ElapsedMilliseconds : swMarketPlaceFinished;
            swCloudLibFinished = swCloudLibFinished == 0 ? timer.ElapsedMilliseconds : swCloudLibFinished;

            //get the tasks results into format we can use
            _logger.LogInformation($"MarketplaceController|AdvancedSearch|Executing tasks using await...");

            var resultSearches = new List<DALResultWithSource<MarketplaceItemModel>>();
            foreach (Task<DALResultWithSource<MarketplaceItemModel>> t in listSearchExternalSources)
            {
                var r = await t;
                resultSearches.Add(r);
            }

            long mergeStarted = timer.ElapsedMilliseconds;
            _logger.LogInformation($"MarketplaceController|AdvancedSearch|Unifying results...");

            //unify the results, sort, handle paging
            var result = MergeSortPageSearchedItems(model, resultSearches);
            long mergeFinished = timer.ElapsedMilliseconds;
            _logger.LogWarning($"MarketplaceController|AdvancedSearch|Duration: {timer.ElapsedMilliseconds}ms. (Marketplace: {swMarketPlaceFinished - swMarketPlaceStarted} ms. CloudLib {swCloudLibFinished - swCloudLibStarted}. MPS: {swMarketPlaceStarted}. ClStart: {swCloudLibStarted}). WaitS/F: {swWaitStarted}/{swWaitFinished}. Merge S/F: {mergeStarted}/{mergeFinished}");

            if (result == null)
            {
                _logger.LogWarning($"MarketplaceController|AdvancedSearch|No records found matching the search criteria.");
                return BadRequest($"No records found matching the search criteria.");
            }
            return Ok(result);

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
        private static SearchCursor PrepareSearchCursor(MarketplaceSearchModel model, string sourceId)
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
            if (result != null) {
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


        /// <summary>
        /// Check if we should filter by type
        /// Check if the type is selected - inspect the types list
        /// check if the user entered a value that is a reserved keyword that makes the type selected. 
        /// </summary>
        /// <param name="types"></param>
        /// <param name="keywordTypes"></param>
        /// <returns></returns>
        private bool IsTypeIncluded(string typeCode, List<LookupItemFilterModel> types, List<LookupItemFilterModel> keywordTypes)
        {
            //nothing is selected meaning everything is selected
            if (!types.Any(x => x.Selected) && !keywordTypes.Any(x => x.Selected)) return true;
            //of the selected types, the typeId passed in is a selected type
            if (types.Any(x => x.Selected && x.Code.ToLower().Equals(typeCode.ToLower()))) return true;
            //of the selected keywords, the typeId passed in is a selected type
            if (keywordTypes.Any(x => x.Selected && x.Code.ToLower().Equals(typeCode.ToLower()))) return true;
            //if we get here, false
            return false;
        }

        /// <summary>
        /// if user enters a reserved word in the search box, we translate that into a type selection.
        /// </summary>
        /// <remarks>model.ItemTypes selected items could be altered in this method</remarks>
        /// <param name="model"></param>
        /// <returns>True if a reserved word altered the type selection. This will be used downstream in the search predicate.</returns>
        private List<LookupItemFilterModel> PrepareKeywordTypeSelections(MarketplaceSearchModel model)
        {
            if (string.IsNullOrEmpty(model.Query)) return new List<LookupItemFilterModel>();

            var terms = _dalSearchKeyword
                .Where(x => x.Term.ToLower().Equals(model.Query.ToLower()), null, null, false, false).Data.ToList();

            //if there are matching reserved terms and that term has an item type in the collection.
            var result = !terms.Any() ? new List<LookupItemFilterModel>() :
                model.ItemTypes.Where(x => terms.Any(y => y.Code.ToLower().Equals(x.Code.ToLower())))
                    .Select(x => new LookupItemFilterModel()
                    {
                        Code = x.Code,
                        ID = x.ID,
                        Name = x.Name,
                        LookupType = x.LookupType,
                        IsActive = x.IsActive,
                        Selected = true,
                    })
                    .ToList();
            return result;
        }

        /// <summary>
        /// If user enters a word in the search box whose value is contained in any of the following:
        /// (a) An Industry Vertical, 
        /// (b) Processes, or
        /// (c) The name of a publisher.
        /// We return the marketplace items as if the user had clicked on the associated
        /// item in the three groups of selection filters in the Marketplace library.
        /// </summary>
        /// <remarks>model.Filters selected items could be altered in this method</remarks>
        /// <param name="model"></param>
/*
        [Obsolete()]
        private List<MarketplaceItemModel> PrepareAdvancedSearchFiltersSelections(
            MarketplaceSearchModel model, LookupTypeEnum enumVal, Func<MarketplaceItem, bool> predLiveOnly)
        {
            // We are looking for something typed into the search box. If empty, we return an empty list.
            if (string.IsNullOrEmpty(model.Query)) return new List<MarketplaceItemModel>();

            // Only include active items (table field "IsActive" = true).
            var matches = _dalLookup.Where(x => x.LookupType.EnumValue == enumVal
                                && x.IsActive
                                && x.Name.ToLower().Equals(model.Query.ToLower())
                                , null, null, false, false).Data.ToList();

            // Add items when all or part of an industry vertical (academia, aerospace, agriculture, etc) is typed into the search box.
            var predicates = new List<Func<MarketplaceItem, bool>>
            {
                x => x.IsActive
            };
            if (predLiveOnly != null)
            {
                predicates.Add(predLiveOnly);
            }

            if (enumVal == LookupTypeEnum.IndustryVertical)
            {
                predicates.Add(x => matches.Any(y => x.IndustryVerticals.Contains(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y.ID)))));
            }
            // Add items where all or part of a defined processes (air compressing, analytics, blowing, chilling, etc) is typed into the search box.
            else if (enumVal == LookupTypeEnum.Process)
            {
                predicates.Add(x => matches.Any(y => x.Categories.Contains(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y.ID)))));
            }
            // Add items where all or part of a publisher name is typed into the search box.
            else if (enumVal == LookupTypeEnum.Publisher)
            {
                predicates.Add(x => matches.Any(y => x.PublisherId.Equals(new MongoDB.Bson.ObjectId(y.ID))));
            }
            else
            {
                return new List<MarketplaceItemModel>();
            }
            return _dal.Where(predicates, null, null, false, false).Data.ToList();
        }
*/

        /// <summary>
        /// If user enters a word in the search box whose value is contained in any of the following:
        /// (a) An Industry Vertical, 
        /// (b) Processes, or
        /// (c) The name of a publisher.
        /// We return the associated category that contains that word
        /// </summary>
        /// <remarks>model.Filters selected items could be altered in this method</remarks>
        /// <param name="model"></param>
        /// <returns>list of ids of lookup items matching the word</returns>
        private List<string> PrepareSearchFiltersSelections(string query, LookupTypeEnum enumVal)
        {
            // We are looking for something typed into the search box. If empty, we return an empty list.
            if (string.IsNullOrEmpty(query)) return new List<string>();

            query = query.ToLower();

            if (enumVal == LookupTypeEnum.Publisher)
            {
                return _dalPublisher.Where(x =>
                                x.IsActive
                                && (x.Name.ToLower().Contains(query)
                                || x.DisplayName.ToLower().Contains(query))
                                , null, null, false, false).Data
                                .ToList()
                                .Select(x => x.ID).ToList();
            }
            else
            {
                // Only include active items (table field "IsActive" = true).
                return _dalLookup.Where(x => x.LookupType.EnumValue == enumVal
                                    && x.IsActive
                                    && x.Name.ToLower().Contains(query.ToLower())
                                    , null, null, false, false).Data
                                    .ToList()
                                    .Select(x => x.ID).ToList();
            }
        }

        /// <summary>
        /// If user enters a word in the search box whose value is contained in any of the following:
        /// (a) An Industry Vertical, 
        /// (b) Processes, or
        /// (c) The name of a publisher.
        /// We return the associated category that contains that word
        /// </summary>
        /// <remarks>model.Filters selected items could be altered in this method</remarks>
        /// <param name="model"></param>
        /// <returns>list of ids of lookup items matching the word</returns>
        private Func<MarketplaceItem, bool> BuildQueryPredicate(string query, List<LookupItemFilterModel> keywordTypes, bool keywordTypeIsSelected)
        {
            // We are looking for something typed into the search box. If empty, we return an empty list.
            if (string.IsNullOrEmpty(query)) return null;

            //get list of cats, verts, pubs containing the query term
            var cats = PrepareSearchFiltersSelections(query, LookupTypeEnum.Process);
            var verts = PrepareSearchFiltersSelections(query, LookupTypeEnum.IndustryVertical);
            var pubs = PrepareSearchFiltersSelections(query, LookupTypeEnum.Publisher);

            //add where clause for the search terms - check against more fields
            query = query.ToLower();
            Func<MarketplaceItem, bool> result = x =>
                x.Name.ToLower().Contains(query)
                //or search on additional fields
                || x.DisplayName.ToLower().Contains(query)
                || x.Description.ToLower().Contains(query)
                || x.Abstract.ToLower().Contains(query)
                || (x.MetaTags != null && x.MetaTags.Contains(query))
                || (keywordTypeIsSelected && x.ItemTypeId != null && keywordTypes.Any(y => y.ID.Equals(x.ItemTypeId.ToString())))
                || cats.Any(y => x.Categories.Contains(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y))))
                || verts.Any(y => x.IndustryVerticals.Contains(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y))))
                || pubs.Any(y => x.PublisherId.Equals(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y))))
            ;
            return result;
        }

        /// <summary>
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<DALResultWithSource<MarketplaceItemModel>> AdvancedSearchMarketplace(
            MarketplaceSearchModel model
            , SearchCursor cursor
            , List<LookupItemFilterModel> types
            , List<LookupItemFilterModel> keywordTypes
            , List<LookupItemFilterModel> cats
            , List<LookupItemFilterModel> verts
            , List<LookupItemFilterModel> pubs
            , bool liveOnly = true)
        {
            _logger.LogInformation($"MarketplaceController|AdvancedSearchMarketplace|Starting...");
            var timer = Stopwatch.StartNew();
            var util = new MarketplaceUtil(_dal, _dalCloudLib, _dalAnalytics, _dalLookup);

            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            //union passed in list w/ lookup list.
            var combinedCats = cats?.Select(x => x.ID).ToArray();
            var combinedVerts = verts?.Select(x => x.ID).ToArray();
            var combinedPubs = pubs?.Select(x => x.ID).ToArray();

            //build list of where clauses all combined into one predicate, 
            //using expression extension to allow for .Or or .And expression
            var predicates = new List<Func<MarketplaceItem, bool>>
            {
                //limit to isActive
                x => x.IsActive
            };

            //limit to publish status of live
            if (liveOnly)
            {
                var statuses = new List<string>() { "live" };
                //if user is an admin, let them also see items listed as 'admin only' status
                if (User.IsInRole("cesmii.marketplace.marketplaceadmin"))
                {
                    statuses.Add("admin-only");
                }
                predicates.Add(util.BuildStatusFilterPredicate(statuses));
            }

            //build list of where clauses - one for each cat passed in
            Func<MarketplaceItem, bool> predicateCat = null;
            if (combinedCats != null && combinedCats.Length > 0)
            {
                foreach (var cat in combinedCats)// model.Categories)
                {
                    if (predicateCat == null) predicateCat = x => x.Categories.Any(c => c.ToString().Equals(cat));
                    else predicateCat = predicateCat.Or(x => x.Categories.Any(c => c.ToString().Equals(cat)));
                }
                predicates.Add(predicateCat);
            }
            //build list of where clauses - one for each industry vertical passed in
            Func<MarketplaceItem, bool> predicateVert = null;
            if (combinedVerts != null && combinedVerts.Length > 0)
            {
                foreach (var vert in combinedVerts)
                {
                    if (predicateVert == null) predicateVert = x => x.IndustryVerticals.Any(c => c.ToString().Equals(vert));
                    else predicateVert = predicateVert.Or(x => x.IndustryVerticals.Any(c => c.ToString().Equals(vert)));
                }
                predicates.Add(predicateVert);
            }

            //build where clause - any publisher id that is in my list of pub ids
            if (combinedPubs != null && combinedPubs.Length > 0)
            {
                predicates.Add(x => combinedPubs.Any(p => p == x.PublisherId.ToString()));
            }

            //scenarios
            //if types are selected, add them to the predicate.
            //any model.query value added would then use an AND with this selected type
            if (types != null && types.Any())
            {
                predicates.Add(x => x.ItemTypeId != null && types.Any(y => y.ID.Equals(x.ItemTypeId.ToString())));
            }
            
            //no types selected (sm-app, sm-hardware, sm-profile) - clicking the type filter button
            //but keyword type is selected via a query value mapped to a type selection (ie profiles maps to sm-profile type)
            //impact of this scenario - find anything with that type OR anything with that term
            bool keywordTypeSelected = ((types == null || !types.Any()) && keywordTypes != null && keywordTypes.Any());

            //add where clause for the search terms - check against more fields
            if (!string.IsNullOrEmpty(model.Query))
            {
                Func<MarketplaceItem, bool> predicateQuery = BuildQueryPredicate(model.Query, keywordTypes, keywordTypeSelected);
                if (predicateQuery != null) predicates.Add(predicateQuery);

                predicates.Add(predicateQuery);
            }

            //now execute the search - if we don't have predicates, then we just get all paged. 
            //each individual predicate (predicateCat, predicateVert, predicateQuery) acts as an OR within. Collectively,
            //the 3 predicates operate like an AND operator
            //(ie (if cat == 'foo' || cat == 'bar') AND (if vert == 'a' || vert == 'b') AND (name.contains('blah') || description.contains('blah'))
            ///--------------------------------------------------------------------------------------------------------
            /// NOTE:
            /// Because we are unifying two sources of information from separate sources (cloud lib, marketplace db), we need to wait on paging, sorting 
            /// and do not do this at the DB level. We have to get the filtered set of info and then apply a sort 
            /// and then the page. 
            ///--------------------------------------------------------------------------------------------------------
            //var result = predicates.Count == 0 ? _dal.GetAllPaged(model.Skip, model.Take, true, false) :
            //    _dal.Where(predicates, model.Skip, model.Take, true, false,
            //        new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
            //        new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName });
            //new - no paging,sorting
            _logger.LogTrace($"MarketplaceController|AdvancedSearchMarketplace|calling DAL...");
            var result = await Task.Run(() =>
            {
                //retrun from task.run
                return predicates.Count == 0 && cursor.Skip == 0
                    ? _dal.GetAllPaged(null, null, !cursor.HasTotalCount, false)
                    : _dal.Where(predicates, cursor.Skip, cursor.Take, !cursor.HasTotalCount, false,
                            new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
                            new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName });
            });

            _logger.LogWarning($"MarketplaceController|AdvancedSearchMarketplace|Duration: { timer.ElapsedMilliseconds}ms.");
            cursor.TotalCount = (int)result.Count;
            return new DALResultWithSource<MarketplaceItemModel>()
            { 
                Data = result.Data,
                Count = result.Count,
                SummaryData = result.SummaryData,
                SourceId = null,
                Cursor = cursor
            };
        }

        /// <summary>
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<DALResultWithSource<MarketplaceItemModel>> AdvancedSearchCloudLib(
            MarketplaceSearchModel model
            , SearchCursor cursor
            , List<LookupItemFilterModel> keywordTypes
            , List<LookupItemFilterModel> cats
            , List<LookupItemFilterModel> verts
            , List<LookupItemFilterModel> pubs
            )
        {
            _logger.LogInformation($"MarketplaceController|AdvancedSearchCloudLib|Starting...");
            var timer = Stopwatch.StartNew();

            //lowercase model.query - preserve original value in model.Query for use elsewere
            var query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();
            //if keywordTypes has any selected, and we get to this function, then we deduce that the special type was
            //true because the user entered a special term related specifically to profile. So, we need to replace the
            //model.Query value with "" so that we don't filter out profiles by the term profile and yield no/very few matches. 
            if (keywordTypes != null &&
                keywordTypes.Any(x => x.Selected && x.Code.ToLower().Equals(_configUtil.MarketplaceSettings.SmProfile.Code.ToLower())))
            {
                query = "";
            }


            var result = new DALResultWithSource<MarketplaceItemModel>();
            //if publishers is a filter, then we skip CloudLib for now because search is trying to only show 
            //items for that publisher. In future, remove this once we store our own metadata. 
            //only search CloudLib if no publisher filter
            if (pubs.Count == 0)
            {
                //NEW: now search CloudLib.
                _logger.LogTrace($"MarketplaceController|AdvancedSearchCloudLib|calling DAL...");
                result = await _dalCloudLib.Where(query: query,
                    cursor,
                    ids: null,
                    processes: cats.Count == 0 ? null : cats.Select(x => x.Name.ToLower()).ToList(),
                    verticals: verts.Count == 0 ? null : verts.Select(x => x.Name.ToLower()).ToList(),
                    exclude: _configUtil.CloudLibSettings?.ExcludedNodeSets);

                //check to see if the CloudLib returned data. 
                if (cats.Count == 0 && verts.Count == 0 && string.IsNullOrEmpty(query)
                    && result.Data.Count == 0)
                {
                    _logger.LogWarning($"MarketplaceController|AdvancedSearchCloudLib|No CloudLib records found yet search criteria was wildcard.");
                }
            }

            _logger.LogInformation($"MarketplaceController|AdvancedSearchCloudLib|Duration: {timer.ElapsedMilliseconds}ms.");
            return result;
        }


        /// <summary>
        /// Because we are unifying multiple sources of information from separate sources, we need to wait on paging 
        /// and do not do this at the DB level. We have to get the filtered set of info and then apply a sort 
        /// and then the page. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static DALResultWithCursors<MarketplaceItemModel> MergeSortPageSearchedItems(
            MarketplaceSearchModel model, 
            List<DALResultWithSource<MarketplaceItemModel>> sets)
        {
            var pageIndex = model.Skip / model.Take;

            //if current cursors is null or skip == 0, start fresh
            //if current cursors not null and skip != null, then we assume we are paging and we need previous cursor
            //  to inform next cursor

            //union results into one set, order set and then find min/max of each source type.
            List<MarketplaceItemModel> resultData = new List<MarketplaceItemModel>();
            foreach (var item in sets)
            {
                if (item.Data != null) resultData = resultData.Union(item.Data).ToList();
            }
            //now order, then filter
            //new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
            //new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName });
            var result = new DALResultWithCursors<MarketplaceItemModel>() { 
                Count = sets.Sum(x => x.Count),
                //for now, order by display name rather than featured/display name
                Data = resultData
                    //.OrderByDescending(x => x.IsFeatured)
                    //.ThenBy(x => x.DisplayName)
                    .OrderBy(x => x.DisplayName)
                    .Skip(model.Skip)
                    .Take(model.Take).ToList(),
                CachedCursors = model.CachedCursors == null ? new List<SourceSearchCursor>() : model.CachedCursors
            };

            //now revise cursors for each source based on this filtering
            foreach (var item in sets)
            {
                //some external sources use cursor, others will use skip/take approach
                //find first and last item for each set and record the index or cursor
                var first = string.IsNullOrEmpty(item.SourceId) ?
                    result.Data.Find(x => string.IsNullOrEmpty(x.ExternalSourceId)) :
                    result.Data.Find(x => !string.IsNullOrEmpty(x.ExternalSourceId) && x.ExternalSourceId.Equals(item.SourceId));
                var last = string.IsNullOrEmpty(item.SourceId) ?
                    result.Data.FindLast(x => string.IsNullOrEmpty(x.ExternalSourceId)) :
                    result.Data.FindLast(x => !string.IsNullOrEmpty(x.ExternalSourceId) && x.ExternalSourceId.Equals(item.SourceId));

                //update the cursor boundaries to reflect the cursor post-merge
                item.Cursor.StartCursor = first == null ? null : first.Cursor;
                item.Cursor.Skip = first == null ? 0 : resultData.IndexOf(first);
                item.Cursor.EndCursor = last == null ? null : last.Cursor;
                item.Cursor.Take = last == null ? 0 :
                    resultData.IndexOf(last) - item.Cursor.Skip;
                item.Cursor.PageIndex = pageIndex;

                //either update an existing cursor or add a new one
                var cursorMatch = result.CachedCursors.Find(x => x.SourceId == item.SourceId); //source id can be null
                if (cursorMatch == null)
                {
                    cursorMatch = new SourceSearchCursor()
                    {
                        SourceId = item.SourceId,
                        Cursors = new List<SearchCursor>() { item.Cursor },
                    };
                    result.CachedCursors.Add(cursorMatch);
                }
                else
                {
                    //now find the cursor in the list of the cursors for this source
                    // and append/update this cursor
                    var cachedMatch = cursorMatch.Cursors.Find(x => x.PageIndex == pageIndex);
                    if (cachedMatch != null)
                    {
                        cachedMatch.StartCursor = item.Cursor.StartCursor;
                        cachedMatch.Skip = item.Cursor.Skip;
                        cachedMatch.EndCursor = item.Cursor.EndCursor;
                        cachedMatch.Take = item.Cursor.Take;
                    }
                    else
                    {
                        cursorMatch.Cursors.Add(item.Cursor);
                    }
                }
            }
            return result;
        }

        private static int Compare(MarketplaceItemModel m1, MarketplaceItemModel m2)
        {
            if (m1.IsFeatured && !m2.IsFeatured)
            {
                return -1;
            }
            if (m2.IsFeatured && !m1.IsFeatured)
            {
                return 1;
            }
            return string.Compare(m1.DisplayName, m2.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        #region Analytics endpoints
        /// <summary>
        /// Increment LikeCount for MarketplaceItem that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("analytics/IncrementLikeCount")]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> IncrementLikeCount([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"MarketplaceController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }
            MarketplaceItemAnalyticsModel analytic = null;

            //Increment Page Count
            //Check if MpItem is there if not add a new one then increment count and save
            analytic = _dalAnalytics.Where(x => x.MarketplaceItemId.ToString() == model.ID, null, null, false).Data.FirstOrDefault();

            if (analytic == null)
            {
                await _dalAnalytics.Add(new MarketplaceItemAnalyticsModel() { MarketplaceItemId = model.ID, LikeCount = 1 }, null);
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = 1
                });
            }
            else
            {
                analytic.LikeCount += 1;
                await _dalAnalytics.Update(analytic, null);
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was updated.",
                    Data = analytic.LikeCount // return analytic.likecount
                });
            }
        }

        /// <summary>
        /// Increment DislikeCount for MarketplaceItem that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("analytics/IncrementDislikeCount")]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> IncrementDislikeCount([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"MarketplaceController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            MarketplaceItemAnalyticsModel analytic = null;
            //Increment Page Count
            //Check if MpItem is there if not add a new one then increment count and save
            analytic = _dalAnalytics.Where(x => x.MarketplaceItemId.ToString() == model.ID, null, null, false).Data.FirstOrDefault();

            if (analytic == null)
            {
                await _dalAnalytics.Add(new MarketplaceItemAnalyticsModel() { MarketplaceItemId = model.ID, DislikeCount = 1 }, null);
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = 1
                });
            }
            else
            {
                analytic.DislikeCount += 1;
                await _dalAnalytics.Update(analytic, null);
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was updated.",
                    Data = analytic.DislikeCount
                });
            }
        }
        #endregion
    }
}
