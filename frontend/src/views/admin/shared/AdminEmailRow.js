import React, { useState } from 'react'
import { Form } from 'react-bootstrap';

import { generateLogMessageString } from '../../../utils/UtilityService';

const CLASS_NAME = "AdminEmailRow";

function AdminEmailRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _publishType = [{ id: 'All', caption: 'All' }, { id: 'ECommerce', caption: 'ECommerce' }];
    const [_isValid, setIsValid] = useState({
        recipientName: true,
        emailAddress: true,
        publishType: true
    });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //on change handler to update state
    const onChange = (e) => {
        console.log(generateLogMessageString(`onChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "recipientName":
            case "emailAddress":
            case "publishType":
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
        console.log(generateLogMessageString(`onDelete||${props.item.recipientName}`, CLASS_NAME));

        //update state for other components to see
        if (props.onDeleteEmail != null) {
            props.onDeleteEmail(props.item.recipientName);
        }
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const validateForm_recipientName = (e) => {
        const isValid = e.target.value.toString() !== "";
        setIsValid({ ..._isValid, recipientName: isValid });
    };

    const validateForm_emailAddress = (e) => {
        const isValid = e.target.value.toString() !== "";
        setIsValid({ ..._isValid, emailAddress: isValid });
    };

    const validateForm_publishType = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, publishType: isValid });
    };


    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderPublishType = () => {

        //show drop down list for edit, copy mode
        const options = _publishType.map((item) => {
            return (<option key={item.id} value={item.id} >{item.caption}</option>)
        });

        return (
            <Form.Group>
                <Form.Control id="publishType" as="select" value={props.item.publishType == null ? "All" : props.item.publishType}
                    className={(!_isValid.publishType ? 'invalid-field minimal py-1' : 'minimal py-1')}
                    onBlur={validateForm_publishType}
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
    if (!_isValid.recipientName || !_isValid.emailAddress || !_isValid.publishType) {
        cssClass += ' alert alert-danger';
    }

    if (props.isHeader) {
        return (
            <div className={`row p-0 m-0 align-items-center ${cssClass}`}>
                <div className="col-2 col-sm-2 font-weight-bold pl-1 pr-0" >
                    Recipient Name
                </div>
                <div className="col-6 col-sm-6 font-weight-bold pl-1 pr-0" >
                    Email Address
                </div>
                <div className="col-3 col-sm-3 font-weight-bold pl-1 pr-0" >
                    Publish Type
                </div>
                <div className="col-1 col-sm-1 text-right font-weight-bold pl-1 pr-1" >
                    Delete
                </div>
            </div>
        );
    }

    //item row
    if (props.item === null || props.item === {}) return null;

    return (
        <div className={`row p-0 m-0 align-items-center ${cssClass}`}>
            <div className="col-2 col-sm-2 pl-1 pr-0 my-0" >
                <Form.Group>
                    <Form.Control id="recipientName" className={(!_isValid.recipientName ? 'invalid-field minimal py-1' : 'minimal py-1')} type="" placeholder={`Enter name`}
                        value={props.item.recipientName} onBlur={validateForm_recipientName} onChange={onChange} />
                </Form.Group>
            </div>
            <div className="col-6 col-sm-6 pl-1 pr-0 my-0" >
                <Form.Group>
                    <Form.Control id="emailAddress" className={(!_isValid.emailAddress ? 'invalid-field minimal py-1' : 'minimal py-1')} type="" placeholder={`Enter email address`}
                        value={props.item.emailAddress} onBlur={validateForm_emailAddress} onChange={onChange} />
                </Form.Group>
            </div>
            <div className="col-3 col-sm-3 pl-1 pr-0 my-0" >
                {renderPublishType()}
            </div>
            <div className="col-1 col-sm-1 text-right pt-1 pl-1 pr-1 my-0" >
                <button className="btn btn-icon-outline circle ml-auto" title="Delete Item" onClick={onDelete} ><i className="material-icons">close</i></button>
            </div>
            </div>
    );
}

export default AdminEmailRow;