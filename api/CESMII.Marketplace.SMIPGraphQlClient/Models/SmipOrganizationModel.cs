namespace CESMII.Marketplace.SmipGraphQlClient.Models
{
    public class SmipOrganizationModel: SmipAbstractModel
    {
        /*
        {
        "id": "72346",
        "displayName": "Sandbox",
        "description": "",
        "relativeName": "sandbox",
        "parentOrganization": null
        }         
         */
        public string displayName { get; set; }
        public string description { get; set; }
        public string relativeName { get; set; }
        public SmipOrganizationModel parentOrganization { get; set; }
    }

}
