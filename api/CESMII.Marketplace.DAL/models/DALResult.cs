namespace CESMII.Marketplace.DAL.Models
{
    using System.Collections.Generic;

    public class DALResult<T>
    {
        // Record count
        public long Count { get; set; }

        // The actual data as a list of type <T>
        public List<T> Data { get; set; }

        // A list of the summary of the data, but could be average etc. hence list.
        public List<T> SummaryData { get; set; }

        public override string ToString() => $"{Data?.Count} of {Count}";
    }
}