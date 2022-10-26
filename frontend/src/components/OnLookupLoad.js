import { useEffect } from 'react';
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

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
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const _isAuthenticated = useIsAuthenticated() && _activeAccount != null;

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - get static lookup data
    //-------------------------------------------------------------------
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchData() {

            const url = `lookup/all`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.get(url).then(result => {
                if (result.status === 200) {
                    //set the data in local storage
                    setLoadingProps({
                        lookupDataStatic: result.data,
                        refreshLookupData: false,
                        lookupDataRefreshed: loadingProps.lookupDataRefreshed + 1
                    });
                } else {
                    setLoadingProps({
                        lookupDataStatic: null,
                        refreshLookupData: false,
                        lookupDataRefreshed: loadingProps.lookupDataRefreshed + 1
                    });
                }
            }).catch(e => {
                setLoadingProps({ refreshLookupData: false });
                if (e.response && e.response.status === 401) {
                }
                else {
                    console.log(generateLogMessageString('useEffect||fetchLookupData||' + JSON.stringify(e), CLASS_NAME, 'error'));
                    console.log(e);
                }
            });
        }

        if (loadingProps.lookupDataStatic == null || loadingProps.refreshLookupData === true) {
            fetchData();
        }

    }, [loadingProps.lookupDataStatic, loadingProps.refreshLookupData]);

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - load & cache search criteria under certain conditions
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {

            const url = `lookup/searchcriteria`;
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
                setLoadingProps({ refreshSearchCriteria: false });
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
            fetchData();
        }

    }, [loadingProps.searchCriteria, loadingProps.refreshSearchCriteria]);

    /*
    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - load & cache favorites list
    //-------------------------------------------------------------------
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchData() {

            const url = `marketplace/lookup/favorites`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.get(url).then(result => {
                if (result.status === 200) {
                    //set the data in local storage
                    setLoadingProps({
                        favoritesList: result.data,
                        refreshFavoritesList: false
                    });
                } else {
                    setLoadingProps({
                        favoritesList: null,
                        refreshFavoritesList: false
                    });
                }
            }).catch(e => {
                setLoadingProps({refreshFavoritesList: false});
                if (e.response && e.response.status === 401) {
                }
                else {
                    console.log(generateLogMessageString('useEffect||fetchFavorites||' + JSON.stringify(e), CLASS_NAME, 'error'));
                    console.log(e);
                }
            });
        }

        //if not logged in yet, return
        if (!_isAuthenticated || !loadingProps.refreshFavoritesList) return;

        if (loadingProps.favoritesList == null || loadingProps.refreshFavoritesList === true) {
            fetchData();
        }

    }, [loadingProps.favoritesList, loadingProps.refreshFavoritesList, _isAuthenticated]);
    */

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    // renders nothing, since nothing is needed
    return null;
};