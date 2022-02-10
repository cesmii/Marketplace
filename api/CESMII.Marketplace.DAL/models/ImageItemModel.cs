namespace CESMII.Marketplace.DAL.Models
{
    public class ImageItemSimpleModel : AbstractModel
    {
        /// <summary>
        /// Note not required. If null, available for all
        /// </summary>
        public string MarketplaceItemId { get; set; }
        /// <summary>
        /// This is image/*
        /// </summary>
        public string Type { get; set; }
        public string FileName { get; set; }
    }

    public class ImageItemModel : ImageItemSimpleModel
    {
        /// <summary>
        /// This can either be a base64 string or a url to point to an external image
        /// </summary>
        public string Src { get; set; }
    }
}
