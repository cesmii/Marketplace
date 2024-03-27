namespace CESMII.Marketplace.Common.Enums
{
    using System.ComponentModel;

    public enum LookupTypeEnum
    {
        [Description("Processes")]
        Process = 1,
        [Description("Industry Vertical")]
        IndustryVertical = 2,
        [Description("Marketplace Status")]
        MarketplaceStatus = 5,
        [Description("Publishers")]
        Publisher = 6 , //only used in front end and controller
        [Description("Request Info")]
        RequestInfo = 7,
        [Description("Status")]
        TaskStatus = 8,
        [Description("Membership Status")]
        MembershipStatus = 9,
        [Description("SM Type")]
        SmItemType = 10,
        [Description("Related Type")]
        RelatedType = 11

    }

    //TBD - update these...adjust to align with this data. 
    //TBD - consider approach where we don't hardcode these.
    //TODO: Update enum permissions to match what is coming from AAD
    public enum PermissionEnum
    {
        [Description("cesmii.marketplace.marketplaceadmin")]
        CanManageMarketplace = 1,

        [Description("cesmii.marketplace.user")]
        GeneralUser = 2,

        [Description("cesmii.marketplace.jobadmin")]
        CanManageJobDefinitions = 3,

        [Description("UserAzureADMapped")]
        UserAzureADMapped = 130

    }

    public enum TaskStatusEnum
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 10,
        Cancelled = 11
    }

    public enum JobActionTypeEnum
    {
        Standard = 0,
        Link = 1
    }
}
