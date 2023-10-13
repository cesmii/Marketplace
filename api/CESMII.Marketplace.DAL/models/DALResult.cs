namespace CESMII.Marketplace.DAL.Models
{
    using System.Collections.Generic;

    public class DALResult<T>
    {
        // Record count
        public long Count { get; set; }

        public string StartCursor { get; set; }
        public string EndCursor { get; set; }

        // The actual data as a list of type <T>
        public List<T> Data { get; set; }

        // A list of the summary of the data, but could be average etc. hence list.
        public List<T> SummaryData { get; set; }

        public override string ToString() => $"{Data?.Count} of {Count}";
    }

    public class DALResultWithSource<T>: DALResult<T>
    {
        /// <summary>
        /// Identifier indicating the source of this result set
        /// </summary>
        public string SourceId { get; set; }
        /// <summary>
        /// Search cursor used in the search call
        /// </summary>
        public SearchCursor Cursor { get; set; }

    }

    public class DALResultWithCursors<T> : DALResult<T>
    {
        /// <summary>
        /// List of cursors for each source
        /// </summary>
        public List<SourceSearchCursor> CachedCursors { get; set; }
    }


    public class SourceSearchCursor 
    {
        public string SourceId { get; set; }

        /// <summary>
        /// A list of search cursors for this source. This allows 
        /// for re-using cached cursors if the user navigates between 
        /// paged data but leaves the search criteria the same
        /// </summary>
        public List<SearchCursor> Cursors { get; set; }
    }

    public class SearchCursor
    {
        public string StartCursor { get; set; }
        public string EndCursor { get; set; }

        public int Skip { get; set; } = 0;
        public int? Take { get; set; }

        /// <summary>
        /// 1 based page index
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// TotalCount
        /// </summary>
        public int? TotalCount { get; set; }

        /// <summary>
        /// TotalCount
        /// </summary>
        public bool HasTotalCount { get { return TotalCount.HasValue; } }
    }

}