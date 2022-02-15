namespace CESMII.Marketplace.DAL.Models
{
    using CESMII.Marketplace.Common.Enums;

    public class LookupTypeModel
    {
        public string Name { get; set; }

        public LookupTypeEnum EnumValue { get; set; }

        public int DisplayOrder { get; set; }
    }
}