namespace CESMII.Marketplace.Data.Entities
{
    public class Organization : AbstractEntity 
    {
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;

    }

}