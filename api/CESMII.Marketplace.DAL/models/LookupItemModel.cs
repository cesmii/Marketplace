namespace CESMII.Marketplace.DAL.Models
{
    public class LookupItemModel : AbstractModel
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public LookupTypeModel LookupType { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class LookupItemFilterModel : LookupItemModel
    {
        public bool Selected { get; set; } = false;
    }



}