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
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL.ExternalSources;
using CESMII.Marketplace.DAL.ExternalSources.Models;
using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.Api.Shared.Utils;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped)), Route("api/admin/externalsource")]
    public class AdminExternalSourceController : BaseController<AdminExternalSourceController>
    {
        private readonly IExternalSourceFactory<AdminMarketplaceItemModel> _sourceFactory;
        private readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;

        public AdminExternalSourceController(
            IExternalSourceFactory<AdminMarketplaceItemModel> sourceFactory,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            UserDAL dalUser,
            ConfigUtil config, ILogger<AdminExternalSourceController> logger)
            : base(config, logger, dalUser)
        {
            _sourceFactory = sourceFactory;
            _dalExternalSource = dalExternalSource;
        }

        #region Admin UI
        [HttpPost, Route("init")]
        [ProducesResponseType(200, Type = typeof(AdminMarketplaceItemModel))]
        [ProducesResponseType(400)]
        public IActionResult Init(string code)
        {
            code = code.ToLower();
            //get external source config, then instantiate object using info in the config by 
            //calling external source factory.
            //get by name - we have to ensure our external sources config data maintains a unique name.
            var sources = _dalExternalSource.Where(x => x.Code.ToLower().Equals(code), null, null, false, false).Data;
            if (sources == null || sources.Count == 0)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|Invalid source : {code}");
                throw new ArgumentException("Invalid source code");
            }
            else if (sources.Count > 1)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|External Source. Too many matches for {code}");
                throw new ArgumentException("Too many source code matches");
            }

            var result = new AdminMarketplaceItemModel();

            //pre-populate list of look up items for industry verts and categories
            //TBD - for now, we don't use this. uncomment this if we start capturing profile's verticals, processes
            /*
            var lookupItems = _dalLookup.Where(x => x.LookupType.EnumValue == LookupTypeEnum.IndustryVertical
                || x.LookupType.EnumValue == LookupTypeEnum.Process, null, null, false, false).Data;
            result.IndustryVerticals = lookupItems.Where(x => x.LookupType.EnumValue == LookupTypeEnum.IndustryVertical)
                .Select(itm => new LookupItemFilterModel
                {
                    ID = itm.ID,
                    Name = itm.Name,
                    IsActive = itm.IsActive,
                    DisplayOrder = itm.DisplayOrder
                }).ToList();

            result.Categories = lookupItems.Where(x => x.LookupType.EnumValue == LookupTypeEnum.Process)
                .Select(itm => new LookupItemFilterModel
                {
                    ID = itm.ID,
                    Name = itm.Name,
                    IsActive = itm.IsActive,
                    DisplayOrder = itm.DisplayOrder
                }).ToList();
            */

            //default some values
            result.Name = "";
            result.DisplayName = "";
            result.Abstract = "";
            result.Description = "";
            result.Version = "";
            result.PublishDate = DateTime.Now.Date;
            result.ExternalSource = new ExternalSourceSimple() { Code = sources[0].Code, SourceId=sources[0].ID };
            
            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        [ProducesResponseType(200, Type = typeof(AdminMarketplaceItemModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByID([FromBody] ExternalSourceRequestModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var dalSource = await GetExternalSourceDAL(model.Code);
            var result = await dalSource.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|GetByID|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            return Ok(result);
        }

        /// <summary>
        /// Update an existing MarketplaceItem that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Upsert")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Upsert([FromBody] AdminMarketplaceItemModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminExternalSourceController|Update|Invalid model");
                return BadRequest("Marketplace|Update|Invalid model");
            }
            var dalSource = await GetExternalSourceDAL(model.ExternalSource.Code);
            var record = await dalSource.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|Update|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            else
            {
                var result = await dalSource.Upsert(model, UserID);
                if (result < 0)
                {
                    _logger.LogWarning($"AdminExternalSourceController|Update|Could not update marketplaceItem. Invalid id:{model.ID}.");
                    return BadRequest("Could not update profile MarketplaceItem. Invalid id.");
                }
                _logger.LogInformation($"AdminExternalSourceController|Update|Updated MarketplaceItem. Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was updated.",
                    Data = model.ID
                });
            }

        }

        /// <summary>
        /// Delete an existing profile. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Delete")]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] ExternalSourceRequestModel model)
        {
            var dalSource = await GetExternalSourceDAL(model.Code);
            var result = await dalSource.Delete(model, UserID);
            if (result < 0)
            {
                _logger.LogWarning($"AdminExternalSourceController|Delete|Could not remove relationships. Invalid id:{model.ID}.");
                return BadRequest("Could not remove relationships. Invalid id.");
            }
            _logger.LogInformation($"AdminExternalSourceController|Delete|Removed relationships. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Relationships were removed." });
        }

        /// <summary>
        /// Admin Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// The admin difference is that it will not include CloudLib profiles in the search
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Search")]
        [ProducesResponseType(200, Type = typeof(List<MarketplaceItemModel>))]
        public async Task<IActionResult> Search([FromBody] MarketplaceSearchModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|Search|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //Admin external source DAL - gets only the items that have related items in local db. 
            var listSearchExternalSources = new List<Task>();
            var sources = _dalExternalSource.Where(x => x.Enabled && x.IsActive, null, null, false, false).Data;
            foreach (var src in sources)
            {
                if (!src.Enabled || string.IsNullOrEmpty(src.AdminTypeName)) continue;

                //cursor for this specific source - look at cached cursors and return the best cursor 
                //option or new cursor for new searches
                var nextCursor = MarketplaceUtil.PrepareSearchCursor(model, src.ID);

                var externalTask = AdvancedSearchExternal(model, src, nextCursor);
                listSearchExternalSources.Add(externalTask);
            }

            //run query calls in parallel
            //long swWhenAllStart = timer.ElapsedMilliseconds;
            var allTasks = Task.WhenAll(listSearchExternalSources);
            //wrap exception handling around the tasks execution so no task exception gets lost
            try
            {
                _logger.LogInformation($"AdminExternalSourceController|AdvancedSearch|Await outcome of .whenAll");
                await allTasks;
                //AdvancedSearchLogDurationTime("When All", timer.ElapsedMilliseconds - swWhenAllStart);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"AdminExternalSourceController|AdvancedSearch|All Tasks Exception|{ex.Message}.");
                throw allTasks.Exception;
            }

            //get the tasks results into format we can use
            _logger.LogInformation($"AdminExternalSourceController|AdvancedSearch|Executing tasks using await...");

            var resultSearches = new List<DALResultWithSource<AdminMarketplaceItemModel>>();
            foreach (Task<DALResultWithSource<AdminMarketplaceItemModel>> t in listSearchExternalSources)
            {
                var r = await t;
                resultSearches.Add(r);
            }

            _logger.LogInformation($"AdminExternalSourceController|AdvancedSearch|Unifying results...");

            //unify the results, sort, handle paging
            var result = MergeSortPageSearchedItems(model, resultSearches);


            if (result == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|Search|No records found.");
                return BadRequest($"No records found.");
            }

            return Ok(result);
        }

        #endregion

        private async Task<DALResultWithSource<AdminMarketplaceItemModel>> AdvancedSearchExternal(
            MarketplaceSearchModel model,
            ExternalSourceModel src,
            SearchCursor nextCursor
            )
        {
            //now perform the search(es)
            var dalSource = await _sourceFactory.InitializeAdminSource(src);
            //the DAL will pull list of ids from repo directly
            var result = await dalSource.GetRelatedItems();
            result.Cursor = nextCursor;
            //now trim by page if needed
            if (result.Count > nextCursor.Skip)
            {
                result.Data = result.Data.Skip(nextCursor.Skip).ToList();
                if (nextCursor.Take.HasValue && nextCursor.Take.Value > result.Count) result.Data = result.Data.Take(nextCursor.Take.Value).ToList();
            }
            return result;
        }

        //TBD - merge common parts into the MarketplaceController code - perhaps move to marketplace util for shared parts. 
        /// <summary>
        /// Because we are unifying multiple sources of information from separate sources, we need to wait on paging 
        /// and do not do this at the DB level. We have to get the filtered set of info and then apply a sort 
        /// and then the page. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static DALResultWithCursors<AdminMarketplaceItemModel> MergeSortPageSearchedItems(
            MarketplaceSearchModel model,
            List<DALResultWithSource<AdminMarketplaceItemModel>> sets)
        {
            var pageIndex = model.Skip / model.Take;

            //if current cursors is null or skip == 0, start fresh
            //if current cursors not null and skip != null, then we assume we are paging and we need previous cursor
            //  to inform next cursor

            //union results into one set, order set and then find min/max of each source type.
            List<AdminMarketplaceItemModel> resultData = new List<AdminMarketplaceItemModel>();
            foreach (var item in sets)
            {
                if (item.Data != null) resultData = resultData.Union(item.Data).ToList();
            }
            //now order, then filter
            //new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
            //new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName });
            var result = new DALResultWithCursors<AdminMarketplaceItemModel>()
            {
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
                    result.Data.Find(x => string.IsNullOrEmpty(x.ExternalSource?.SourceId)) :
                    result.Data.Find(x => !string.IsNullOrEmpty(x.ExternalSource?.SourceId) && x.ExternalSource?.SourceId == item.SourceId);
                var last = string.IsNullOrEmpty(item.SourceId) ?
                    result.Data.FindLast(x => string.IsNullOrEmpty(x.ExternalSource?.SourceId)) :
                    result.Data.FindLast(x => !string.IsNullOrEmpty(x.ExternalSource?.SourceId) && x.ExternalSource?.SourceId == item.SourceId);

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

        private async Task<IAdminExternalDAL<AdminMarketplaceItemModel>> GetExternalSourceDAL(string code)
        {
            code = code.ToLower();
            //get external source config, then instantiate object using info in the config by 
            //calling external source factory.
            //get by name - we have to ensure our external sources config data maintains a unique name.
            var sources = _dalExternalSource.Where(x => x.Code.ToLower().Equals(code), null, null, false, false).Data;
            if (sources == null || sources.Count == 0)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|Invalid source : {code}");
                throw new ArgumentException("Invalid source code");
            }
            else if (sources.Count > 1)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|External Source. Too many matches for {code}");
                throw new ArgumentException("Too many source code matches");
            }

            var src = sources[0];
            if (!src.Enabled)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|External Source. Source not enabled. {code}");
                throw new ArgumentException("Invalid source code");
            }

            //now perform the get by id call
            return await _sourceFactory.InitializeAdminSource(src);
        }

    }


}
