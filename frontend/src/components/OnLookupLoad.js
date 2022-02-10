import { useEffect } from 'react';

import axiosInstance from '../services/AxiosService'
import { useLoadingContext } from "./contexts/LoadingContext";
import { generateLogMessageString } from '../utils/UtilityService';
import { getMarketplacePreferences } from '../services/MarketplaceService';

const CLASS_NAME = "OnLookupLoad";

// Component that handles scenario when f5 / refresh happens
// We want to turn off processing flag in that scenario as protection against
// scenario where exception occurs and isLoading remains true.
// renders nothing, just attaches side effects
export const OnLookupLoad = () => {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - get static lookup data
    //-------------------------------------------------------------------
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchLookupData() {

            var url = `lookup/all`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.get(url).then(result => {
                if (result.status === 200) {
                    //set the data in local storage
                    setLoadingProps({
                        lookupDataStatic: result.data,
                        refreshLookupData: false
                    });
                } else {
                    setLoadingProps({
                        lookupDataStatic: null,
                        refreshLookupData: false
                    });
                }
            }).catch(e => {
                if (e.response && e.response.status === 401) {
                }
                else {
                    console.log(generateLogMessageString('useEffect||fetchLookupData||' + JSON.stringify(e), CLASS_NAME, 'error'));
                    console.log(e);
                }
            });
        };

        if (loadingProps.lookupDataStatic == null || loadingProps.refreshLookupData === true) {
            //console.log(generateLogMessageString('useEffect||refreshLookupData||Trigger fetch', CLASS_NAME));
            fetchLookupData();
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||refreshLookupData||Cleanup', CLASS_NAME));
        };
    }, [loadingProps.lookupDataStatic, loadingProps.refreshLookupData]);

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - load & cache search criteria under certain conditions
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {

            var url = `lookup/searchcriteria`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.get(url).then(result => {
                if (result.status === 200) {

                    //init the page size value
                    result.data.take = getMarketplacePreferences().pageSize;

                    //set the data in local storage
                    setLoadingProps({
                        searchCriteria: result.data,
                        refreshSearchCriteria: false,
                        searchCriteriaRefreshed: loadingProps.searchCriteriaRefreshed + 1
                    });

                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace filters.', isTimed: true }]
                    });
                }

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace filters.', isTimed: true }]
                    });
                }
            });
        }

        //trigger retrieval of lookup data - if necessary
        if (loadingProps == null || loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null
            || loadingProps.refreshSearchCriteria) {
            //console.log(generateLogMessageString('useEffect||refreshSearchCriteria||Trigger fetch', CLASS_NAME));
            fetchData();
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||refreshSearchCriteria||Cleanup', CLASS_NAME));
        };

    }, [loadingProps.searchCriteria, loadingProps.refreshSearchCriteria]);


    ////-------------------------------------------------------------------
    //// Region: hooks
    //// useEffect - load & cache favorites list
    ////-------------------------------------------------------------------
    //useEffect(() => {
    //    // Load lookup data upon certain triggers in the background
    //    async function fetchData() {

    //        var url = `marketplace/lookup/favorites`;
    //        console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

    //        await axiosInstance.get(url).then(result => {
    //            if (result.status === 200) {
    //                //set the data in local storage
    //                setLoadingProps({
    //                    favoritesList: result.data,
    //                    refreshFavoritesList: false
    //                });
    //            } else {
    //                setLoadingProps({
    //                    favoritesList: null,
    //                    refreshFavoritesList: false
    //                });
    //            }
    //        }).catch(e => {
    //            if (e.response && e.response.status === 401) {
    //            }
    //            else {
    //                console.log(generateLogMessageString('useEffect||fetchFavorites||' + JSON.stringify(e), CLASS_NAME, 'error'));
    //                console.log(e);
    //            }
    //        });
    //    };

    //    if (loadingProps.favoritesList == null || loadingProps.refreshFavoritesList === true) {
    //        //console.log(generateLogMessageString('useEffect||refreshFavoritesList||Trigger fetch', CLASS_NAME));
    //        fetchData();
    //    }

    //    //this will execute on unmount
    //    return () => {
    //        //console.log(generateLogMessageString('useEffect||refreshLookupData||Cleanup', CLASS_NAME));
    //    };
    //}, [loadingProps.favoritesList, loadingProps.refreshFavoritesList]);

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    // renders nothing, since nothing is needed
    return null;
};