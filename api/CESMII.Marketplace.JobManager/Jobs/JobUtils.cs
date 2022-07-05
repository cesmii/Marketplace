namespace CESMII.Marketplace.JobManager.Jobs
{
    /// <summary>
    /// TBD - Use the status table in the DB with the proper ids
    /// </summary>
    public enum JobBatchStatusEnum
    {
        NotStarted = 1,
        InProgress = 2,
        Complete = 3,
        Retry = 4,
        Fail = 5,
        CompleteWithErrors = 6
    }

    public enum JobQueueEnum
    {
        ToAmazon,
        FromAmazon,
        AdCipher,
        WebJobProto,
        Default
    }

    /// <summary>
    /// TBD - move this to a table in data project and pull from DB.
    /// </summary>
    public enum CampaignTypeEnum
    {
        Auto = 1,
        Harvest = 2,
        PCTarget = 3,
        Convert = 4
    }

}
