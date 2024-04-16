import React, { useState, useEffect } from 'react'
import { Form, Col } from 'react-bootstrap'
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
    const [_quantity, setQuantity] = useState(null);

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        setQuantity(props.item.quantity);

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.item.quantity]);

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        //note the use of the name property rather than id because of the row grid type nature of this component.
        switch (e.target.name) {
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
                if (props.onChange != null) props.onChange(props.item.marketplaceItem, qty);

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
            <Form.Group className={props.className == null ? '' : props.className}>
                <Form.Row className="mb-2 d-flex align-items-center">
                    <Form.Label className="w-50" >Price</Form.Label>
                    <Col className={props.isAdd ? 'w-50 d-flex' : 'w-25 d-flex'} >
                        <Form.Label className={`ml-auto pr-2`} >${props.item.marketplaceItem?.price}</Form.Label>
                    </Col>
                    {!props.isAdd &&
                        <Col className='w-25' >
                        </Col>
                    }
                </Form.Row>
                <Form.Row className="mb-2 d-flex align-items-center">
                    <Form.Label htmlFor={`quantity_${props.item.marketplaceItem?.id}`} className="w-50" >Quantity</Form.Label>
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
                    <Col className={props.isAdd ? 'w-50' : 'w-25'} >
                        <Form.Control id={`quantity_${props.item.marketplaceItem?.id}`} name="quantity" className={`minimal px-2 text-right ${!(_isValid.required || _isValid.numeric || _isValid.range) ? 'invalid-field' : ''}`}
                            value={_quantity == null ? '' : _quantity} onBlur={validateForm_quantity} onChange={onChange} />
                    </Col>
                    {!props.isAdd &&
                        <Col className='w-25' >
                                <button className="btn btn-icon-outline circle ml-auto" title="Remove item from cart" onClick={onRemoveItem} ><i className="material-icons">close</i></button>
                        </Col>
                    }
                </Form.Row>
            </Form.Group>
        );
    };


    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (props.item == null || props.item.marketplaceItem == null) return null;

    //return final ui
    return (
        <>
            <div className="mb-2 pb-0">
                <span className="font-weight-bold mb-1" >{props.item.marketplaceItem.displayName}</span>
                {(props.showAbstract && props.item.marketplaceItem.abstract != null) &&
                    <div dangerouslySetInnerHTML={{ __html: props.item.marketplaceItem.abstract }} ></div>
                }
            </div>
            {renderForm()}
        </>
    )
}

export default CartItem;


