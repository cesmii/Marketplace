import React, { useState, Fragment, useEffect } from 'react'
import { Form } from 'react-bootstrap'

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
    const [_useCredits, setUseCredits] = useState(false);

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
        setLoadingProps({ cart: cart});
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
                        onValidate={onValidate} onRemoveItem={onRemoveItem} showAbstract={true} className="col-12 mx-auto" />
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

    const renderUseCredits = (cart) => {

        if (cart?.items == null || cart?.items.length === 0 ||
            loadingProps.user?.credit == null || loadingProps.user.credit === 0) {
            return null;
        }

        return (
            <div className="text-center">
                <p>Your organization has <b>{loadingProps.user.credit} {loadingProps.user.credit === 1? 'credit' : 'credits' }</b> available to apply to this purchase.</p>
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
            {renderUseCredits(loadingProps.cart)}
        </>
    )
}

export default CartPreview;