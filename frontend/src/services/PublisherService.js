import { getUserPreferences, setUserPreferences } from '../utils/UtilityService';

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
