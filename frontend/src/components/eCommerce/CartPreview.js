import React, { useState, Fragment, useEffect } from 'react'
import { Form } from 'react-bootstrap'
import axiosInstance from "../../services/AxiosService";

import { useLoginStatus } from '../../components/OnLoginHandler';
import { AppSettings } from '../../utils/appsettings';
import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { formatCurrency, generateLogMessageString } from '../../utils/UtilityService';
import { removeCartItem, updateCart } from '../../utils/CartUtil';
import CartItem from './CartItem';

const CLASS_NAME = "CartPreview";

function CartPreview() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_isValid, setIsValid] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_useCredits, setUseCredits] = useState(false);
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const { isAuthenticated } = useLoginStatus(null, [AppSettings.AADAdminRole]);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        setUseCredits(loadingProps?.cart == null ? false : loadingProps.cart.useCredits);

        return () => {
        };
    }, [loadingProps?.cart?.useCredits]);

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    const onValidate = (isValid) => {
        console.log(generateLogMessageString('onValidate', CLASS_NAME));
        setIsValid(isValid.required && isValid.numeric && isValid.range);
    };

    const onChangeChecked = (e) => {
        console.log(generateLogMessageString('onChangeChecked', CLASS_NAME));

        var cart = JSON.parse(JSON.stringify(loadingProps.cart));
        cart.useCredits = e.target.checked;
        // If User authenticated, save the cart items in the database
        if (isAuthenticated) {
            updateCartItem(cart);
        } else {
            setLoadingProps({ cart: cart });
        }
    };

    const updateCartItem = (cart) => {
        const url = `ecommerce/cart/update`;
        axiosInstance.post(url, cart)
            .then(resp => {
                if (resp.data.isSuccess) {
                    setLoadingProps({ cart: resp.data.data });
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
    };

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const onChange = (item, qty, price) => {
        console.log(generateLogMessageString(`onChange`, CLASS_NAME));
        //add the item to the cart and save context
        let cart = updateCart(loadingProps.cart, item, qty, price, true); //overwrite existing amount
        setLoadingProps({ cart: cart });
    };

    const onUpdateCart = () => {
        console.log(generateLogMessageString(`onUpdateCart`, CLASS_NAME));
        // If User authenticated, save the cart items in the database
        if (isAuthenticated) {
            updateCartItem({ cart: loadingProps.cart });
        }
    };

    const onRemoveItem = (id) => {
        console.log(generateLogMessageString('onRemoveItem', CLASS_NAME));
        //TBD - consider showing confirmation modal first.
        let cart = removeCartItem(loadingProps.cart, id);
        // If User authenticated, save the cart items in the database
        if (isAuthenticated) {
            updateCartItem(cart);
        } else {
            setLoadingProps({ cart: cart });
        }
    };


    //-------------------------------------------------------------------
    // Region: helper methods
    //-------------------------------------------------------------------
    const calculateSubTotal = (cart) => {
        return cart.items.reduce((total, itm) => itm.selectedPrice != null ? (total + itm.selectedPrice.amount * itm.quantity):0, 0)
    }
    const calculateCredits = (cart) => {
        if (_useCredits) {
            const subTotal = calculateSubTotal(cart);
            return loadingProps.user.credit >= subTotal ? subTotal : loadingProps.user.credit;
        }
        return null;
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderCartItems = (cart) => {

        if (cart == null || cart.items == null || cart.items.length === 0) {
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

        const mainBody = cart?.items.map((item, i) => {
            return (
                <Fragment key={i} >
                    <CartItem item={item} isAdd={false} onChange={onChange} showSelectedPriceOnly={true}
                        onValidate={onValidate} onUpdateCart={onUpdateCart} onRemoveItem={onRemoveItem} showAbstract={false} className="col-12 mx-auto" />
                    <hr />
                </Fragment>
            );
        });


        return (
            <div className="mb-2">
                {mainBody}
            </div>
        );
    }

    const renderTotals = (cart) => {

        if (cart == null || cart.items == null || cart.items.length === 0) return;

            const subTotal = calculateSubTotal(cart);
        const credits = calculateCredits(cart);
        const total = credits == null ? subTotal : subTotal - credits;

        return (
            <>
                {credits != null && 
                <>
                    <div className="row m-0 py-1 mt-1 border-bottom">
                        <div className="col-8">
                            Sub-Total
                        </div>
                        <div className="col-4 text-right">
                            <span className="pr-2" >{formatCurrency(subTotal)}</span>
                        </div>
                    </div>
                    <div className="row m-0 py-1 mt-1 border-bottom">
                        <div className="col-8">
                            Credits Applied
                        </div>
                        <div className="col-4 text-right">
                            <span className="pr-2" >{formatCurrency(credits)}</span>
                        </div>
                    </div>
                </>
                }
                <div className="row m-0 pt-1 mt-1 font-weight-bold">
                    <div className="col-8">
                        Total
                    </div>
                    <div className="col-4 text-right">
                        <span className="pr-2" >{formatCurrency(total)}</span>
                    </div>
                </div>
            </>
        );
    }

    const renderUseCredits = (cart) => {

        if (cart?.items == null || cart?.items.length === 0 ||
            loadingProps.user?.credit == null || loadingProps.user.credit === 0) {
            return null;
        }

        return (
            <div className="text-center border-top mt-3 pt-2">
                <p>Your organization has <b>{formatCurrency(loadingProps.user.credit)} {loadingProps.user.credit === 1? 'credit' : 'credits' }</b> available to apply to this purchase.</p>
                <Form.Group>
                    <Form.Check className="align-self-end" type="checkbox" id="useCredits" label="Use credits for this purchase?" 
                        checked={_useCredits} onChange={onChangeChecked} />
                </Form.Group>
            </div>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {renderCartItems(loadingProps.cart)}
            {renderTotals(loadingProps.cart)}
            {renderUseCredits(loadingProps.cart)}
        </>
    )
}

export default CartPreview;