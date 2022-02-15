import { getUserPreferences, setUserPreferences } from '../utils/UtilityService';

//-------------------------------------------------------------------
// get/set commonly shared user preferences (ie page size)
//-------------------------------------------------------------------
export function getLookupPreferences() {
    var item = getUserPreferences();
    return item.lookupPreferences;
}

export function setLookupPreferencesPageSize(val) {
    var item = getUserPreferences();
    item.lookupPreferences.pageSize = val;
    setUserPreferences(item);
}
