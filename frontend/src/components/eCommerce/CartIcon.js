import React, { useEffect } from 'react'
import axiosInstance from "../../services/AxiosService";

import { useLoadingContext } from '../contexts/LoadingContext';
import { getCartCount } from '../../utils/CartUtil';
import { useLoginStatus } from '../../components/OnLoginHandler';
import { AppSettings } from '../../utils/appsettings'

import '../styles/Cart.scss'

//const CLASS_NAME = "Cart";

function Cart() { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //used in popup profile add/edit ui. Default to new version
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { isAuthenticated } = useLoginStatus([AppSettings.AADUserRole]);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchCartItems() {
            const url = `ecommerce/cart`;
            axiosInstance.get(url)
                .then(resp => {
                    setLoadingProps({ cart: resp.data.data });
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

        // If user authenticated, Fetch the cart items from database
        if (isAuthenticated) {
            fetchCartItems();
        }

    }, [isAuthenticated]);
    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    const _cartCount = getCartCount(loadingProps.cart);
    const _cssLarge = _cartCount > 10 ? 'large' : '';

    return (
        <>
            <a className="btn btn-icon-outline primary circle" href='/cart'><i className="material-icons">shopping_cart</i>
                {(loadingProps.cart?.items != null && loadingProps.cart?.items.length > 0) &&
                    <div className={`footnote ${_cssLarge}`} >
                    <span>{_cartCount}</span>
                    </div>
                }
            </a>
        </>
    );
}

export default Cart;