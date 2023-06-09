﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Data.Extensions;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.Api.Shared.Utils;
using System.Text;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class MarketplaceController : BaseController<MarketplaceController>
    {
        private readonly IDal<MarketplaceItem, MarketplaceItemModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly IDal<Publisher, PublisherModel> _dalPublisher;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;
        private readonly ICloudLibDAL<MarketplaceItemModel> _dalCloudLib;
        private readonly IDal<SearchKeyword, SearchKeywordModel> _dalSearchKeyword;
        public MarketplaceController(IDal<MarketplaceItem, MarketplaceItemModel> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<Publisher, PublisherModel> dalPublisher,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            ICloudLibDAL<MarketplaceItemModel> dalCloudLib,
            IDal<SearchKeyword, SearchKeywordModel> dalSearchKeyword,
            UserDAL dalUser,
            ConfigUtil config, ILogger<MarketplaceController> logger)
            : base(config, logger, dalUser)
        {
            _dal = dal;
            _dalPublisher = dalPublisher;
            _dalLookup = dalLookup;
            _dalAnalytics = dalAnalytics;
            _dalCloudLib = dalCloudLib;
            _dalSearchKeyword = dalSearchKeyword;
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
                NewItems = _dal.Where(x => x.IsActive, null, 3, false, false,
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

            //trim down to 3 most recent 
            var result = _dal.Where(predicates, null, 3, false, false,
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
            var resultProfiles = (await _dalCloudLib.GetAll())
                .Select(x => new { ID = x.ID, DisplayName = x.DisplayName, Version = x.Version, Namespace = x.Namespace });
            return Ok(new { LookupItems = resultItems, LookupProfiles = resultProfiles });
        }


        /// <summary>
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<IActionResult> AdvancedSearch([FromBody] MarketplaceSearchModel model
            , bool includeCloudLib = true, bool liveOnly = true)
        {
            var timer = Stopwatch.StartNew();
            //init and then flags set by user or system will determine which of the following get applied

            //Special handling for types
            //if model.query value has specially designated terms, then alter the item type filters or the model.filters for those items
            var useSpecialTypeSelection = PrepareAdvancedSearchTypeSelections(model);

            //extract selected items within a list of items
            var cats = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.FirstOrDefault(x => x.EnumValue == LookupTypeEnum.Process).Items.Where(x => x.Selected).ToList();
            var verts = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.FirstOrDefault(x => x.EnumValue == LookupTypeEnum.IndustryVertical).Items.Where(x => x.Selected).ToList();
            var pubs = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.FirstOrDefault(x => x.EnumValue == LookupTypeEnum.Publisher).Items.Where(x => x.Selected).ToList();
            var types = model.ItemTypes.Count == 0 ? new List<LookupItemFilterModel>() : model.ItemTypes.Where(x => x.Selected).ToList();

            // Check for publishers in the query string
            if (!string.IsNullOrEmpty(model.Query))
            {
                bool bAnyAdded = false;
                StringBuilder sbNewQuery = new StringBuilder();
                List<PublisherModel> pubAll = _dalPublisher.GetAll(false);
                var astrWords = model.Query.Split(new char[] { ' ',',','.','\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (astrWords.Length > 0)
                {
                    foreach (string strItem in astrWords)
                    {
                        bool bItemFound = false;
                        foreach (PublisherModel pm in pubAll)
                        {
                            if (pm.DisplayName.Contains(strItem))
                            {
                                pubs.Add(new LookupItemFilterModel() { Code = null, DisplayOrder = 999, ID = pm.ID.ToString(), IsActive = pm.IsActive, Name = pm.DisplayName, Selected = true });
                                bAnyAdded = true;
                                bItemFound = true;
                            }
                        }

                        if (!bItemFound)
                            sbNewQuery.Append($"{strItem} ");
                    }

                    // If we found any publishers, update the query minus the publisher name.
                    if (bAnyAdded)
                        model.Query = sbNewQuery.ToString().Trim();
                }
            }

            // Any type (Apps or Hardware) but not Profiles
            // User driven flag to select only a certain type. Determine if none are selected or if item type of sm app is selected.
            DALResult<MarketplaceItemModel> result;

            // SM Profiles
            // User driven flag to select only a certain type. Determine if none are selected or if item type of sm profile is selected.
            var includeSmProfileTypes = !types.Any(x => x.Selected) ||
                types.Any(x => x.Selected && x.Code.ToLower().Equals(_configUtil.MarketplaceSettings.SmProfile.Code.ToLower()));
            //Skip over this in certain scenarios. ie. admin section
            if (_configUtil.MarketplaceSettings.EnableCloudLibSearch && includeCloudLib && includeSmProfileTypes)
            {
                _logger.LogInformation($"MarketplaceController|AdvancedSearch|Setting up tasks.");
                long swMarketPlaceStarted = timer.ElapsedMilliseconds;
                var searchMarketplaceTask = Task.Run(() =>
                {
                    return AdvancedSearchMarketplace(model, types, cats, verts, pubs, useSpecialTypeSelection, liveOnly);
                });
                long swMarketPlaceFinished = 0;
                _ = searchMarketplaceTask.ContinueWith(t => swMarketPlaceFinished = timer.ElapsedMilliseconds);

                long swCloudLibStarted = timer.ElapsedMilliseconds;
                var searchCloudLibTask = AdvancedSearchCloudLib(model, cats, verts, pubs, useSpecialTypeSelection);
                long swCloudLibFinished = 0;
                _ = searchCloudLibTask.ContinueWith(t => swCloudLibFinished = timer.ElapsedMilliseconds);
                //run in parallel
                long swWaitStarted = timer.ElapsedMilliseconds;
                var allTasks = Task.WhenAll(searchMarketplaceTask, searchCloudLibTask);

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

                //get the tasks results into format we can use
                _logger.LogInformation($"MarketplaceController|AdvancedSearch|Executing tasks using await...");
                var resultSearchMarketplace = await searchMarketplaceTask;
                var resultSearchCloudLib = await searchCloudLibTask;

                long mergeStarted = timer.ElapsedMilliseconds;
                _logger.LogInformation($"MarketplaceController|AdvancedSearch|Unifying results...");

                //unify the results, sort, handle paging
                result = MergeSortPageSearchedItems(resultSearchMarketplace, resultSearchCloudLib, model);
                long mergeFinished = timer.ElapsedMilliseconds;
                _logger.LogWarning($"MarketplaceController|AdvancedSearch|Duration: {timer.ElapsedMilliseconds}ms. (Marketplace: {swMarketPlaceFinished - swMarketPlaceStarted} ms. CloudLib {swCloudLibFinished - swCloudLibStarted}. MPS: {swMarketPlaceStarted}. ClStart: {swCloudLibStarted}). WaitS/F: {swWaitStarted}/{swWaitFinished}. Merge S/F: {mergeStarted}/{mergeFinished}");
            }
            else
            {
                long swMarketPlaceStarted = timer.ElapsedMilliseconds;
                result = await AdvancedSearchMarketplace(model, types, cats, verts, pubs, useSpecialTypeSelection, liveOnly);
                //because we wait to page, sort till after in the combined (Cloud and marketplace) scenario, we need to do same here. 
                //now page the data. 
                result.Data = result.Data?
                    // The sources are ordered and merged in order: no need to re-order
                    //.OrderBy(x => x.IsFeatured)
                    //.ThenBy(x => x.DisplayName)
                    .Skip(model.Skip)
                    .Take(model.Take)
                    .ToList();

                long swMarketPlaceFinished = timer.ElapsedMilliseconds;
                _logger.LogWarning($"MarketplaceController|AdvancedSearch|Duration: {timer.ElapsedMilliseconds}ms. (Marketplace: {swMarketPlaceFinished - swMarketPlaceStarted} ms.");
            }

            //_logger.LogWarning($"MarketplaceController|AdvancedSearch|Duration: { timer.ElapsedMilliseconds}ms.");

            if (result == null)
            {
                _logger.LogWarning($"MarketplaceController|AdvancedSearch|No records found matching the search criteria.");
                return BadRequest($"No records found matching the search criteria.");
            }
            return Ok(result);
        }

        /// <summary>
        /// if user enters a reserved word in the search box, we translate that into a type selection.
        /// </summary>
        /// <remarks>model.ItemTypes selected items could be altered in this method</remarks>
        /// <param name="model"></param>
        /// <returns>True if a reserved word altered the type selection. This will be used downstream in the search predicate.</returns>
        private bool PrepareAdvancedSearchTypeSelections(MarketplaceSearchModel model)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(model.Query))
            {
                var terms = _dalSearchKeyword
                    .Where(x => x.Term.ToLower().Equals(model.Query.ToLower()), null, null, false, false).Data.ToList();

                //if there are matching reserved terms and that term has an item type in the collection.
                result = terms.Any() && model.ItemTypes.Any(x => terms.Any(y => y.Code.ToLower().Equals(x.Code.ToLower())));

                foreach (var t in model.ItemTypes)
                {
                    t.Selected = t.Selected || terms.Any(y => y.Code.ToLower().Equals(t.Code.ToLower()));
                }
            }

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
        private Func<MarketplaceItem, bool> BuildQueryPredicate(string query, List<LookupItemFilterModel> types, bool useSpecialTypeSelection)
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
                || (useSpecialTypeSelection && x.ItemTypeId != null && types.Any(y => y.ID.Equals(x.ItemTypeId.ToString())))
                || cats.Any(y => x.Categories.Contains(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y))))
                || verts.Any(y => x.IndustryVerticals.Contains(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y))))
                || pubs.Any(y => x.PublisherId.Equals(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y))))
            ;
            /*
            Func<MarketplaceItem, bool> result = x => x.Name.ToLower().Contains(query);
            //or search on additional fields
            result.Or(x => x.DisplayName.ToLower().Contains(query));
            result.Or(x => x.Description.ToLower().Contains(query));
            result.Or(x => x.Abstract.ToLower().Contains(query));
            result.Or(x => x.MetaTags != null && x.MetaTags.Contains(query));

            //if we are using special type, it means user entered special word for query like "profile". In this case,
            //we want to get all types of sm-profile >>OR<< any item containing the word profile
            if (useSpecialTypeSelection)
            {
                result.Or(x => x.ItemTypeId != null && types.Any(y => y.ID.Equals(x.ItemTypeId.ToString())));
            }
            
            //if we have a query value which has a category, vert or pub match, then we don't require match
            //on one of the freeform text fields. 
            if (cats.Any())
                result.Or(x => cats.Any(y => x.Categories.Contains(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y)))));
            if (verts.Any())
                result.Or(x => verts.Any(y => x.IndustryVerticals.Contains(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y)))));
            if (pubs.Any())
                result.Or(x => pubs.Any(y => x.PublisherId.Equals(new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(y)))));
                //result.Or(x => pubs.Any(p => p == x.PublisherId.ToString()));
            */
            return result;
        }

        /// <summary>
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<DALResult<MarketplaceItemModel>> AdvancedSearchMarketplace([FromBody] MarketplaceSearchModel model
            , List<LookupItemFilterModel> types
            , List<LookupItemFilterModel> cats
            , List<LookupItemFilterModel> verts
            , List<LookupItemFilterModel> pubs
            , bool useSpecialTypeSelection
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
                predicates.Add(util.BuildStatusFilterPredicate());
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

            //build where clause - one for each type passed in
            if (types != null && types.Any())
            {
                //if we are using special type, it means user entered special word for query like "profile". In this case,
                //we want to get all types of sm-profile >>OR<< any item containing the word profile
                //so, predicateTypes applied within the model.query section next. 
                //Otherwise, we add it right away per usual
                if (!useSpecialTypeSelection)
                {
                    predicates.Add(x => x.ItemTypeId != null && types.Any(y => y.ID.Equals(x.ItemTypeId.ToString())));
                }
            }

            //add where clause for the search terms - check against more fields
            if (!string.IsNullOrEmpty(model.Query))
            {
                Func<MarketplaceItem, bool> predicateQuery = BuildQueryPredicate(model.Query, types, useSpecialTypeSelection);
                if (predicateQuery != null) predicates.Add(predicateQuery);

                /*
                //TBD - Academia - no longer returning value because not in name but is in category. Have to figure out 
                //how to weave that into the mix.
                //TBD - check that we can update appsettings values from Azure in Configuraiton area.

                //add series of where conditions
                if (useSpecialTypeSelection)
                {
                    predicateQuery = x => x.Name.ToLower().Contains(model.Query)
                        //or search on additional fields
                        || x.DisplayName.ToLower().Contains(model.Query)
                        || x.Description.ToLower().Contains(model.Query)
                        || x.Abstract.ToLower().Contains(model.Query)
                        || (x.MetaTags != null && x.MetaTags.Contains(model.Query))
                        //if we are using special type, it means user entered special word for query like "profile". In this case,
                        //we want to get all types of sm-profile >>OR<< any item containing the word profile
                        || (x.ItemTypeId != null && types.Any(y => y.ID.Equals(x.ItemTypeId.ToString())))
                        ;
                }
                else
                {
                    predicateQuery = x => x.Name.ToLower().Contains(model.Query)
                        //or search on additional fields
                        || x.DisplayName.ToLower().Contains(model.Query)
                        || x.Description.ToLower().Contains(model.Query)
                        || x.Abstract.ToLower().Contains(model.Query)
                        || (x.MetaTags != null && x.MetaTags.Contains(model.Query))
                        ;
                }
                */
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
                return predicates.Count == 0 ? _dal.GetAllPaged(null, null, true, false) :
                _dal.Where(predicates, null, model.Skip + model.Take, true, false,
                    new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
                    new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName });
            });

            // Special handling for processes, industry verticals, and publishers. If the search box contains a
            // term in one of these three categories, the corresponding market items are added to the list of
            // items to include. 

            // We do it as a separate query because the initial marketplace query already has an abundance of logic and 
            // and complexity and it was not working to add in additional logic.

            // First -- run the searches.
            /*
            var matchesProcesses = Task.Run(() =>
            {
                return PrepareAdvancedSearchFiltersSelections(model, LookupTypeEnum.Process, liveOnly ? util.BuildStatusFilterPredicate() : null);
            });
            var matchesVerts = Task.Run(() =>
            {
                return PrepareAdvancedSearchFiltersSelections(model, LookupTypeEnum.IndustryVertical, liveOnly ? util.BuildStatusFilterPredicate() : null);
            });

            var matchesPublishers = Task.Run(() =>
            {
                return PrepareAdvancedSearchFiltersSelections(model, LookupTypeEnum.Publisher, liveOnly ? util.BuildStatusFilterPredicate() : null);
            });


            // Second - grab the results.
            var itemsProcesses = await matchesProcesses;
            var itemsVerts = await matchesVerts; 
            var itemsPublishers = await matchesPublishers;

            // Third - Join the results together.
            //if (itemsProcesses.Any()) result.Data = result.Data.UnionBy(itemsProcesses, x=> x.ID).ToList();
            //if (itemsVerts.Any()) result.Data = result.Data.UnionBy(itemsVerts, x => x.ID).ToList();
            //if (itemsPublishers.Any()) result.Data = result.Data.UnionBy(itemsPublishers, x => x.ID).ToList();
            */
            //result.Count = result.Data.Count;

            _logger.LogWarning($"MarketplaceController|AdvancedSearchMarketplace|Duration: { timer.ElapsedMilliseconds}ms.");
            return result;
        }

        /// <summary>
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<DALResult<MarketplaceItemModel>> AdvancedSearchCloudLib([FromBody] MarketplaceSearchModel model
            , List<LookupItemFilterModel> cats
            , List<LookupItemFilterModel> verts
            , List<LookupItemFilterModel> pubs
            , bool useSpecialTypeSelection
            )
        {
            _logger.LogInformation($"MarketplaceController|AdvancedSearchCloudLib|Starting...");
            var timer = Stopwatch.StartNew();

            //lowercase model.query - preserve original value in model.Query for use elsewere
            var query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();
            //if useSpecialTypeSelection == true and we get to this function, then we deduce that the special type was
            //true because the user entered a special term related specifically to profile. So, we need to replace the
            //model.Query value with "" so that we don't filter out profiles by the term profile and yield no/very few matches. 
            if (useSpecialTypeSelection) query = "";

            var result = new DALResult<MarketplaceItemModel>();
            //if publishers is a filter, then we skip CloudLib for now because search is trying to only show 
            //items for that publisher. In future, remove this once we store our own metadata. 
            //only search CloudLib if no publisher filter
            if (pubs.Count == 0)
            {
                //NEW: now search CloudLib.
                _logger.LogTrace($"MarketplaceController|AdvancedSearchCloudLib|calling DAL...");
                result = await _dalCloudLib.Where(query,
                    0, // Because we are merging multiple sources and don't preserve individual cursors for each source, we have to always start from the beginning
                    model.Skip + model.Take,
                    null,
                    cats.Count == 0 ? null : cats.Select(x => x.Name.ToLower()).ToList(),
                    verts.Count == 0 ? null : verts.Select(x => x.Name.ToLower()).ToList(),
                    _configUtil.CloudLibSettings?.ExcludedNodeSets);

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
        /// Because we are unifying two sources of information from separate sources, we need to wait on paging 
        /// and do not do this at the DB level. We have to get the filtered set of info and then apply a sort 
        /// and then the page. 
        /// </summary>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private static DALResult<MarketplaceItemModel> MergeSortPageSearchedItems(DALResult<MarketplaceItemModel> set1, DALResult<MarketplaceItemModel> set2,
            MarketplaceSearchModel model)
        {
            //get count before paging
            var count = set1.Count + set2.Count;
            //sort 2nd set but always put it after the regular marketplace items.  
            //set2 = set2.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.DisplayName).ToList();
            //combine the data, get the total count

            // Assume both inputs are ordered: merge while preserving order

            int i1 = 0, i2 = 0;
            List<MarketplaceItemModel> combined = new List<MarketplaceItemModel>();
            do
            {
                if (i2 >= set2.Data.Count || (i1 < set1.Data.Count && Compare(set1.Data[i1], set2.Data[i2]) <= 0))
                {
                    combined.Add(set1.Data[i1]);
                    i1++;
                }
                else
                {
                    combined.Add(set2.Data[i2]);
                    i2++;
                }
            } while (i1 < set1.Data.Count || i2 < set2.Data.Count);

            //var combined = set1.Data.Union(set2.Data);
            //TBD - order by the unified result - order by type, then by featured then by name
            //combined = combined
            //    .OrderBy(x => x.Type?.DisplayOrder)
            //    .ThenBy(x => x.Type?.Name)
            //    .ThenByDescending(x => x.IsFeatured)
            //    .ThenBy(x => x.DisplayName); //.ToList();
            //combined = combined
            //    .OrderByDescending(x => x.IsFeatured)
            //    .ThenBy(x => x.DisplayName);

            //now page the data. 
            combined = combined.Skip(model.Skip).Take(model.Take).ToList();
            return new DALResult<MarketplaceItemModel>() {
                Count = count,
                Data = combined
            };
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
