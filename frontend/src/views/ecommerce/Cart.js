import React, { useState, useEffect } from 'react'
import { useHistory } from 'react-router-dom';
import { Button } from 'react-bootstrap';
import { Helmet } from "react-helmet"

import axiosInstance from "../../services/AxiosService";
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
    const history = useHistory();
    const _caption = 'Shopping Cart';
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const { isAuthenticated } = useLoginStatus(null, [AppSettings.AADAdminRole]);

    //-------------------------------------------------------------------
    // Region: hooks - checkout status - if user leaves and comes back to cart page, return to the 
    //  checkout screen and do not make them start over if we are in progress on checkout.
    //-------------------------------------------------------------------
    useEffect(() => {
        console.log(generateLogMessageString(`useEffect-cart-fetchStatus`, CLASS_NAME));

        async function fetchStatus() {
            setLoadingProps({ isLoading: true, message: "" });

            const url = `ecommerce/checkout/status`;
            axiosInstance.post(url, { id: loadingProps.checkout.sessionId })
                .then(resp => {

                    if (resp.data.isSuccess) {

                        if (resp.data.data != null) {
                            console.log(generateLogMessageString(`fetchStatus|checkout status|${resp.data.data.status}`, CLASS_NAME));
                            switch (resp.data.data.status) {
                                case "complete":
                                    setLoadingProps({isLoading: false, cart: null, checkout: null});
                                    return;
                                case "open":
                                    console.log(generateLogMessageString(`fetchStatus||redirect to checkout`, CLASS_NAME));
                                    setLoadingProps({ isLoading: false });
                                    history.push('/checkout');
                                    return;
                                case "expired":
                                    console.warn(generateLogMessageString(`fetchStatus|checkout status|${resp.data.data.status}`, CLASS_NAME));
                                    setLoadingProps({ isLoading: false, checkout: null });
                                    return;
                                default:
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

        if (loadingProps.checkout?.sessionId != null) {
            fetchStatus();
        }

        return () => {
        };

    }, [loadingProps.checkout?.sessionId]);

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
                    //hide a spinner, set checkout data to trigger load of embedded stripe form
                    setLoadingProps({ isLoading: false, checkout: resp.data.data });
                    history.push('/checkout');
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


    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${_caption} | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            {(loadingProps.checkout == null) &&
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
            {(loadingProps.checkout == null && loadingProps.cart?.items != null && loadingProps.cart.items.length > 0) &&
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
            {renderErrorMessage()}
        </>
    )
}

export default Cart;