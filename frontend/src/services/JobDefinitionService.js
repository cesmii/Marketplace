import { getUserPreferences, setUserPreferences } from '../utils/UtilityService';

//-------------------------------------------------------------------
// get/set commonly shared user preferences (ie page size)
//-------------------------------------------------------------------
export function getJobDefinitionPreferences() {
    var item = getUserPreferences();
    return item.jobDefinitionPreferences;
}

export function setJobDefinitionPreferencesPageSize(val) {
    var item = getUserPreferences();
    item.jobDefinitionPreferences.pageSize = val;
    setUserPreferences(item);
}
