import React, { useEffect, useState } from 'react'
import Modal from 'react-bootstrap/Modal'
import Button from 'react-bootstrap/Button'

import axiosInstance from "../../services/AxiosService";
import { useLoginStatus } from '../../components/OnLoginHandler';
import { AppSettings } from '../../utils/appsettings';
import { useLoadingContext } from "../contexts/LoadingContext";
import { generateLogMessageString } from '../../utils/UtilityService';
import CartItem from './CartItem';
import { updateCart } from '../../utils/CartUtil';
import _icon from '../img/icon-cesmii-white.png'
import '../styles/Modal.scss';
import ConfirmationModal from '../../components/ConfirmationModal';
import color from '../../components/Constants';

const CLASS_NAME = "CartAddModal";

function CartAddModal(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [showModal, setShowModal] = useState(props.showModal);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState({marketplaceItem: props.item, quantity: 1, selectedPrice: null });
    const [_isValid, setIsValid] = useState(true);
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const { isAuthenticated } = useLoginStatus(null, [AppSettings.AADAdminRole]);

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        if (props.item?.eCommerce?.prices == null) return;

        //if exactly one item, set selected price to that value. 
        const selectedPrice = props.item?.eCommerce?.prices != null && props.item?.eCommerce?.prices.length === 1 ?
            props.item?.eCommerce?.prices[0] : null;
        setItem({ ..._item, marketplaceItem: props.item, selectedPrice: selectedPrice });

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
        );
    };

    async function AddCart(cart) {
        const url = `ecommerce/cart/add`;
        axiosInstance.post(url, cart)
            .then(resp => {
                if (resp.data.isSuccess) {
                    console.log(resp.data.data);
                    setLoadingProps({ cart: resp.data.data });
                    if (props.onAdd) props.onAdd();
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
    }

    const onAdd = () => {
        console.log(generateLogMessageString('onAdd', CLASS_NAME));

        //do validation
        if (!_isValid) {
            //alert("validation failed");
            return;
        }

        //add the item to the cart and save context
        let cart = updateCart(loadingProps.cart, _item.marketplaceItem, _item.quantity, _item.selectedPrice);
        //If user authencated, save the cart items in to the database
        if (isAuthenticated) {
            AddCart(cart);
        } else {
            setLoadingProps({ cart: cart });
            if (props.onAdd) props.onAdd();
        }
    };

    const onValidate = (id, isValid) => {
        console.log(generateLogMessageString('onValidate', CLASS_NAME));
        setIsValid(isValid.required && isValid.numeric && isValid.range);
    };

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onChange = (itm, qty, price) => {
        console.log(generateLogMessageString('onChange', CLASS_NAME));
        if (loadingProps.cart != null && loadingProps.cart.items != null && loadingProps.cart.items.length !== 0) {
            if (price.billingPeriod !== "OneTime") {
                //loop through cart and see if item is there. If not, add, if there, add new quantity to existing.
                let cartItem = loadingProps.cart.items.find(x => {
                    return x.selectedPrice?.billingPeriod === "OneTime"
                });

                if (cartItem != null) {
                    setError({ show: true, caption: 'Cart - Error', message: 'Cannot add this item to the cart. Already onetime purchase item added to the cart.'});
                    return;
                }
            }
            else {
                //loop through cart and see if item is there. If not, add, if there, add new quantity to existing.
                let cartItem = loadingProps.cart.items.find(x => {
                    return x.selectedPrice?.billingPeriod === "Yearly" || x.selectedPrice?.billingPeriod === "Monthly"
                });

                if (cartItem != null) {
                    setError({ show: true, caption: 'Cart - Error', message: 'Cannot add this item to the cart. Already subscription purchase item added to the cart.' });
                    return;
                }
            }
        }
        
        //update state
        setItem({ ..._item, marketplaceItem: itm, quantity: qty, selectedPrice: price });
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
                    <CartItem item={_item} isAdd={true} onChange={onChange} showSelectedPriceOnly={false} onValidate={onValidate} />
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="text-solo" className="mx-1" onClick={onCancel} >Cancel</Button>
                    <Button variant="secondary" type="button" className="mx-3" onClick={onAdd} >Add to Cart</Button>
                </Modal.Footer>
            </Modal>
            {renderErrorMessage()}
        </>
    )
}

export default CartAddModal;


