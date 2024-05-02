import React, { useState, useEffect, Fragment } from 'react'
import { Form, Col, Card } from 'react-bootstrap'
import { validateCartItem_Quantity } from '../../utils/CartUtil';
import { generateLogMessageString } from '../../utils/UtilityService';
import _icon from '../img/icon-cesmii-white.png'
import '../styles/Modal.scss';

const CLASS_NAME = "CartItem";

function CartItem(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_isValid, setIsValid] = useState({ required: true, range: true, numeric: true });
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
    // Region: helper methods
    //-------------------------------------------------------------------
    //we don't have a unique id, combine fields to produce a unique id
    const getPriceId = (price, counter = 0) => {
        if (price == null) return null;
        return `${price.priceId}-${price.billingPeriod}-${price.description}-${price.amount}-${counter}`;
    }

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        //note the use of the name property rather than id because of the row grid type nature of this component.
        switch (e.target.id) {
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
                if (props.onChange != null) props.onChange(props.item.marketplaceItem, qty, props.item.selectedPrice);
                break;
            default:
                console.log(e);
                return;
        }
    }

    const onSelectPrice = (price) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));
        if (props.onChange != null) props.onChange(props.item.marketplaceItem, props.item.quantity, price);
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

    const renderSelectItem = (price, counter) => {
        const id = getPriceId(price, counter);
        const selectedId = getPriceId(props.item?.selectedPrice, counter);
        return (
            <Form.Check type={"radio"} >
                <Form.Check.Input type={"radio"} name="rbgPriceSelection" id={id} checked={id == selectedId}
                    onChange={(e) => {
                        if (e.target.checked) {
                            onSelectPrice(price);
                        }
                    }}
                />
                <Form.Check.Label htmlFor={id} >{price.description}</Form.Check.Label>
            </Form.Check>
        )
    }

    const renderQuantity = () => {
        return (
            <div className="row" >
                <div className="col-8 col-lg-9 text-right align-content-center" >
                    <Form.Label htmlFor={`quantity`} >Quantity</Form.Label>
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
                </div>
                <div className="col-4 col-lg-3 align-content-center" >
                    <Form.Control id="quantity" className={`minimal text-right ${!(_isValid.required || _isValid.numeric || _isValid.range) ? 'invalid-field' : ''}`}
                        value={_quantity == null ? '' : _quantity} onBlur={validateForm_quantity} onChange={onChange} />
                </div>
            </div>
        );
    };

    const renderPriceItem = (price, counter, showRadio) => {
        return (
            <Form.Group>
                <Form.Row>
                    {showRadio &&
                        renderSelectItem(price, counter)
                    }
                    {!showRadio &&
                        <Col className='d-flex'>
                        <Form.Label>{price.description}</Form.Label>
                        </Col>
                    }
                    <Col className='d-flex'>
                        <Form.Label className='ml-auto'>${price.amount}</Form.Label>
                    </Col>
                </Form.Row>
            </Form.Group>
        );
    };
    /*
    const renderRecurringPrice = (price, showRadio) => {
        if (price.billingPeriod == "Yearly" || price.billingPeriod == "SixMonths" || price.billingPeriod == "Monthly" ) {
            return (
                <Form.Group>                        
                    <Form.Row>
                        {showRadio &&
                            renderSelectItem(price)
                        }
                        {!showRadio &&
                            <Col className='d-flex'>
                            <Form.Label>{price.description}</Form.Label>
                            </Col>
                        }
                        <Col className='d-flex'>
                            <Form.Label className='ml-auto' >${price.amount}</Form.Label>
                        </Col>
                    </Form.Row>
                </Form.Group>
            );
        }
    };
    */

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderPrices = () => {
        if (props == null || props.item.marketplaceItem?.prices == null) {
            return;
        }

        //if only show selected, trim down list to selected item only
        var pricesFiltered = props.item.marketplaceItem.prices.filter((x, counter) => {
            const id = getPriceId(x, counter);
            const selectedId = getPriceId(props.item?.selectedPrice, counter);
            return !props.showSelectedPriceOnly ? true : id === selectedId;
        });

        const pricesHtml = pricesFiltered.map((price, i) => {
            return (
                <Fragment key={i} >
                    {props.item.marketplaceItem.prices.length === 1 ?
                    //if we only have one price option, then just display the one w/o the elevated card
                    renderPriceItem(price, i, false)
                    :
                    //else - multiple price options w/ card layout
                    <Card body key={i} className='elevated my-1 rounded'>
                        {renderPriceItem(price, i, true)}
                    </Card>
                    }
                </Fragment>
            );            
        });

        return (
            <>
                {pricesHtml}
                {renderQuantity()}
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (props.item == null || props.item.marketplaceItem == null) return null;

    //return final ui
    return (
        <>
            <div className="row">
                <div className={props.isAdd ? 'col-12' : 'col-11'}>
                    <span className="font-weight-bold mb-1" >{props.item.marketplaceItem.displayName}</span>
                    {(props.showAbstract && props.item.marketplaceItem.abstract != null) &&
                        <div dangerouslySetInnerHTML={{ __html: props.item.marketplaceItem.abstract }} ></div>
                    }                       
                </div>
                {!props.isAdd &&
                <div className="col-1">
                        <button className="btn btn-icon-outline circle ml-auto" title="Remove item from cart" onClick={onRemoveItem} >
                            <i className="material-icons">close</i>
                        </button>
                </div>
                }
            </div>
            <div className="row">
                <div className="col-12">
                    {renderPrices()}
                </div>
            </div>
       </>
    )
}

export default CartItem;