import React, { useEffect, useState } from 'react'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

import { useLoadingContext } from "../contexts/LoadingContext";
import { generateLogMessageString } from '../../utils/UtilityService';
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
    const [_quantity, setQuantity] = useState(1);
    const [_isValid, setIsValid] = useState({required: true, range: true, numeric: true});

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        setQuantity(1);

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.item]);

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "quantity":
                //update the state
                if (e.target.value == '') {
                    setQuantity(e.target.value);
                    return;
                }
                else if (isNaN(parseInt(e.target.value))) return;
                
                setQuantity(parseInt(e.target.value));
            default:
                return;
        }
    }

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validate_quantity = (val) => {
        var required = (val != null);
        var numeric = required && (!isNaN(parseInt(val)));
        var range = required && parseInt(val) > 0;
        return { required: required, numeric: numeric, range: range };
    };

    const validateForm_quantity = (e) => {
        const result = validate_quantity(e.target.value);
        setIsValid(result);
    };

    // check validation
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        const result = validate_quantity(_quantity);
        setIsValid(result);
        return (_isValid.required && _isValid.numeric && _isValid.range);
    }


    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onCancel = () => {
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        setShowModal(false);
        if (props.onCancel != null) props.onCancel();
    };

    const onAdd = () => {
        console.log(generateLogMessageString('onAdd', CLASS_NAME));

        //do validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //add the item to the cart and save context
        let cart = updateCart(loadingProps.cart, props.item, _quantity);
        setLoadingProps({ cart: cart });

        //call parent form which will combine request info and sm profile and submit to server.
        if (props.onAdd) props.onAdd(_quantity);
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderForm = () => {
        return (
            <Form noValidate >
                <div className="row">
                    <div className="col-12">
                        <Form.Group>
                            <Form.Label htmlFor="quantity" >Quantity</Form.Label>
                            {!_isValid.required &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            {!_isValid.range &&
                                <span className="invalid-field-message inline">
                                    Enter a number greater than 0
                                </span>
                            }
                            {!_isValid.numeric &&
                                <span className="invalid-field-message inline">
                                    Enter a valid integer
                                </span>
                            }
                            <Form.Control id="quantity" className={(!(_isValid.required || _isValid.numeric || _isValid.range) ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_quantity} onBlur={validateForm_quantity} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
            </Form>
        );
    };


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
                <Modal.Body className="my-1 py-2">
                    <div className="mb-2 pb-2 border-bottom">
                        <span>{props.item.displayName}</span>
                        {props.item.abstract != null &&
                            <>
                            -
                            <div dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                            </>
                        }
                    </div>
                    {renderForm()}
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


