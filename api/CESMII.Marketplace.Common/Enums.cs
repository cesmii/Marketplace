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
    }

    //TBD - update these...adjust to align with this data. 
    //TBD - consider approach where we don't hardcode these.
    //TODO: Update enum permissions to match what is coming from AAD
    public enum PermissionEnum
    {
        [Description("CanManageMarketplace")]
        CanManageMarketplace = 1,

        [Description("CanManageUsers")]
        CanManageUsers = 2,

        [Description("CanManageSystemSettings")]
        CanManageSystemSettings = 3,

        [Description("CanManagePublishers")]
        CanManagePublishers = 4,

        [Description("CanManageRequestInfo")]
        CanManageRequestInfo = 5,

        [Description("CanManageJobDefinitions")]
        CanManageJobDefinitions = 6

    }

    public enum TaskStatusEnum
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 10,
        Cancelled = 11
    }


}
