using System;
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

namespace CESMII.Marketplace.Api.Controllers
{
    [AllowAnonymous, Route("api/[controller]")]
    public class MarketplaceController : BaseController<MarketplaceController>
    {
        private readonly IDal<MarketplaceItem, MarketplaceItemModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;
        private readonly ICloudLibDAL<MarketplaceItemModel> _dalCloudLib;

        public MarketplaceController(IDal<MarketplaceItem, MarketplaceItemModel> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            ICloudLibDAL<MarketplaceItemModel> dalCloudLib,
            ConfigUtil config, ILogger<MarketplaceController> logger)
            : base(config, logger)
        {
            _dal = dal;
            _dalLookup = dalLookup;
            _dalAnalytics = dalAnalytics;
            _dalCloudLib = dalCloudLib;
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
        public IActionResult Home()
        {
            MarketplaceHomeModel result = new MarketplaceHomeModel();
            result.FeaturedItems = _dal.Where(x => x.IsFeatured && x.IsActive, null, null, false, false).Data;
            //trim down to 4 most recent 
            result.NewItems = _dal.Where(x => x.IsActive , null, 3, false, false,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.PublishDate, IsDescending = true }).Data;
            //calculate most popular based on analytics counts
            var util = new MarketplaceUtil(_dal, _dalAnalytics, _dalLookup);
            result.PopularItems = util.PopularItems();

            return Ok(result);
        }

        [HttpPost, Route("featured")]
        [ProducesResponseType(200, Type = typeof(List<MarketplaceItemModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetFeatured()
        {
            List<Func<MarketplaceItem, bool>> predicates = new List<Func<MarketplaceItem, bool>>();
            //limit to featured and isActive
            predicates.Add(x => x.IsFeatured && x.IsActive);
            //limit to publish status of live
            var util = new MarketplaceUtil(_dal, _dalAnalytics, _dalLookup);
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
            List<Func<MarketplaceItem, bool>> predicates = new List<Func<MarketplaceItem, bool>>();
            //limit to isActive
            predicates.Add(x => x.IsActive);
            //limit to publish status of live
            var util = new MarketplaceUtil(_dal, _dalAnalytics, _dalLookup);
            predicates.Add(util.BuildStatusFilterPredicate());

            //trim down to 3 most recent 
            var result = _dal.Where(predicates, null, 3, false, false,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.PublishDate, IsDescending = true }).Data;
            return Ok(result);
        }

        [HttpPost, Route("popular")]
        [ProducesResponseType(200, Type = typeof(List<MarketplaceItemModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetPopular()
        {
            //calculate most popular based on analytics counts
            var util = new MarketplaceUtil(_dal, _dalAnalytics, _dalLookup);
            var result = util.PopularItems();
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

                    // _dalAnalytics.Update(analytic, model.ID);
                    // _dalAnalytics.Update(analytic, analytic.ID.ToString());
                }
                result.Analytics = analytic;
            }
            //get related items
            var util = new MarketplaceUtil(_dal, _dalAnalytics, _dalLookup);
            result.SimilarItems = util.SimilarItems(result);

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

            if (matches == null || matches.Count() == 0)
            {
                _logger.LogWarning($"MarketplaceController|GetByName|No records found matching this name: {model.ID}");
                return BadRequest($"No records found matching this name: {model.ID}");
            }
            if (matches.Count() > 1)
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
            var util = new MarketplaceUtil(_dal, _dalAnalytics, _dalLookup);
            result.SimilarItems = util.SimilarItems(result);

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

            //  var allData = _dal.Where(s => model.Select(x => x.ID).ToList().Contains(s.Categories));
            // var result = _dal.Where(s => 
            //                         s.Categories.Any(t => model.ListItems.Select(x => x.ID).ToList().Contains(s.ID)),
            //                         model.Skip, model.Take, true);
            // test2.Where(t2 => test1.Any(t1 => t2.Contains(t1)));
            // model.Categories.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList()
            //var result = _dal.Where(s =>
            //                        model.ListItems.ToList().Any(s2 => s.Categories.Select(x => x.Value.ToString()).ToList().Contains(s2.ID)),
            //                        model.Skip, model.Take, true);
            //return _dal.Where(p => model.SiteIds.Contains(p.Site.ID), model.StartIndex, model.Length, true);

            //var catsFilter = model.Select(x => new MongoDB.Bson.BsonObjectId(new MongoDB.Bson.ObjectId(x))).ToList();
            //Any is not supported by Mongo DB driver
            //var result = _dal.Where(s => s.Categories.Except(catsFilter).Any();

            //build list of where clauses - one for each cat passed in
            List<Func<MarketplaceItem, bool>> predicates = new List<Func<MarketplaceItem, bool>>();
            foreach (var cat in model)
            {
                predicates.Add(x => x.Categories.Any(c => c.ToString().Equals(cat)));
            }

            var result = _dal.Where(predicates, null, null, true, false,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.Name });

            //var result = _dal.Where(s => s.Categories.Any(x => x.Equals(model)),
            //model.Contains(s.Categories.Any(c => c.ToString())).ListItems.ToList().Any(s2 => s.Categories.Select(x => x.Value.ToString()).ToList().Contains(s2.ID)),
            //null ,null, true);

            //var result = _dal.Where(s =>
            //                        model.Contains(s.Categories.Any(c => c.ToString())) .ListItems.ToList().Any(s2 => s.Categories.Select(x => x.Value.ToString()).ToList().Contains(s2.ID)),
            //                        model.Skip, model.Take, true);
            // var result = allData.Where(s => s.Categories.Contains(model));

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
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
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
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<IActionResult> AdvancedSearch([FromBody] MarketplaceSearchModel model
            , bool includeCloudLib = true, bool liveOnly = true)
        {

            //init and then flags set by user or system will determine which of the following get applied
            var result = new DALResult<MarketplaceItemModel>() { Data = new List<MarketplaceItemModel>() };

            //extract selected items within a list of items
            var cats = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.Where(x => x.EnumValue == LookupTypeEnum.Process).FirstOrDefault().Items.Where(x => x.Selected).ToList();
            var verts = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.Where(x => x.EnumValue == LookupTypeEnum.IndustryVertical).FirstOrDefault().Items.Where(x => x.Selected).ToList();
            var pubs = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.Where(x => x.EnumValue == LookupTypeEnum.Publisher).FirstOrDefault().Items.Where(x => x.Selected).ToList();
            var types = model.ItemTypes.Count == 0 ? new List<LookupItemFilterModel>() : model.ItemTypes.Where(x => x.Selected).ToList();

            //SM Apps, Hardware, etc. - anything other than sm profile types
            //User driven flag to select only a certain type. Determine if none are selected or if item type of sm app is selected.
            result = AdvancedSearchMarketplace(model, types, cats, verts, pubs, liveOnly);

            //SM Profiles
            //User driven flag to select only a certain type. Determine if none are selected or if item type of sm profile is selected.
            var includeSmProfileTypes = model.ItemTypes == null || model.ItemTypes.Where(x => x.Selected).Count() == 0 ||
                model.ItemTypes.Where(x => x.Selected && x.ID.Equals(_configUtil.MarketplaceSettings.SmProfile.TypeId)).Count() > 0;
            //Skip over this in certain scenarios. ie. admin section
            if (_configUtil.MarketplaceSettings.EnableCloudLibSearch && includeCloudLib && includeSmProfileTypes)
            {
                var resultCloudLib = await AdvancedSearchCloudLib(model, cats, verts, pubs);

                //unify the results, sort, handle paging
                result = MergeSortPageSearchedItems(result.Data, resultCloudLib, model);
            }

            if (result == null)
            {
                _logger.LogWarning($"MarketplaceController|AdvancedSearch|No records found matching the search criteria.");
                return BadRequest($"No records found matching the search criteria.");
            }
            return Ok(result);
        }


        /// <summary>
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private DALResult<MarketplaceItemModel> AdvancedSearchMarketplace([FromBody] MarketplaceSearchModel model
            , List<LookupItemFilterModel> types
            , List<LookupItemFilterModel> cats
            , List<LookupItemFilterModel> verts
            , List<LookupItemFilterModel> pubs
            , bool liveOnly = true)
        {
            var util = new MarketplaceUtil(_dal, _dalAnalytics, _dalLookup);

            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            //union passed in list w/ lookup list.
            var combinedCats = cats == null ? null : cats.Select(x => x.ID).ToArray();
            var combinedVerts = verts == null ? null : verts.Select(x => x.ID).ToArray();
            var combinedPubs = pubs == null ? null : pubs.Select(x => x.ID).ToArray();

            //build list of where clauses all combined into one predicate, 
            //using expression extension to allow for .Or or .And expression
            List<Func<MarketplaceItem, bool>> predicates = new List<Func<MarketplaceItem, bool>>();

            //limit to isActive
            predicates.Add(x => x.IsActive);

            //limit to publish status of live
            if (liveOnly)
            {
                predicates.Add(util.BuildStatusFilterPredicate());
            }

            //build where clause - one for each type passed in
            if (types != null && types.Count() > 0)//model.Categories != null)
            {
                predicates.Add(x => x.ItemTypeId != null && types.Any(y => y.ID.Equals(x.ItemTypeId.ToString())));
            }

            //build list of where clauses - one for each cat passed in
            Func<MarketplaceItem, bool> predicateCat = null;
            if (combinedCats != null && combinedCats.Length > 0)//model.Categories != null)
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

            //add where clause for the search terms - check against more fields
            Func<MarketplaceItem, bool> predicateQuery = null;
            if (!string.IsNullOrEmpty(model.Query))
            {
                //add series of where conditions
                predicateQuery = x => x.Name.ToLower().Contains(model.Query)
                    //or search on additional fields
                    || x.DisplayName.ToLower().Contains(model.Query)
                    || x.Description.ToLower().Contains(model.Query)
                    || x.Abstract.ToLower().Contains(model.Query)
                    || (x.MetaTags != null && x.MetaTags.Contains(model.Query))
                    || (x.Author != null && (x.Author.FirstName.Contains(model.Query) || x.Author.LastName.Contains(model.Query)));

                predicates.Add(predicateQuery);
            }

            //now execute the search - if we don't have predicates, then we just get all paged. 
            //each individual predicate (predicateCat, predicateVert, predicateQuery) acts as an OR within. Collectively,
            //the 3 predicates operate like an AND operator
            //(ie (if cat == 'foo' || cat == 'bar') AND (if vert == 'a' || vert == 'b') AND (name.contains('blah') || description.contains('blah'))
            ///--------------------------------------------------------------------------------------------------------
            /// NOTE:
            /// Because we are unifying two sources of information from separate sources, we need to wait on paging, sorting 
            /// and do not do this at the DB level. We have to get the filtered set of info and then apply a sort 
            /// and then the page. 
            ///--------------------------------------------------------------------------------------------------------
            //var result = predicates.Count == 0 ? _dal.GetAllPaged(model.Skip, model.Take, true, false) :
            //    _dal.Where(predicates, model.Skip, model.Take, true, false,
            //        new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
            //        new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName });
            //new - no paging,sorting
            var result = predicates.Count == 0 ? _dal.GetAllPaged(null, null, true, false) :
                _dal.Where(predicates, null, null, true, false,
                    new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
                    new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName });

            return result;
        }

        /// <summary>
        /// Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<List<MarketplaceItemModel>> AdvancedSearchCloudLib([FromBody] MarketplaceSearchModel model
            , List<LookupItemFilterModel> cats
            , List<LookupItemFilterModel> verts
            , List<LookupItemFilterModel> pubs
            )
        {
            var util = new MarketplaceUtil(_dal, _dalAnalytics, _dalLookup);

            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            var result = new List<MarketplaceItemModel>();
            //if publishers is a filter, then we skip CloudLib for now because search is trying to only show 
            //items for that publisher. In future, remove this once we store our own metadata. 
            //only search CloudLib if no publisher filter
            if (pubs.Count == 0)
            {
                //NEW: now search CloudLib.
                result = await _dalCloudLib.Where(model.Query,
                    cats.Count == 0 ? null : cats.Select(x => x.Name.ToLower()).ToList(),
                    verts.Count == 0 ? null : verts.Select(x => x.Name.ToLower()).ToList(),
                    _configUtil.CloudLibSettings?.ExcludedNodeSets);

                //check to see if the CloudLib returned data. 
                if (cats.Count == 0 && verts.Count == 0 && string.IsNullOrEmpty(model.Query)
                    && result.Count == 0)
                {
                    _logger.LogWarning($"MarketplaceController|AdvancedSearch|No CloudLib records found yet search criteria was wildcard.");
                }
            }

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
        private DALResult<MarketplaceItemModel> MergeSortPageSearchedItems(List<MarketplaceItemModel> set1, List<MarketplaceItemModel> set2,
            MarketplaceSearchModel model)
        {
            //get count before paging
            var count = set1.Count + set2.Count;
            //sort 2nd set but always put it after the regular marketplace items.  
            //set2 = set2.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.DisplayName).ToList();
            //combine the data, get the total count
            var combined = set1.Union(set2);
            //TBD - order by the unified result - order by type, then by featured then by name
            //combined = combined
            //    .OrderBy(x => x.Type?.DisplayOrder)
            //    .ThenBy(x => x.Type?.Name)
            //    .ThenByDescending(x => x.IsFeatured)
            //    .ThenBy(x => x.DisplayName); //.ToList();
            combined = combined
                .OrderByDescending(x => x.IsFeatured)
                .ThenBy(x => x.DisplayName); //.ToList();

            //now page the data. 
            combined = combined.Skip(model.Skip).Take(model.Take);
            return new DALResult<MarketplaceItemModel>() { 
                Count = count,
                Data = combined.ToList()
            };
        }

        #region Analytics endpoints
        /// <summary>
        /// Increment LikeCount for MarketplaceItem that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("analytics/IncrementLikeCount")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
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

            }
            else
            {
                // analytic.PageVisitCount += 1;
                analytic.LikeCount += 1;
                await _dalAnalytics.Update(analytic, null);
            }
            return Ok(new ResultMessageWithDataModel()
            {
                IsSuccess = true,
                Message = "Item was updated.",
                Data = analytic.LikeCount // return analytic.likecount
            });
        }

        /// <summary>
        /// Increment DislikeCount for MarketplaceItem that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("analytics/IncrementDislikeCount")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
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

            }
            else
            {
                analytic.DislikeCount += 1;
                await _dalAnalytics.Update(analytic, null);
            }
            return Ok(new ResultMessageWithDataModel()
            {
                IsSuccess = true,
                Message = "Item was updated.",
                Data = analytic.DislikeCount
            });
        }
        #endregion
    }
}
