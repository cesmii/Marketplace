using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.DAL.Models
{

    /// <summary>
    /// An external related marketplace item. Used in the admin tool to make selections
    /// where keeping data small is helpful
    /// </summary>
    public class ExternalSourceItemModel
    {
        public string RelatedId { get; set; }
        /// <summary>
        /// This represents the id from the native system plus the external source info.
        /// </summary>
        public ExternalSourceSimple ExternalSource { get; set; }
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Namespace { get; set; }
        public string Description { get; set; }
        public LookupItemModel RelatedType { get; set; }
    }

}