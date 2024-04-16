import { getUserPreferences, setUserPreferences } from '../utils/UtilityService';
import { clearSearchCriteria, generateSearchQueryString, toggleSearchFilterSelected } from './MarketplaceService';

//-------------------------------------------------------------------
// get/set commonly shared user preferences (ie page size)
//-------------------------------------------------------------------
export function getPublisherPreferences() {
    var item = getUserPreferences();
    return item.publisherPreferences;
}

export function setPublisherPreferencesPageSize(val) {
    var item = getUserPreferences();
    item.publisherPreferences.pageSize = val;
    setUserPreferences(item);
}

export const getViewByPublisherUrl = (loadingProps, publisher) => {

    if (publisher == null || !publisher.allowFilterBy ) return null;

    //clear out the selected, the query val
    var criteria = clearSearchCriteria(loadingProps.searchCriteria);

    //loop through filters and their items and find the publisher id
    toggleSearchFilterSelected(criteria, publisher.id);

    //return url that will filter by publisher
    return `/library?${generateSearchQueryString(criteria, 1)}`;
};
