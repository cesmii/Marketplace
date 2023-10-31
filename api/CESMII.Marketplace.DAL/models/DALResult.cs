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

    /// <summary>
    /// This is returned by the external source search and marketplace item search.
    /// This will return the data but also the cursor used in making the search.
    /// </summary>
    /// <typeparam name="T"></typeparam>
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

    /// <summary>
    /// This is the object returned to client on search. 
    /// The cached cursors will be returned back to the server on 
    /// a subsequent search call. If the search changes by either 
    /// different filters, different query value, different sm types, different page size, then the 
    /// cached cursor is reset. It only lasts as long as the only changes
    /// are changes to the page index. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DALResultWithCursors<T> : DALResult<T>
    {
        /// <summary>
        /// List of cursors for each source
        /// </summary>
        public List<SourceSearchCursor> CachedCursors { get; set; }
    }


    /// <summary>
    /// This identifies the search source (ie marketplace item is null, Cloud lib or Bennit)
    /// and the 
    /// </summary>
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