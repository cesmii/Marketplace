import React, { useState, Fragment, useEffect } from 'react'
import { Form, Col } from 'react-bootstrap'

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { generateLogMessageString } from '../../utils/UtilityService';
import { removeCartItem, updateCart } from '../../utils/CartUtil';
import CartItem from './MarketplaceCartItem';

const CLASS_NAME = "CartPreview";

function CartPreview() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_isValid, setIsValid] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_usedCredit, setUsedCredit] = useState(0);

    useEffect(() => {
        if (loadingProps == null || loadingProps.user == null || loadingProps.user.usedcredit == null)
            return;

        setUsedCredit(loadingProps.user.usedcredit);

        return () => {
        };
    }, [loadingProps]);

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

    const onCreditChange = (e) => {
        switch (e.target.name) {
            case "usercredit":
                //update the state
                let cred = 0;
                if (e.target.value == '') {
                    cred = null;
                }
                else if (isNaN(parseInt(e.target.value))) {
                    cred = null;
                }
                else {
                    cred = parseInt(e.target.value);
                }

                if (loadingProps == null || loadingProps.user == null || loadingProps.user.usedcredit == null)
                    return;

                if (cred > loadingProps.user.credit)
                    return;

                setLoadingProps({ user: { credit: loadingProps.user.credit, usedcredit: cred, total: 0 } });
            default:
                return;
        }
    };

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_credit = (e) => {
        const result = parseInt(e.target.value);

        if (loadingProps == null || loadingProps.user == null || loadingProps.user.usedcredit == null)
            return;

        if (result > loadingProps.user.credit)
            return;

        if (loadingProps.onValidate != null) loadingProps.onValidate(loadingProps.user.usedcredit, result);
    };

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

    const renderCredits = (checkout) => {

        if (checkout.cart?.items == null || checkout.cart?.items.length === 0 ||
            checkout.user?.credit == null || checkout.user.credit === 0) {
            return null;
        }

        return (
            <div>
                <Form.Group className="mb-3 d-flex align-items-center">
                    <Form.Row className="d-flex align-items-center">
                        <Form.Label className="w-25" >Credits</Form.Label>
                        <Col className="w-25" >
                            <Form.Control name="usercredit" className="minimal px-2 text-right"
                                value={_usedCredit} onChange={onCreditChange} onBlur={validateForm_credit} />
                        </Col>
                        <Col >
                            <Form.Label htmlFor="credit" >out of {checkout.user.credit}</Form.Label>
                        </Col>
                    </Form.Row>
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
            {renderCredits(loadingProps)}
        </>
    )
}

export default CartPreview;