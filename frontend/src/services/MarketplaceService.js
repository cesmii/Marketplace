import { generateLogMessageString, getUserPreferences, setUserPreferences } from '../utils/UtilityService';

const CLASS_NAME = "MarketplaceService";

//-------------------------------------------------------------------
// getMarketplacePreferences, setMarketplacePageSize - get/set commonly shared user preferences for a profile (ie page size)
//-------------------------------------------------------------------
export function getMarketplacePreferences() {
    var item = getUserPreferences();
    return item.marketplacePreferences;
}

export function setMarketplacePageSize(val) {
    var item = getUserPreferences();
    item.marketplacePreferences.pageSize = val;
    setUserPreferences(item);
}

//-------------------------------------------------------------------
// Common search criteria helpers
//-------------------------------------------------------------------
//Clear out all search criteria values
export function clearSearchCriteria(criteria) {

    var result = JSON.parse(JSON.stringify(criteria));

    //loop over parents then over children and set selected to false
    result.filters.forEach(parent => {
        parent.items.forEach(item => {
            item.selected = false;
        });
    });

    //loop over item types and set selected to false
    result.itemTypes?.forEach(item => {
        item.selected = false;
    });

    result.query = null;
    result.skip = 0;
    return result;
}

//Find a filter item and set the selected value
export function toggleSearchFilterSelected(criteria, id) {

    //loop through filters and their items and find the id
    var item = null;
    //note it won't stop the foreach loop even if it finds it. Account for that.
    criteria.filters.forEach(parent => {
        if (item != null) return;
        item = parent.items.find(x => { return x.id.toString() === id; });
    });
    if (item == null) {
        console.warn(generateLogMessageString(`toggleSearchFilterValue||Could not find item with id: ${id} in lookup data`, CLASS_NAME));
        return;
    }
    //toggle the selection or set for initial scenario
    item.selected = !item.selected;
}