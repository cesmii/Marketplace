import React, { useState } from 'react'

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { generateLogMessageString } from '../../utils/UtilityService';
import { removeCartItem, updateCart } from '../../utils/CartUtil';
import CartItem from './CartItem';

const CLASS_NAME = "CartPreview";

function CartPreview() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_isValid, setIsValid] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    /*
    const onCheckout = async () => {
        console.log(generateLogMessageString('onCheckout', CLASS_NAME));
        if (props.onCheckout != null) props.onCheckout();

           const response = await fetch("https://localhost:44373/api/ecommerce/checkout/init", {
                method: 'POST',
                headers: {
                    'Accept': 'application/json, text/plain',
                    'Content-Type': 'application/json;charset=UTF-8'
                },
                body: JSON.stringify(loadingProps.cart)
           });

        const stripePromise = await loadStripe("pk_test_51Os66lHXjPkvmDZJ927KVzxAVIWaFhySoPDcoGVfxog1SXioudXZCbcaoMysdUrUBu1TgGEUGos0XkLpFyr0HB0Y00IxD721az");
        const session = await response.json();
        stripePromise.redirectToCheckout({sessionId:session});

        //TBD - call API to start checkout
        //TBd - show a processing message and disable cart interactivity
    };

    */

    const onValidate = (isValid) => {
        console.log(generateLogMessageString('onValidate', CLASS_NAME));
        setIsValid(isValid.required && isValid.numeric && isValid.range);
    };

    const onChange = (item, qty) => {
        console.log(generateLogMessageString(`onChange`, CLASS_NAME));
        //add the item to the cart and save context
        let cart = updateCart(loadingProps.cart, item, qty);
        setLoadingProps({ cart: cart });
    };

    const onRemoveItem = (id) => {
        console.log(generateLogMessageString('onRemoveItem', CLASS_NAME));
        //TBD - consider showing confirmation modal first.
        let cart = removeCartItem(loadingProps.cart, id);
        setLoadingProps({ cart: cart });
    };


    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderCartItems = (cart) => {

        if (cart == null || cart.items == null || cart.items.length === 1) {
            return (
                <div className="row" >
                    <div className="col-sm-4 my-5 mx-auto text-center">
                        <span className="icon-circle primary mx-auto" ><i className="material-icons">shopping_cart</i></span>
                        <div className="d-block py-4" >
                            Your cart is empty.
                        </div>
                        <a className="btn btn-primary" href='/library'>Shop Now</a>
                    </div>
                </div>
            );
        }

        const mainBody = cart?.items.map((item, i) => {
            return (
                <CartItem key={i} item={item} quantity={item.quantity} isAdd={false} onChange={onChange} onChange={onValidate} onRemoveItem={onRemoveItem} />
            );
        });


        return (
            <div className="row" >
                <div className="col-sm-12 mb-4">
                    {mainBody}
                </div>
            </div>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {renderCartItems(loadingProps.cart)}
        </>
    )
}

export default CartPreview;