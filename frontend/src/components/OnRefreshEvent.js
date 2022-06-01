import { useEffect, useState } from 'react';

import { useLoadingContext } from "./contexts/LoadingContext";
import { generateLogMessageString } from '../utils/UtilityService';

const CLASS_NAME = "OnRefreshEvent";

// Component that handles scenario when f5 / refresh happens
// We want to turn off processing flag in that scenario as protection against
// scenario where exception occurs and isLoading remains true.
// renders nothing, just attaches side effects
export const OnRefreshEvent = () => {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { setLoadingProps } = useLoadingContext();
    const [_triggerRefresh, setTriggerRefresh] = useState(true);

    //-------------------------------------------------------------------
    // useEffect - if user clicks f5, then turn off is processing. 
    //-------------------------------------------------------------------
    useEffect(() => {
        if (performance.navigation.type === 1 && _triggerRefresh) {
            console.log(generateLogMessageString(`useEffect||navigation||f5||Page is reloaded`, CLASS_NAME));
            setTriggerRefresh(false);
            //turn off processing ui and then also re-get lookup, search criteria on f5/refresh
            setLoadingProps({ isLoading: false, refreshLookupData: true, refreshSearchCriteria: true });
        }
        //else {
        //    console.log(generateLogMessageString(`useEffect||navigation||Page not reloaded`, CLASS_NAME));
        //}
    }, [_triggerRefresh]);

    // renders nothing, since nothing is needed
    return null;
};