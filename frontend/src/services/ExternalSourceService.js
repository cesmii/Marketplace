import { getUserPreferences, setUserPreferences } from '../utils/UtilityService';

//-------------------------------------------------------------------
// get/set commonly shared user preferences (ie page size)
//-------------------------------------------------------------------
export function getExternalSourcePreferences() {
    var item = getUserPreferences();
    return item.externalSourcePreferences;
}

export function setExternalSourcePreferencesPageSize(val) {
    var item = getUserPreferences();
    item.externalSourcePreferences.pageSize = val;
    setUserPreferences(item);
}
