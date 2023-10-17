using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Net.Mime;

using Newtonsoft.Json;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.ExternalSources.Models;
using Microsoft.Extensions.Configuration;
using CESMII.Marketplace.Data.Repositories;

namespace CESMII.Marketplace.DAL.ExternalSources
{
    public class BennitResponseEntity: ExternalAbstractEntity
    {
        public string Id { get; set; }
        public string Headline { get; set; }
        public string Abstract { get; set; }
        public string Experience { get; set; }
        public string Availability { get; set; }
        public string Locations { get; set; }
        public string IsCoach { get; set; }
        public string BannerImage { get; set; }
        public string PortraitImage { get; set; }
        public DateTime? Updated_At { get; set; }
        public string OrgName { get; set; }
        public string FK_Org_Id { get; set; }
        //properties not yet in data
        public List<BennitSkills> Skills { get; set; }
        //public List<BennitSmProfileLink> RelatedProfiles { get; set; }
        //public List<BennitSmProfileLink> Certifications { get; set; }
    }

    public class BennitSmProfileLink
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class BennitSkills
    {
        public string Id { get; set; }
        public string Skill_Name { get; set; }
        public string FK_Solver_Id { get; set; }
        //public string Duration { get; set; }
        //public string Level { get; set; }
    }

    public class BennitRelatedItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class BennitConfigData
    {
        public List<KeyValuePair<string, string>> Urls { get; set; }
    }

    public class BennitErrorResponse
    {
        public string Error { get; set; }
    }

    /// <summary>
    /// Call Bennit.AI API. Note url must include trailing slash. Otherwise, it treats it as a post. 
    /// </summary>
    public class BennitDAL : ExternalBaseDAL<BennitResponseEntity, MarketplaceItemModel> , IExternalDAL<MarketplaceItemModel>
    {
        private enum SearchModeEnum
        {
            detail,
            search
        };

        protected ExternalSourceModel _configSmProfile;
        protected List<ImageItemModel> _images;
        // Custom implementation of the Data property in the DB. 
        // This can be unique for each source.
        protected BennitConfigData _configCustom;

        public BennitDAL(ExternalSourceModel config,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IHttpApiFactory httpApiFactory,
            IMongoRepository<ImageItem> repoImages,
            IConfiguration configuration,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ProfileItem> repoExternalItem
            ) : base(dalExternalSource, config, httpApiFactory, repoImages)
        {
            this.Init(dalExternalSource);
        }

        public BennitDAL(
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IHttpApiFactory httpApiFactory,
            IMongoRepository<ImageItem> repoImages,
            IConfiguration configuration,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IMongoRepository<MarketplaceItem> repoMarketplace,
            IMongoRepository<ProfileItem> repoExternalItem
            ) : base(dalExternalSource, "bennit", httpApiFactory, repoImages)
        {
            this.Init(dalExternalSource);
        }

        protected void Init(IDal<ExternalSource, ExternalSourceModel> dalExternalSource)
        {
            //init some stuff we will use during the mapping methods
            //go get the config for this source
            _configSmProfile = dalExternalSource.Where(x => x.Code.ToLower().Equals("cloudlib")
                    , null, null, false, true).Data?.FirstOrDefault();
            if (_configSmProfile == null)
            {
                throw new ArgumentNullException($"External Source Config: {"cloudlib"}");
            }

            //get default images
            _images = GetImagesByIdList(new List<string>() {
                _config.DefaultImageBanner?.ID,
                _config.DefaultImageLandscape?.ID,
                _config.DefaultImagePortrait?.ID,
                _config.DefaultImageBanner?.ID,
                _config.DefaultImagePortrait?.ID,
                _config.DefaultImageBanner?.ID
            }).Result;

            //set up the _config data attribute specifically for the Bennit DAL
            _configCustom = JsonConvert.DeserializeObject<BennitConfigData>(_config.Data);
        }

        public async Task<MarketplaceItemModel> GetById(string id) {

            string url = _configCustom.Urls.Find(x => x.Key.ToLower().Equals("getbyid")).Value;
            if (string.IsNullOrEmpty(url)) throw new InvalidOperationException($"External Source|Url 'GetById' is not properly configured.");

            MultipartFormDataContent formData = PrepareFormData(SearchModeEnum.detail, id);
            var response = await base.ExecuteApiCall(PrepareApiConfig(url, formData)); 
            if (!response.IsSuccess) return null;

            //check for error response from server
            if (IsErrorResponse(response.Data))
            {
                return null;
            }

            //no error response, proceed
            var entity = JsonConvert.DeserializeObject<BennitResponseEntity>(response.Data);
            if (entity == null) return null;

            return MapToModel(entity, true);
        }

        public async Task<DALResultWithSource<MarketplaceItemModel>> GetManyById(List<string> ids)
        {
            _logger.Log(NLog.LogLevel.Info, $"BennitDAL|GetManyById|Not supported for this data source.");
            return await Task.Run(() =>
            {
                return new DALResultWithSource<MarketplaceItemModel>()
                { Count = 0, Data = new List<MarketplaceItemModel>(), SourceId = _config.ID, Cursor = null };
            });
        }

        public async Task<DALResultWithSource<MarketplaceItemModel>> GetAll() {
            //setting to very high to get all...this is called by admin which needs full list right now for dropdown selection
            var result = await this.Where(null, new SearchCursor() { PageIndex = 0, Skip = 0, Take = 999 } ); 
            return result;
        }

        public async Task<DALResultWithSource<MarketplaceItemModel>> Where(string query,
            SearchCursor cursor, 
            List<string> ids = null, List<string> processes = null, List<string> verticals = null,
            List<string> exclude = null)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            /*
            //possible future usage
            var keywords = new List<string>();

            //append list of ids, processes, verticals
            if (ids != null)
            {
                keywords = keywords.Union(ids).ToList();
            }
            if (processes != null)
            {
                keywords = keywords.Union(processes).ToList();
            }
            if (verticals != null)
            {
                keywords = keywords.Union(verticals).ToList();
            }

            //inject wildcard to get all if keywords count == 0 or inject query
            if (string.IsNullOrEmpty(query) && keywords.Count == 0) keywords.Add("*");
            if (!string.IsNullOrEmpty(query)) keywords.Add(query);
            */

            //business rule - do not return results if query value is not populated. 
            //they do not want to return all items.
            if (string.IsNullOrEmpty(query))
            {
                cursor.TotalCount = null;
                return new DALResultWithSource<MarketplaceItemModel>()
                { Count = 0, Data = new List<MarketplaceItemModel>(), SourceId = _config.ID, Cursor = cursor };
            }
            //if (string.IsNullOrEmpty(query)) query = "";

            //for now, just support query value. still pass in the other stuff so 
            //we have it for future enhancements. 
            string url = _configCustom.Urls.Find(x => x.Key.ToLower().Equals("search")).Value;
            if (string.IsNullOrEmpty(url)) throw new InvalidOperationException($"External Source|Url 'search' is not properly configured.");

            MultipartFormDataContent formData = PrepareFormData(SearchModeEnum.search, query, cursor.Skip, cursor.Take);
            var response = await base.ExecuteApiCall(PrepareApiConfig(url, formData));
            if (!response.IsSuccess) return null;

            //check for error response from server
            if (IsErrorResponse(response.Data))
            {
                cursor.TotalCount = null;
                return new DALResultWithSource<MarketplaceItemModel>()
                { Count = 0, Data = new List<MarketplaceItemModel>(), SourceId = _config.ID, Cursor = cursor };
            }

            //no error response, proceed
            var entities = JsonConvert.DeserializeObject<List<BennitResponseEntity>>(response.Data);

            //map the data to the final result
            cursor.TotalCount = entities.Count;  //TBD - get total count not just returned count
            var result = new DALResultWithSource<MarketplaceItemModel>
            {
                Count = entities.Count,
                Data = MapToModels(entities, false),
                SummaryData = null, 
                SourceId = _config.ID,
                Cursor = cursor
            };

            _logger.Log(NLog.LogLevel.Warn, $"BennitDAL|Where|Duration: { timer.ElapsedMilliseconds}ms.");
            return result;
        }

        public async Task<ExternalItemExportModel> Export(string id)
        {
            throw new NotSupportedException();
        }

        private MultipartFormDataContent PrepareFormData(SearchModeEnum mode, string value, int? skip = null, int? take = null)
        {
            var formData = new MultipartFormDataContent();
            //case sensitive
            formData.Add(new StringContent(_config.AccessToken), "partnerkey");
            formData.Add(new StringContent(mode.ToString()), "query");
            formData.Add(new StringContent(value), "value");
            if (skip.HasValue) formData.Add(new StringContent(skip.ToString()), "skip");
            if (take.HasValue) formData.Add(new StringContent(take.ToString()), "take");
            return formData;
        }

        protected override HttpApiConfig PrepareApiConfig(string url, MultipartFormDataContent formData)
        {
            return new HttpApiConfig()
            {
                BaseAddress = _config.BaseUrl,
                Url = url,
                Body = formData,
                BodyContentType = "multipart/form-data",
                Method = HttpMethod.Post,
                Headers = PrepareHeaders()
            };
        }

        private bool IsErrorResponse(string data)
        {
            //check for error response from external server. This will be a json string with an error tag.
            try
            {
                var err = JsonConvert.DeserializeObject<BennitErrorResponse>(data);
                if (err != null && !string.IsNullOrEmpty(err.Error))
                {
                    _logger.Log(NLog.LogLevel.Error, $"BennitDAL|CheckForErrorResponse|Error returned from API|{err.Error}.");
                    return true;
                }
            }
            catch (JsonException)
            {
                //do nothing
                //the exception means the data returned is not an error. 
            }
            return false;
        }

        protected override MarketplaceItemModel MapToModel(BennitResponseEntity entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new MarketplaceItemModel
                {
                    ID = entity.Id,
                    //ensure this value is always without spaces and is lowercase. 
                    Name = entity.Headline.ToLower().Trim().Replace(" ", "-").Replace("_", "-"),
                    DisplayName = entity.Headline?.Trim(),
                    Abstract = entity.Abstract,
                    Description = string.IsNullOrEmpty(entity.Experience) ? "" : $"<p>{entity.Experience}</p>",
                    Type = _config.ItemType,
                    AuthorId = null,
                    Created = new DateTime(0),
                    Updated = new DateTime(0),
                    PublishDate = entity.Updated_At,
                    Version = null,
                    //Type = new LookupItemModel() { ID = entity.TypeId, Name = entity.Type.Name }
                    MetaTags = entity.Skills != null ? entity.Skills.Select(x => x.Skill_Name).ToList() : null,
                    // Categories = MapToModelLookupData(entity.Categories, _lookupItemsAll.Where(x => x.TypeId.Equals((int)LookupTypeEnum.Categories)).ToList()),
                    // IndustryVerticals = MapToModelLookupData(entity.IndustryVerticals, _lookupItemsAll.Where(x => x.TypeId.Equals((int)LookupTypeEnum.IndustryVerticals)).ToList()),
                    // MarketplaceStatus = MapToModelLookupData(entity.MarketplaceStatus, _lookupItemsAll.Where(x => x.TypeId.Equals((int)LookupTypeEnum.MarketplaceStatus)).ToList())
                    //Categories = MapToModelLookupItems(entity.Categories, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.Process)).ToList()),
                    //IndustryVerticals = MapToModelLookupItems(entity.IndustryVerticals, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.IndustryVertical)).ToList()),
                    //Status = MapToModelLookupItem(entity.StatusId, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.MarketplaceStatus)).ToList()),
                    //Analytics = MapToModelMarketplaceItemAnalyticsData(entity.ID, _marketplaceItemAnalyticsAll),
                    Publisher = new PublisherModel() {ID = _config.Publisher.ID, Verified = true, Description = _config.Publisher.Description,
                        CompanyUrl = _config.Publisher.CompanyUrl, Name = _config.Publisher.Name, DisplayName = _config.Publisher.DisplayName,
                        AllowFilterBy = _config.Publisher.AllowFilterBy
                    },
                    IsActive = true,
                    IsFeatured = false,
                    //populate with values from source if present
                    ImagePortrait = MapToModelImage( entity.PortraitImage, $"{entity.Headline.Replace(" ","-")} - portrait",
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImagePortrait?.ID))),
                    ImageBanner = MapToModelImage(entity.BannerImage, $"{entity.Headline.Replace(" ", "-")} - banner", 
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageBanner?.ID))),
                    ImageLandscape = MapToModelImage(entity.BannerImage, $"{entity.Headline.Replace(" ", "-")} - landscape",
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageLandscape?.ID))),
                    //we expect this is unique - per source
                    ExternalSource = new ExternalSourceSimple() { ID = entity.Id, SourceId = _config.ID, Code = _config.Code }
                };
                //get additional data under certain scenarios
                if (verbose)
                {
                    result.Description +=
                        (string.IsNullOrEmpty(entity.OrgName) ? "" : $"<p><b>Organization</b>: {entity.OrgName}</p>") +
                        (string.IsNullOrEmpty(entity.Availability) ? "" : $"<p><b>Availability</b>: {entity.Availability}</p>") +
                        (string.IsNullOrEmpty(entity.Locations) ? "" : $"<p><b>Locations</b>: {entity.Locations}</p>");

                    //map related profiles
                    /*
                    var relatedProfiles = MapToModelRelatedProfiles(
                        new LookupItemModel() { ID = "1", DisplayOrder = 1, Code = "expertise", Name = "Expertise In" },
                        entity.RelatedProfiles);
                    //map other related data
                    var relatedCertifications = MapToModelRelatedItems(
                        new LookupItemModel() { ID = "2", DisplayOrder = 2, Code = "certification", Name = "Certifications" },
                        entity.Certifications);

                    //map related items into specific buckets
                    result.RelatedItemsGrouped = GroupAndMergeRelatedItems(relatedProfiles, relatedCertifications);
                    */
                }
                return result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Map profiles to related items
        /// </summary>
        protected ImageItemModel MapToModelImage(string imagePath, string fileName, ImageItemModel defaultImage)
        {
            if (string.IsNullOrEmpty(imagePath)) return defaultImage;
            return new ImageItemModel() { ID="0", Src=imagePath, FileName= fileName};
        }

        /// <summary>
        /// Map profiles to related items
        /// </summary>
        protected List<MarketplaceItemRelatedModel> MapToModelRelatedProfiles(LookupItemModel type, List<BennitSmProfileLink> items)
        {
            if (items == null)
            {
                return new List<MarketplaceItemRelatedModel>();
            }

            return items.Select(x => new MarketplaceItemRelatedModel()
                {
                    RelatedId = x.Id,
                    Abstract = null,
                    DisplayName = x.Name,
                    Description = null,
                    Name = x.Name,
                    //Type = new LookupItemModel() {  }, // x.Type,
                    Version = null,
                    ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_configSmProfile.DefaultImagePortrait.ID)),
                    ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_configSmProfile.DefaultImageLandscape.ID)),
                    //assumes only one related item per type
                    //TBD - move this to appSettings.
                    RelatedType = type
                }).ToList();
        }

        protected List<MarketplaceItemRelatedModel> MapToModelRelatedItems(LookupItemModel type, List<BennitSmProfileLink> items)
        {
            if (items == null)
            {
                return new List<MarketplaceItemRelatedModel>();
            }

            return items.Select(x => new MarketplaceItemRelatedModel()
            {
                RelatedId = x.Id,
                Abstract = null,
                DisplayName = x.Name,
                Description = null,
                Name = x.Name,
                //Type = new LookupItemModel() {  }, // x.Type,
                Version = null,
                ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_configSmProfile.DefaultImagePortrait.ID)),
                ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_configSmProfile.DefaultImageLandscape.ID)),
                //assumes only one related item per type
                //TBD - move this to appSettings.
                RelatedType = type
            }).ToList();
        }
    }
}