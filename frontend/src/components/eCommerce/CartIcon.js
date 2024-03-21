import React from 'react'

import { useLoadingContext } from '../contexts/LoadingContext';
import { getCartCount } from '../../utils/CartUtil';

import '../styles/Cart.scss'

//const CLASS_NAME = "Cart";

function Cart() { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //used in popup profile add/edit ui. Default to new version
    const { loadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <a className="btn btn-icon-outline primary circle" href='/cart'><i className="material-icons">shopping_cart</i>
                {(loadingProps.cart?.items != null && loadingProps.cart?.items.length > 0) &&
                    <div className="footnote" >
                    <span className="primary icon-circle small blazeOrange" >{getCartCount(loadingProps.cart)}</span>
                    </div>
                }
            </a>
        </>
    );
}

export default Cart;