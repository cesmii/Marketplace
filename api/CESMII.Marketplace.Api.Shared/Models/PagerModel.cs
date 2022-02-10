using CESMII.Marketplace.Common.Enums;

namespace CESMII.Marketplace.Api.Shared.Models
{
    public class PagerModel
    {
        /// <summary>
        /// This is the start index
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// This is the number of items to include in the page
        /// </summary>
        public int Skip { get; set; }

    }

    public class PagerFilterSimpleModel : PagerModel
    {
        public string Query { get; set; }
    }

    public class PagerFilterLookupModel : PagerFilterSimpleModel
    {
        public int TypeId { get; set; }
    }


}
