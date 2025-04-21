import React, { useEffect, useState } from 'react'
import { Helmet } from "react-helmet"
import { useHistory } from 'react-router-dom';

import { loadStripe } from '@stripe/stripe-js';
import {
    EmbeddedCheckoutProvider,
    EmbeddedCheckout
} from '@stripe/react-stripe-js';

import axiosInstance from "../../services/AxiosService";
import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService';

const CLASS_NAME = "Checkout";

function Checkout() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _caption = 'Checkout';
    const history = useHistory();
    const [_status, setStatus] = useState(null);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_stripePromise, setStripePromise] = useState(null);
    const [_checkStatus, setCheckStatus] = useState(true);

    //-------------------------------------------------------------------
    // Region: on checkout init - prepare for embedding Stripe checkout form
    //-------------------------------------------------------------------
    useEffect(() => {

        //only load once
        if (_stripePromise != null) return;

        if (loadingProps?.checkout?.apiKey == null) return;

        setStripePromise(loadStripe(loadingProps.checkout.apiKey));

        return () => {
        };
    }, [loadingProps.checkout?.apiKey, _stripePromise]);

    //-------------------------------------------------------------------
    // Region: hooks - checkout status - if user leaves and comes back to cart page, return to the 
    //  checkout screen and do not make them start over if we are in progress on checkout.
    //-------------------------------------------------------------------
    useEffect(() => {
        console.log(generateLogMessageString(`useEffect-checkout-fetchStatus`, CLASS_NAME));

        async function fetchStatus() {
            const url = `ecommerce/checkout/status`;
            axiosInstance.post(url, { id: loadingProps.checkout.sessionId })
                .then(resp => {

                    if (resp.data.isSuccess) {

                        if (resp.data.data == null) {
                            setStatus(AppSettings.CartStatusEnum.None);
                        }
                        else {
                            switch (resp.data.data.status) {
                                case "complete":
                                    history.push(`/checkout/complete/${loadingProps.checkout.sessionId}`);
                                    //setStatus(AppSettings.CartStatusEnum.Completed);
                                    //setLoadingProps({ isLoading: false, cart: null, checkout: null });
                                    return;
                                case "open":
                                    setStatus(AppSettings.CartStatusEnum.Pending);
                                    return;
                                case "expired":
                                    console.warn(generateLogMessageString(`fetchStatus|checkout status|${resp.data.data.status}`, CLASS_NAME));
                                    setLoadingProps({ isLoading: false, checkout: null });
                                    return;
                                default:
                                    setStatus(AppSettings.CartStatusEnum.None);
                                    break;
                            }
                        }
                    }
                    else {
                        setLoadingProps({
                            isLoading: false, message: null, inlineMessages: [
                                { id: new Date().getTime(), severity: "danger", body: resp.data.message, isTimed: true }
                            ]
                        });
                    }
                })
                .catch(error => {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: `An error occurred checking status.`, isTimed: false }
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

        //fetch checkout completion status
        if (loadingProps.checkout?.sessionId != null && _checkStatus) {
        //if (loadingProps.checkout != null) {
            fetchStatus();
            setCheckStatus(false);
        }

        return () => {
        };

    }, [loadingProps.checkout?.sessionId, _checkStatus]);

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderCheckout = () => {
        if (loadingProps.checkout?.apiKey == null || loadingProps.checkout?.clientSecret == null) {
            return (
                <>
                    <div className="row" >
                        <div className="col-sm-6 mt-4 mx-auto text-center">
                            <h1 className="m-0 headline-2">
                                Checkout
                            </h1>
                        </div>
                    </div>
                    <div className="row" >
                        <div className="col-12 col-md-10 col-lg-6 my-2 p-3 p-md-4 mx-sm-auto bg-white rounded border">
                            <div className="col-sm-12 my-5 mx-auto text-center" >
                                <span className="icon-circle primary mx-auto" ><i className="material-icons">shopping_cart</i></span>
                                <div className="text-center mx-auto" >
                                    <div className="py-4" >
                                        There is no checkout in progress.
                                    </div>
                                    <a className="btn btn-primary" href='/cart'>View Your Cart</a>
                                </div>
                            </div>
                        </div>
                    </div>
                </>
            );
        }

        if (loadingProps.cart?.items == null || loadingProps.cart?.items.length == 0) {
            return (
                <div className="col-sm-12 my-5 mx-auto text-center">
                    <span className="icon-circle primary mx-auto" ><i className="material-icons">shopping_cart</i></span>
                    <div className="d-block py-4" >
                        Your cart is empty.
                    </div>
                    <a className="btn btn-primary" href='/library'>Shop Now</a>
                </div>
            );
        }

        //else
        const options = {
            clientSecret: loadingProps.checkout.clientSecret,
        };

        return (
            <div id="checkout">
                <EmbeddedCheckoutProvider
                    stripe={_stripePromise}
                    options={options} >
                    <EmbeddedCheckout />
                </EmbeddedCheckoutProvider>
            </div>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${_caption} | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            {renderCheckout()}
        </>
    )
}

export default Checkout;