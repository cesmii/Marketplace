import React, { useState } from 'react'
import { Button } from 'react-bootstrap';

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { generateLogMessageString } from '../../utils/UtilityService';
import { removeCartItem, updateCart } from '../../utils/CartUtil';
import CartItem from './CartItem';

const CLASS_NAME = "CartPreview";

function CartPreview(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _caption = 'Shopping Cart';
    const [_isValid, setIsValid] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    const onCheckout = () => {
        console.log(generateLogMessageString('onCheckout', CLASS_NAME));
        if (props.onCheckout != null) props.onCheckout();
        //TBD - call API to start checkout
        //TBd - show a processing message and disable cart interactivity
    };

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
                <div className="col-sm-12 pt-4 border-top">
                    <Button variant="secondary" type="button" className="mx-3" onClick={onEmptyCart} >Empty Cart</Button>
                    <a className="mx-1 ml-auto" href="/library" >Continue Shopping</a>
                    <Button variant="primary" type="button" className="mx-3" onClick={onCheckout} disabled={_isValid} >Checkout</Button>
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