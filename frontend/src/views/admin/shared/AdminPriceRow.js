import React, { useState } from 'react'
import { Form } from 'react-bootstrap';

//import { AppSettings } from '../../../utils/appsettings';
import { generateLogMessageString } from '../../../utils/UtilityService';

const CLASS_NAME = "AdminPriceRow";

function AdminPriceRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _billingPeriods = [{ id: 'OneTime', caption: 'OneTime' }, { id: 'Yearly', caption: 'Yearly' }, { id: 'Monthly', caption: 'Monthly' }];
    const [_isValid, setIsValid] = useState({
        amount: true,
        description: true
    });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onChange||e:${e.target}`, CLASS_NAME));

        switch (e.target.id) {
            case "amount":
                validateForm_amount(e);
                break;
            case "description":
                validateForm_description(e);
                break;
            default:
                break;
        }

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "amount":
            case "description":
            case "billingPeriod":
            case "priceId":
                props.item[e.target.id] = e.target.value;
                break;
            default:
                return;
        }
        //update the state
        if (props.onChangeItem) props.onChangeItem(JSON.parse(JSON.stringify(props.item)));
    }


    //called when an item is selected in the panel
    const onDelete = () => {
        console.log(generateLogMessageString(`onDelete||${props.item.caption}`, CLASS_NAME));

        //update state for other components to see
        if (props.onDelete != null) {
            props.onDelete(props.item.id);
        }
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const validateForm_amount = (e) => {
        const isValid = e.target.value.toString() !== "";
        setIsValid({ ..._isValid, amount: isValid });
    };

    const validateForm_description = (e) => {
        const isValid = e.target.value.toString() !== "";
        setIsValid({ ..._isValid, description: isValid });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderBillingPeriods = () => {

        //show drop down list for edit, copy mode
        const options = _billingPeriods.map((item) => {
            return (<option key={item.id} value={item.id} >{item.caption}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Billing Period</Form.Label>
                <Form.Control id="billingPeriod" as="select" value={props.item.billingPeriod == null ? "OneTime" : props.item.billingPeriod}
                    className={`minimal pr-5`}
                    onChange={onChange} >
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    let cssClass = props.cssClass + (props.isHeader ? " bottom header" : " center border-top");
    if (!_isValid.amount || !_isValid.description) {
        cssClass += ' alert alert-danger';
    }

    if (props.isHeader) {
        return (
            <div className={`row my-1 p-0 py-1 d-flex align-items-center ${cssClass}`}>
                <div className="col-sm-5 font-weight-bold" >
                    Amount
                </div>
                <div className="col-sm-4 font-weight-bold" >
                    Description
                </div>
                <div className="col-sm-3 text-right font-weight-bold" >
                    Billing Period
                </div>
                <div className="col-sm-2 text-right font-weight-bold" >
                    Price Id
                </div>
            </div>
        );
    }

    //item row
    if (props.item === null || props.item === {}) return null;

    return (
        <div className={`row my-1 p-0 py-1 d-flex align-items-center ${cssClass}`}>
            <div className="col-sm-5" >
                <Form.Group>
                    <Form.Label>Amount*</Form.Label>
                    {!_isValid.amount &&
                        <span className="invalid-field-message inline">
                            Required
                        </span>
                    }
                    <Form.Control id="amount" className={(!_isValid.amount ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter amount`}
                        value={props.item.amount} onBlur={validateForm_amount} onChange={onChange} />
                </Form.Group>
            </div>            
            <div className="col-sm-5" >
                <Form.Group>
                    <Form.Label>Description*</Form.Label>
                    {!_isValid.description &&
                        <span className="invalid-field-message inline">
                            Required
                        </span>
                    }
                    <Form.Control id="description" className={(!_isValid.description ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter description`}
                        value={props.item.description} onBlur={validateForm_description} onChange={onChange} />
                </Form.Group>
            </div>   
            <div className="col-sm-2 text-right pt-1" >
                <button className="btn btn-icon-outline circle ml-auto" title="Delete Item" onClick={onDelete} ><i className="material-icons">close</i></button>
            </div>
            <div className="col-sm-5" >
                {renderBillingPeriods()}
            </div>
        </div>
    );
}

export default AdminPriceRow;