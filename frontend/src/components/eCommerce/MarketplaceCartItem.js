import React, { useState, useEffect, Fragment } from 'react'
import { Form, Col, Card } from 'react-bootstrap'
import { validateCartItem_Quantity } from '../../utils/CartUtil';
import { generateLogMessageString } from '../../utils/UtilityService';
import _icon from '../img/icon-cesmii-white.png'
import '../styles/Modal.scss';
import { useLoadingContext } from "../contexts/LoadingContext";

const CLASS_NAME = "CartItem";

function CartItem(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_isValid, setIsValid] = useState({ required: true, range: true, numeric: true });
    const [_quantity, setQuantity] = useState(null);

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        setQuantity(props.item.quantity);

        if (props.item.marketplaceItem.paymentPriceId != null) {
            props.item.marketplaceItem.prices.map((price, i) => {
                if (price.priceId == props.item.marketplaceItem.paymentPriceId) {
                    price.isSelected = true;
                }
            });
        }
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
                else if (isNaN(parseInt(e.target.value))) {
                    qty = null;
                }
                else {
                    qty = parseInt(e.target.value);
                }
                if (props.onChange != null) props.onChange(props.item.marketplaceItem, qty);

            default:
                console.log(e);
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

    const renderOneTimePriceForm = (price, showRadio) => {
        return (
            <Form.Group>
                <Form.Row>
                    {showRadio &&
                        <Form.Check type={"radio"} >
                            <Form.Check.Input type={"radio"} name="rbgPriceSelection" id={`price-${price.priceId}`} checked={price.isSelected}
                                onClick={(e) => {
                                    if (e.target.checked) {
                                        props.item.marketplaceItem.paymentPriceId = price.priceId;
                                    }
                                }}
                            />
                        <Form.Check.Label>{price.description}</Form.Check.Label>
                        </Form.Check>
                    }
                    {!showRadio &&
                        <Col className='d-flex'>
                            <Form.Label>Price</Form.Label>
                        </Col>
                    }
                    <Col className='d-flex'>
                        <Form.Label className='ml-auto'>${price.amount}</Form.Label>
                    </Col>
                </Form.Row>
                
                <Form.Row >
                    <Col>
                        <Form.Label htmlFor={`quantity_${props.item.marketplaceItem?.id}`}>Quantity</Form.Label>
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
                    </Col>
                    <Col>
                        <Form.Control id={`quantity_${props.item.marketplaceItem?.id}`} name="quantity" className={`minimal text-right ${!(_isValid.required || _isValid.numeric || _isValid.range) ? 'invalid-field' : ''}`}
                            value={_quantity == null ? '' : _quantity} onBlur={validateForm_quantity} onChange={onChange} />
                    </Col>
                </Form.Row>
            </Form.Group>
        );
    };

    const renderOneTimePrice = (price) => {
        if (price.billingPeriod == "OneTime") {
            return (
                <Card body className='elevated my-1'>
                    {renderOneTimePriceForm(price, true)}
                </Card>
            );
        }
    };

    const renderRecurringPrice = (price) => {
        if (price.billingPeriod == "Yearly" || price.billingPeriod == "SixMonths" || price.billingPeriod == "Monthly" ) {
            return (
                <Card body className='elevated my-1'>
                    <Form.Group>                        
                        <Form.Row>
                            <Form.Check type={"radio"}>
                                <Form.Check.Input type={"radio"} name="rbgPriceSelection" id={`price-${price.priceId}`} checked={price.isSelected}
                                    onClick={(e) => {
                                        if (e.target.checked) {
                                            props.item.marketplaceItem.paymentPriceId = price.priceId;
                                        }
                                    }}
                                />
                                <Form.Check.Label>{price.description}</Form.Check.Label>
                            </Form.Check>
                            <Col className='d-flex'>
                                <Form.Label className='ml-auto' >${price.amount}</Form.Label>
                            </Col>
                        </Form.Row>
                    </Form.Group>
                </Card>
            );
        }
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderForm = () => {
        if (props == null || props.item.marketplaceItem?.prices == null) {
            return;
        }

        const mainBodySubscription = props.item.marketplaceItem.prices.map((price, i) => {
            return (
                <Fragment key={i}>
                    {renderRecurringPrice(price)}
                    {renderOneTimePrice(price, true)}                        
                </Fragment>
            );            
        });

        if (props.item.marketplaceItem.prices.length == 1 &&
            props.item.marketplaceItem.prices[0].billingPeriod == "OneTime") {

            props.item.marketplaceItem.paymentPriceId = props.item.marketplaceItem.prices[0].priceId;


            return (
                <div >
                    {renderOneTimePriceForm(props.item.marketplaceItem.prices[0], false)}
                </div>
            );
        }
        return (
            <div >
                {mainBodySubscription}
            </div>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (props.item == null || props.item.marketplaceItem == null) return null;

    //return final ui
    return (
        <Form.Group >
            <Form.Row>
                <Col>
                    <div className="pb-0">
                        <span className="font-weight-bold mb-1" >{props.item.marketplaceItem.displayName}</span>
                        {(props.showAbstract && props.item.marketplaceItem.abstract != null) &&
                            <div dangerouslySetInnerHTML={{ __html: props.item.marketplaceItem.abstract }} ></div>
                        }                       
                    </div>
                </Col>
                <Col>
                    {!props.isAdd &&
                        <Col className='w-25' >
                            <button className="btn btn-icon-outline circle ml-auto" title="Remove item from cart" onClick={onRemoveItem} >
                                <i className="material-icons">close</i>
                            </button>
                        </Col>
                    }
                </Col>
            </Form.Row>
            <Form.Row >
                <Col className='d-flex'>
                    {renderForm()}
                </Col>
            </Form.Row>
       </Form.Group>
    )
}

export default CartItem;