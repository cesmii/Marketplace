import React, { useEffect, useState } from 'react'
import Form from 'react-bootstrap/Form'

import { generateLogMessageString } from '../../utils/UtilityService';
import _icon from '../img/icon-cesmii-white.png'
import '../styles/Modal.scss';

const CLASS_NAME = "CartItem";

function CartItem(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_isValid, setIsValid] = useState({required: true, range: true, numeric: true});

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------

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
                let qty = 0;
                if (e.target.value == '') {
                    qty = null;
                }
                else if (isNaN(parseInt(e.target.value)))
                {
                    qty = null;
                }
                else {
                    qty = parseInt(e.target.value);
                }
                if (props.onChange != null) props.onChange(props.item.marketplaceItem?.id, qty);

            default:
                return;
        }
    }

    const onRemoveItem = (e) => {
        console.log(generateLogMessageString('onRemoveItem', CLASS_NAME));
        if (props.onRemoveItem != null) props.onRemoveItem(props.item.marketplaceItem?.id);
        e.preventDefault();
    };

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
        if (props.onValidate != null) props.onValidate(props.item.marketplaceItem?.id, result);
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderForm = () => {
        return (
            <Form noValidate >
                <div className="row">
                    <div className="col-8">
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
                                value={props.quantity == null ? '' : props.quantity} onBlur={validateForm_quantity} onChange={onChange} />
                        </Form.Group>
                        <div className="col-4">
                            { !props.isAdd &&
                                <button className="btn btn-icon-outline circle ml-auto" title="Remove item from cart" onClick={onRemoveItem} ><i className="material-icons">close</i></button>
                            }
                        </div>
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
            <div className="mb-2 pb-2 border-bottom">
                <span className="font-weight-bold" >{props.item.marketplaceItem?.displayName}</span>
                {props.item.marketplaceItem?.abstract != null &&
                    <div dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                }
            </div>
            {renderForm()}
        </>
    )
}

export default CartItem;


