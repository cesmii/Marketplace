import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import ReactGA from 'react-ga4';
import { generateLogMessageString } from '../utils/UtilityService';
import { AppSettings } from '../utils/appsettings';

const CLASS_NAME = "onRouteChange";

// on router change, log a tracking record to Google analytics
// renders nothing, just attaches side effects
export const OnRouteChange = () => {
    // this assumes that current router state is accessed via hook
    // but it does not matter, pathname and search (or that ever) may come from props, context, etc.
    const location = useLocation();

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - capture page view - just run the effect on pathname and/or search change
    //-------------------------------------------------------------------
    useEffect(() => {

        if (AppSettings.TrackAnalytics === "true") {
            //log the page view
            //var url = window.location.pathname + window.location.search;
            var url = location.pathname + location.search;
            console.log(generateLogMessageString(`Analytics||PageView||${url}`, CLASS_NAME));
            ReactGA.pageview(url);

            // This would be how you check that the calls are made correctly
            //console.log(ReactGA.testModeAPI.calls);
            //expect(ReactGA.testModeAPI.calls).toEqual([
            //    ['send', 'pageview', url]
            //]);
        }

    }, [location.pathname]);

    // renders nothing, since nothing is needed
    return null;
};