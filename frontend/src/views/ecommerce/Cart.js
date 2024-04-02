import React, { useState, useEffect } from 'react'
import { Button } from 'react-bootstrap';
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";
import { loadStripe } from '@stripe/stripe-js';

import { AppSettings } from '../../utils/appsettings'
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

    //-------------------------------------------------------------------
    // Region: api calls
    //-------------------------------------------------------------------
    useEffect(() => {

        if (_cartResponse == null) return;

        //const stripePromise = await loadStripe("pk_test_51Os66lHXjPkvmDZJ927KVzxAVIWaFhySoPDcoGVfxog1SXioudXZCbcaoMysdUrUBu1TgGEUGos0XkLpFyr0HB0Y00IxD721az");
        //const session = await response.json();
        //const stripePromise = await loadStripe(_cartResponse.apiKey);
        //stripePromise.redirectToCheckout({ sessionId: _cartResponse.sessionId });

        loadStripe(_cartResponse.apiKey).then(resp => {
            resp.redirectToCheckout({ sessionId: _cartResponse.sessionId });
        });

        //this will execute on unmount
        return () => {
        };
    }, [_cartResponse]);



    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    const onCheckout = () => {
        console.log(generateLogMessageString('onCheckout', CLASS_NAME));

        //TBD - call API to start checkout
        //TBd - show a processing message and disable cart interactivity

        //do validation
        //TBD
        /*
        const isValid = validateCart(loadingProps.cart);
        if (!isValid.quantity || !isValid.allowPurchase) {
            //TBD - show a nice message
            //hide a spinner, show a message
            setLoadingProps({
                isLoading: false, message: null, inlineMessages: [
                    { id: new Date().getTime(), severity: "success", body: `Checkout started...`, isTimed: true }
                ]
            });

            alert("validation failed");
            return;
        }
        */

        //show a spinner
        let cart = loadingProps.cart;
        cart.status = AppSettings.CartStatusEnum.Pending;
        const host = window.location.protocol.concat("//").concat(window.location.host);
        //TBD - refine these once we further understand how to handle each scenario.
        cart.returnUrl = host.concat(`/checkout`);
        //cart.returnUrl = host.concat(`/checkout/success`);
        //cart.successUrl = host.concat(`/checkout/success`);
        //cart.cancelUrl = host.concat(`/checkout/cancel`);
        setLoadingProps({ isLoading: true, message: "", cart: cart });

        //perform do checkout call
        console.log(generateLogMessageString(`onCheckout`, CLASS_NAME));
        var url = `ecommerce/checkout/init`;
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
                    //setLoadingProps({ checkout: { init: resp.data.data } });

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
        const cart = null;
        setLoadingProps({ cart: cart });
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
            <>
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
            </>
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
            <div className="row" >
                <div className="col-sm-12 mb-2">
                    <h1 className="m-0 headline-2">
                        {_caption}
                    </h1>
                </div>
            </div>
            <CartPreview />
            <div className="row" >
                <div className="col-sm-12 pt-4 border-top">
                    <Button variant="secondary" type="button" className="mx-3" onClick={onEmptyCart} >Empty Cart</Button>
                    <a className="mx-1 ml-auto" href="/library" >Continue Shopping</a>
                    <Button variant="primary" type="button" className="mx-3" onClick={onCheckout} >Checkout</Button>
                </div>
            </div>
            {renderErrorMessage()}
        </>
    )
}

export default Cart;