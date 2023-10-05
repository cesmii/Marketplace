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
using CESMII.Marketplace.ExternalSources.Models;

namespace CESMII.Marketplace.ExternalSources.DAL
{
    public class BennitResponseEntity: ExternalAbstractEntity
    {
        public string Id { get; set; }
        public string Headline { get; set; }
        public string Experience { get; set; }
        public string Availability { get; set; }
        public string Locations { get; set; }
        public string IsCoach { get; set; }
        public string ImagePath { get; set; }
        public string OrgName { get; set; }
        public string FK_Org_Id { get; set; }
        //properties not yet in data
        public List<string> Skills { get; set; }
        public List<BennitSmProfileLink> RelatedProfiles { get; set; }
        public List<BennitSmProfileLink> Certifications { get; set; }
    }

    public class BennitSmProfileLink
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class BennitRelatedItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
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

        private readonly MarketplaceItemConfig _configSmProfile;
        protected List<ImageItemModel> _images;

        public BennitDAL(
            ExternalSourceModel config,
            IHttpApiFactory httpApiFactory,
            ConfigUtil configUtil,
            IDal<ImageItem, ImageItemModel> dalImages
            ) : base (config, httpApiFactory)
        {
            //init some stuff we will use during the mapping methods
            _configSmProfile = configUtil.MarketplaceSettings.SmProfile;

            //get default images
            _images = dalImages.Where(
                x => x.ID.Equals(_config.DefaultImageBanner?.ID) ||
                x.ID.Equals(_config.DefaultImageLandscape?.ID) ||
                x.ID.Equals(_config.DefaultImagePortrait?.ID) ||
                x.ID.Equals(_configSmProfile.DefaultImageIdLandscape) ||
                x.ID.Equals(_configSmProfile.DefaultImageIdPortrait) ||
                x.ID.Equals(_configSmProfile.DefaultImageIdBanner)
                //|| x.ID.Equals(_config.DefaultImageIdSquare)
                , null, null, false, false).Data;

            //set some default settings specific for this external source. 
            config.Publisher.DisplayViewAllLink = false;
        }

        public async Task<MarketplaceItemModel> GetById(string id) {

            string url = _config.Urls.Find(x => x.Key.ToLower().Equals("getbyid")).Value;
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
            var entities = JsonConvert.DeserializeObject<List<BennitResponseEntity>>(response.Data);
            if (entities == null) return null;

            //send warning to log if more than one item returned
            if (entities.Count > 1)
            {
                _logger.Log(NLog.LogLevel.Warn, $"BennitDAL|GetById|Id: {id}|Expected one item, API returned {entities.Count}.");
            }

            return MapToModel(entities[0]);
        }

        public async Task<List<MarketplaceItemModel>> GetManyById(List<string> ids)
        {
            throw new NotSupportedException();
        }

        public async Task<List<MarketplaceItemModel>> GetAll() {
            //setting to very high to get all...this is called by admin which needs full list right now for dropdown selection
            var result = await this.Where(null, 0, 999); 
            return result.Data;
        }

        public async Task<DALResult<MarketplaceItemModel>> Where(string query, int? skip = null, int? take = null, string? startCursor = null, string? endCursor = null, bool noTotalCount = false,
            List<string> ids = null, List<string> processes = null, List<string> verticals = null)
        {
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

            if (string.IsNullOrEmpty(query)) query = "";

            //for now, just support query value. still pass in the other stuff so 
            //we have it for future enhancements. 
            string url = _config.Urls.Find(x => x.Key.ToLower().Equals("search")).Value;
            if (string.IsNullOrEmpty(url)) throw new InvalidOperationException($"External Source|Url 'GetById' is not properly configured.");

            MultipartFormDataContent formData = PrepareFormData(SearchModeEnum.search, query);
            var response = await base.ExecuteApiCall(PrepareApiConfig(url, formData));
            if (!response.IsSuccess) return null;

            //check for error response from server
            if (IsErrorResponse(response.Data))
            {
                return new DALResult<MarketplaceItemModel>()
                { Count = 0, Data = new List<MarketplaceItemModel>() };
            }

            //no error response, proceed
            var entities = JsonConvert.DeserializeObject<List<BennitResponseEntity>>(response.Data);

            //map the data to the final result
            var result = new DALResult<MarketplaceItemModel>
            {
                Count = entities.Count,
                Data = MapToModels(entities, false),
                SummaryData = null
            };

            return result;
        }

        private MultipartFormDataContent PrepareFormData(SearchModeEnum mode, string value)
        {
            var formData = new MultipartFormDataContent();
            //case sensitive
            formData.Add(new StringContent(_config.AccessToken), "partnerkey");
            formData.Add(new StringContent(mode.ToString()), "query");
            formData.Add(new StringContent(value), "value");
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
                if (err != null)
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
                    DisplayName = entity.Headline,
                    Abstract = entity.Experience,   //trim this down to first 300 characters
                    Description = entity.Experience,
                    Type = _config.ItemType,
                    AuthorId = null,
                    Created = new DateTime(0),
                    Updated = new DateTime(0),
                    PublishDate = null,
                    Version = null,
                    //Type = new LookupItemModel() { ID = entity.TypeId, Name = entity.Type.Name }
                    MetaTags = entity.Skills,
                    // Categories = MapToModelLookupData(entity.Categories, _lookupItemsAll.Where(x => x.TypeId.Equals((int)LookupTypeEnum.Categories)).ToList()),
                    // IndustryVerticals = MapToModelLookupData(entity.IndustryVerticals, _lookupItemsAll.Where(x => x.TypeId.Equals((int)LookupTypeEnum.IndustryVerticals)).ToList()),
                    // MarketplaceStatus = MapToModelLookupData(entity.MarketplaceStatus, _lookupItemsAll.Where(x => x.TypeId.Equals((int)LookupTypeEnum.MarketplaceStatus)).ToList())
                    //Categories = MapToModelLookupItems(entity.Categories, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.Process)).ToList()),
                    //IndustryVerticals = MapToModelLookupItems(entity.IndustryVerticals, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.IndustryVertical)).ToList()),
                    //Status = MapToModelLookupItem(entity.StatusId, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.MarketplaceStatus)).ToList()),
                    //Analytics = MapToModelMarketplaceItemAnalyticsData(entity.ID, _marketplaceItemAnalyticsAll),
                    Publisher = new PublisherModel() {ID = _config.Publisher.ID, Verified = true, Description = _config.Publisher.Description,
                        CompanyUrl = _config.Publisher.CompanyUrl, Name = _config.Publisher.Name, DisplayName = _config.Publisher.DisplayName
                    },
                    IsActive = true,
                    IsFeatured = false,
                    //populate with values from source if present
                    ImagePortrait = MapToModelImage( entity.ImagePath, $"{entity.Headline.Replace(" ","-")} - portrait",
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImagePortrait?.ID))),
                    ImageBanner = MapToModelImage(entity.ImagePath, $"{entity.Headline.Replace(" ", "-")} - banner", 
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageBanner?.ID))),
                    ImageLandscape = MapToModelImage(entity.ImagePath, $"{entity.Headline.Replace(" ", "-")} - landscape",
                        _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageLandscape?.ID))),
                };
                //get additional data under certain scenarios
                if (verbose)
                {
                    //map related profiles 
                    var relatedProfiles = MapToModelRelatedProfiles(
                        new LookupItemModel() { ID = "1", DisplayOrder = 1, Code = "expertise", Name = "Expertise In" },
                        entity.RelatedProfiles);
                    //map other related data
                    var relatedCertifications = MapToModelRelatedItems(
                        new LookupItemModel() { ID = "2", DisplayOrder = 2, Code = "certification", Name = "Certifications" },
                        entity.Certifications);

                    //map related items into specific buckets
                    result.RelatedItemsGrouped = GroupAndMergeRelatedItems(relatedProfiles, relatedCertifications);
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
                    ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_configSmProfile.DefaultImageIdPortrait)),
                    ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_configSmProfile.DefaultImageIdLandscape)),
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
                ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_configSmProfile.DefaultImageIdPortrait)),
                ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_configSmProfile.DefaultImageIdLandscape)),
                //assumes only one related item per type
                //TBD - move this to appSettings.
                RelatedType = type
            }).ToList();
        }


        public virtual void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            //set flag so we only run dispose once.
            _disposed = true;
        }
    }
}