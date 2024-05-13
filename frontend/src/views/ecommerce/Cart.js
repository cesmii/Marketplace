import React, { useState, useEffect } from 'react'
import { Button } from 'react-bootstrap';
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";
import { loadStripe } from '@stripe/stripe-js';
import {
    EmbeddedCheckoutProvider,
    EmbeddedCheckout
} from '@stripe/react-stripe-js';

import { AppSettings } from '../../utils/appsettings'
import { useLoginStatus } from '../../components/OnLoginHandler';
import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { generateLogMessageString } from '../../utils/UtilityService';
import CartPreview from '../../components/eCommerce/CartPreview';
import ConfirmationModal from '../../components/ConfirmationModal';
import color from '../../components/Constants';

const CLASS_NAME = "Cart";

function Cart() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _caption = 'Shopping Cart';
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const [_cartResponse, setCartResponse] = useState(null);
    const { isAuthenticated } = useLoginStatus(null, [AppSettings.AADAdminRole]);
    const [_stripePromise, setStripePromise] = useState(null); //loadStripe("pk_test_51Os66lHXjPkvmDZJ927KVzxAVIWaFhySoPDcoGVfxog1SXioudXZCbcaoMysdUrUBu1TgGEUGos0XkLpFyr0HB0Y00IxD721az"));

    //-------------------------------------------------------------------
    // Region: api call - fetch credit on load
    //-------------------------------------------------------------------
    ///*
    useEffect(() => {
        async function fetchCredits() {
            const url = `ecommerce/credits`;
            axiosInstance.get(url)
                .then(resp => {
                    if (resp.data.isSuccess) {
                        var cart = loadingProps.cart;

                        if (cart == null || cart.items == null) return;

                        cart.credits = resp.data.data;

                        setLoadingProps({ user: { credit: resp.data.data, usedcredit: resp.data.data, total: 0 } });
                    }
                    else {
                        //update spinner, messages
                        setError({ show: true, caption: 'Credits - Error', message: resp.data.message });
                    }
                })
                .catch(error => {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: `An error occurred during fetching credits.`, isTimed: false }
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

        if (!isAuthenticated || loadingProps.cart == null || loadingProps.cart?.items == null) return;

        fetchCredits();

        return () => {
        };
    }, [loadingProps.cart]);
    

    //-------------------------------------------------------------------
    // Region: on checkout init - prepare for embedding Stripe checkout form
    //-------------------------------------------------------------------
    useEffect(() => {

        if (_cartResponse == null) return;

        setLoadingProps({ checkout: { apiKey: _cartResponse.apiKey, sessionId: _cartResponse.sessionId } });

        //only load once
        if (_stripePromise == null) {
            setStripePromise(loadStripe(_cartResponse.apiKey));
        }

        return () => {
        };
    }, [_cartResponse, _stripePromise]);

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    const onCheckout = () => {
        console.log(generateLogMessageString('onCheckout', CLASS_NAME));

        //show a spinner
        let cart = loadingProps.cart;
        cart.status = AppSettings.CartStatusEnum.Pending;
        const host = window.location.protocol.concat("//").concat(window.location.host);
        cart.returnUrl = host.concat(`/checkout/complete`);

        setLoadingProps({ isLoading: true, message: "", cart: cart });

        if (loadingProps.user != null) { 
            cart.credits = loadingProps.user.usedcredit;
        }

        //perform do checkout call
        console.log(generateLogMessageString(`onCheckout`, CLASS_NAME));
        const url = `ecommerce/checkout/init`;
        axiosInstance.post(url, loadingProps.cart)
            .then(resp => {
                if (resp.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "success", body: `Checkout started...`, isTimed: true }
                        ]
                    });

                    //TBD - now redirect to checkout page and show the embedded Stripe form
                    setCartResponse(resp.data.data);
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Checkout - Error', message: resp.data.message });
                    setLoadingProps({ isLoading: false, message: null });
                }
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred during checkout.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('onCheckout||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });
    };


    const onEmptyCart = () => {
        console.log(generateLogMessageString('onEmptyCart', CLASS_NAME));
        //TBD - consider showing confirmation modal first.

        // If User authenticated, Then remove the cart items from database
        if (isAuthenticated) {
            const url = `ecommerce/cart/delete`;
            axiosInstance.post(url, { id: loadingProps.cart.id })
                .then(resp => {
                    if (resp.data.isSuccess) {
                        const cart = null;
                        setLoadingProps({ cart: cart });
                    }
                    else {
                        //update spinner, messages
                        setError({ show: true, caption: 'Cart - Error', message: resp.data.message });
                    }
                })
                .catch(error => {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: `An error occurred during adding item to cart credits.`, isTimed: false }
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
        } else {
            const cart = null;
            setLoadingProps({ cart: cart });
        }
    };

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    //render error message as a modal to force user to say ok.
    const renderErrorMessage = () => {

        if (!_error.show) return;

        return (
            <ConfirmationModal showModal={_error.show} caption={_error.caption} message={_error.message}
                icon={{ name: "warning", color: color.trinidad }}
                confirm={null}
                cancel={{
                    caption: "OK",
                    callback: () => {
                        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
                        setError({ show: false, caption: null, message: null });
                    },
                    buttonVariant: 'danger'
                }} />
        );
    };

    const renderCheckout = () => {
        if (_cartResponse == null) return;

        if (_cartResponse.apiKey == null || _cartResponse.clientSecret == null) return;

        const options = {
            clientSecret: _cartResponse.clientSecret,
        };

        return (
            <div id="checkout">
                <EmbeddedCheckoutProvider
                    stripe={_stripePromise}
                    options={options}>
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
            {(_cartResponse == null) &&
                <>
                <div className="row" >
                    <div className="col-sm-6 mt-4 mx-auto text-center">
                        <h1 className="m-0 headline-2">
                            {_caption}
                        </h1>
                    </div>
                </div>
                <div className="row my-3 mx-2 mx-lg-0" >
                    <div className="col-12 col-md-10 col-lg-6 my-2 p-3 p-md-4 mx-sm-auto bg-white rounded border">
                        <CartPreview />
                    </div>
                </div>
                </>
            }
            {(_cartResponse == null && loadingProps.cart != null && loadingProps.cart.items != null && loadingProps.cart.items.length > 0) &&
                <>
                <div className="row" >
                    <div className="col-7 mx-auto pt-2 text-center">
                        <Button variant="secondary" type="button" className="mx-3 mb-3" onClick={onEmptyCart} >Empty Cart</Button>
                        <Button variant="primary" type="button" className="mx-3 mb-3 " onClick={onCheckout} >Checkout</Button>
                    </div>
                </div>
                <div className="row" >
                    <div className="col-7 mx-auto pt-1 text-center">
                        <a className="mx-3" href="/library" >Continue Shopping</a>
                    </div>
                </div>
                </>
            }
            {renderCheckout()}
            {renderErrorMessage()}
        </>
    )
}

export default Cart;