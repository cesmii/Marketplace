import { getUserPreferences, setUserPreferences } from '../utils/UtilityService';

//-------------------------------------------------------------------
// get/set commonly shared user preferences for a profile (ie page size)
//-------------------------------------------------------------------
export function getRequestInfoPreferences() {
    var item = getUserPreferences();
    return item.requestInfoPreferences;
}

export function setRequestInfoPreferencesPageSize(val) {
    var item = getUserPreferences();
    item.requestInfoPreferences.pageSize = val;
    setUserPreferences(item);
}
