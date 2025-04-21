import { useEffect } from 'react';

import axiosInstance from '../services/AxiosService'
import { AppSettings } from '../utils/appsettings';
import { generateLogMessageString } from '../utils/UtilityService';
import { useLoadingContext } from "./contexts/LoadingContext";
import { useLoginStatus } from './OnLoginHandler';

const CLASS_NAME = "OnECommerceLoad";

// Component that handles init for eCommerce stuff. Check if eCommerce is enabled. 
export const OnECommerceLoad = () => {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { isAuthenticated } = useLoginStatus([AppSettings.AADUserRole]);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchCartItems() {
            //timestamp to ensure not calling cached api call
            const url = `ecommerce/cart?timestamp=${new Date().getTime()}`;
            axiosInstance.get(url)
                .then(resp => {
                    setLoadingProps({ cart: resp.data.data });
                })
                .catch(error => {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: `An error occurred during fetching the cart.`, isTimed: true }
                        ]
                    });
                    console.log(error);
                    //scroll back to top
                    window.scroll({
                        top: 0,
                        left: 0,
                        behavior: 'smooth',
                    });
                });
        }

        // If user authenticated, Fetch the cart items from database
        if (isAuthenticated && loadingProps.eCommerceEnabled) {
            fetchCartItems();
        }

    }, [isAuthenticated, loadingProps.eCommerceEnabled]);

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - get eCommerce enabled flag
    //-------------------------------------------------------------------
    useEffect(() => {
        async function checkECommerceEnabled() {
            console.log(generateLogMessageString(`checkECommerceEnabled`, CLASS_NAME));
            //timestamp to ensure not calling cached api call
            const url = `ecommerce/enabled`;
            axiosInstance.get(url)
                .then(resp => {
                    if (!resp.data)
                        setLoadingProps({ cart: null, eCommerceEnabled: resp.data, refreshECommerceInit: false });
                    else
                        setLoadingProps({ eCommerceEnabled: resp.data, refreshECommerceInit: false });
                })
                .catch(error => {
                    setLoadingProps({ refreshECommerceInit: false });
                    console.error(error);
                });
        }

        if (loadingProps.eCommerceEnabled == null || loadingProps.refreshECommerceInit === true ||
            window.location.pathname === '/') {
            checkECommerceEnabled();
        }

    }, [loadingProps.eCommerceEnabled, loadingProps.refreshECommerceInit, window.location.href]);

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    // renders nothing, since nothing is needed
    return null;
};


