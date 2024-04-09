import React, { useState } from 'react'
import { Form, Row, Col } from 'react-bootstrap'
import { validateCartItem_Quantity } from '../../utils/CartUtil';
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
    const validateForm_quantity = (e) => {
        const result = validateCartItem_Quantity(e.target.value);
        setIsValid(result);
        if (props.onValidate != null) props.onValidate(props.item.marketplaceItem?.id, result);
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderForm = () => {
        return (
            <Form.Group>
                <Form.Row>
                    <Form.Label htmlFor="price" column lg={2}>Price</Form.Label>
                    <Form.Label htmlFor="price" column lg={2}>{props.item.marketplaceItem?.price}$</Form.Label>
                </Form.Row>
                <Form.Row>
                    <Form.Label htmlFor="quantity" column lg={2}>Quantity</Form.Label>
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
                    <Col column lg={2}>
                        <Form.Control id="quantity" className={(!(_isValid.required || _isValid.numeric || _isValid.range) ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                            value={props.item.quantity == null ? '' : props.item.quantity} onBlur={validateForm_quantity} onChange={onChange} />
                    </Col>
                    <Col column lg={1}>
                        {!props.isAdd &&
                            <button className="btn btn-icon-outline circle ml-auto" title="Remove item from cart" onClick={onRemoveItem} ><i className="material-icons">close</i></button>
                        }
                    </Col>
                </Form.Row>
            </Form.Group>
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


