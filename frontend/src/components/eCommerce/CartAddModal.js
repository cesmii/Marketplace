import React, { useEffect, useState } from 'react'
import Modal from 'react-bootstrap/Modal'
import Button from 'react-bootstrap/Button'

import { useLoadingContext } from "../contexts/LoadingContext";
import { generateLogMessageString } from '../../utils/UtilityService';
import CartItem from './MarketplaceCartItem';
import { updateCart } from '../../utils/CartUtil';
import _icon from '../img/icon-cesmii-white.png'
import '../styles/Modal.scss';

const CLASS_NAME = "CartAddModal";

function CartAddModal(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [showModal, setShowModal] = useState(props.showModal);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState({marketplaceItem: props.item, quantity: 1, selectedPrice: null });
    const [_isValid, setIsValid] = useState(true);

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        setItem({ ..._item, marketplaceItem: props.item});

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.item]);

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    const onCancel = () => {
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        setShowModal(false);
        if (props.onCancel != null) props.onCancel();
    };

    const onAdd = () => {
        console.log(generateLogMessageString('onAdd', CLASS_NAME));

        //do validation
        if (!_isValid) {
            //alert("validation failed");
            return;
        }

        //add the item to the cart and save context
        let cart = updateCart(loadingProps.cart, _item.marketplaceItem, _item.quantity, _item.selectedPrice);
        setLoadingProps({ cart: cart });
        if (props.onAdd) props.onAdd();
    };

    const onValidate = (id, isValid) => {
        console.log(generateLogMessageString('onValidate', CLASS_NAME));
        setIsValid(isValid.required && isValid.numeric && isValid.range);
    };

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onChange = (itm, qty) => {
        console.log(generateLogMessageString('onChange', CLASS_NAME));
        setItem({ ..._item, marketplaceItem: itm, quantity: qty });
    };

    const onSelectPrice = (itm, price) => {
        console.log(generateLogMessageString('onSelectPrice', CLASS_NAME));
        setItem({ ..._item, marketplaceItem: itm, selectedPrice: price });
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (props.item == null) return;

    //return final ui
    return (
        <>
            {/* Add animation=false to prevent React warning findDomNode is deprecated in StrictMode*/}
            <Modal animation={false} show={showModal} onHide={onCancel} centered>
                <Modal.Header className="py-2 pb-1 d-flex align-items-center" closeButton>
                    <Modal.Title className="d-flex align-items-center py-2">
                        <img className="mr-2 icon" src={_icon} alt="CESMII icon"></img>
                        <span className="headline-3 d-none d-md-block">SM Marketplace - </span>
                        <span className="headline-3">Add to Cart</span>
                    </Modal.Title>
                </Modal.Header>
                <Modal.Body >
                    <CartItem item={_item} isAdd={true} onChange={onChange} onSelectPrice={onSelectPrice} onValidate={onValidate} />
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="text-solo" className="mx-1" onClick={onCancel} >Cancel</Button>
                    <Button variant="secondary" type="button" className="mx-3" onClick={onAdd} >Add to Cart</Button>
                </Modal.Footer>
            </Modal>
        </>
    )
}

export default CartAddModal;


