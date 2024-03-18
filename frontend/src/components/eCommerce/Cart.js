import React, { useState } from 'react'
import { Button } from 'react-bootstrap';

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { generateLogMessageString } from '../../utils/UtilityService';
import CartPreview from '../../components/eCommerce/CartPreview';

const CLASS_NAME = "Cart";

function Cart(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //used in popup profile add/edit ui. Default to new version
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_slideOutShow, setShow] = useState(false);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onSlideOut = (e) => {
        console.log(generateLogMessageString('slideOut', CLASS_NAME));
        setShow(true);
    }

    const onCheckout = () => {
        //initiate checkout, call API to perform a checkout.
        setShow(false);
    }

    const onEmptyCart = () => {
        const cart = null;
        setLoadingProps({ cart: cart });
        setShow(false);
    }

    const onClose = () => {
        console.log(generateLogMessageString(`onClose`, CLASS_NAME));
        setShow(false);
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (loadingProps.cart?.items == null) return null;

    return (
        <>
            <Button variant="icon-outline" className="primary circle" onClick={onSlideOut}><i className="material-icons">shopping_cart</i></Button>

            {_slideOutShow &&
                <CartPreview cart={loadingProps.cart} onCheckout={onCheckout} onEmptyCart={onEmptyCart} onClose={onClose} />
            }
        </>
    );
}

export default Cart;