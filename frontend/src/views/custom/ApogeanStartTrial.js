import React, { useState } from 'react'
import { Form } from 'react-bootstrap';
import Button from 'react-bootstrap/Button'

import { generateLogMessageString, validate_Email } from '../../utils/UtilityService'
import '../styles/RequestInfo.scss';

const CLASS_NAME = "ApogeanStartTrial";

function ApogeanStartTrial(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_item, setItem] = useState(
        {
            hostUrl: window.location.host,
            smipSettings: null,
            formData: { firstName: '', lastName: '', email: '', phone: '', companyName: '' }
        });
    const [_isValid, setIsValid] = useState({
        firstName: true, lastName: true, email: true, emailFormat: true,
        companyName: true, phone: true
    });

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_firstName = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, firstName: isValid });
    };

    const validateForm_lastName = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, lastName: isValid });
    };

    const validateForm_email = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        var isValidEmail = validate_Email(e.target.value);
        setIsValid({ ..._isValid, email: isValid, emailFormat: isValidEmail });
    };

    const validateForm_phone = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, phone: isValid });
    };

    const validateForm_companyName = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, companyName: isValid });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.firstName = _item.formData.firstName != null && _item.formData.firstName.trim().length > 0;
        _isValid.lastName = _item.formData.lastName != null && _item.formData.lastName.trim().length > 0;
        _isValid.email = _item.formData.email != null && _item.formData.email.trim().length > 0;
        _isValid.emailFormat = validate_Email(_item.formData.email);
        _isValid.companyName = _item.formData.companyName != null && _item.formData.companyName.trim().length > 0;
        _isValid.phone = _item.formData.phone != null && _item.formData.phone.trim().length > 0;//I'm not sure about our validation here is it just US numbers or int'l
        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.firstName && _isValid.lastName && _isValid.email && _isValid.emailFormat &&
            _isValid.companyName && _isValid.phone);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onCancel = () => {
        if (props.onCancel) props.onCancel();
    };

    // initiate submit and creation of trial, parent will handle event
    const onSubmit = () => {

        //raised from header nav
        console.log(generateLogMessageString('onSubmit', CLASS_NAME));

        //do validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        if (props.onExecute) props.onExecute(_item);
    };
  
    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "firstName":
            case "lastName":
            case "email":
            case "companyName":
            case "phone":
                _item.formData[e.target.id] = e.target.value;
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    //return final ui
    return (
        <>
        <Form noValidate >
            <div className="row">
                <div className="col-md-6">
                    <Form.Group>
                        <Form.Label htmlFor="firstName" >First Name</Form.Label>
                        {!_isValid.firstName &&
                            <span className="invalid-field-message inline">
                                Required
                            </span>
                        }
                        <Form.Control id="firstName" className={(!_isValid.firstName ? 'invalid-field minimal pr-5' : 'minimal pr-5')} 
                            value={_item.formData.firstName} onBlur={validateForm_firstName} onChange={onChange} />
                    </Form.Group>
                </div>
            </div>
            <div className="row">
                <div className="col-md-6">
                    <Form.Group>
                        <Form.Label htmlFor="lastName" >Last Name</Form.Label>
                        {!_isValid.lastName &&
                            <span className="invalid-field-message inline">
                                Required
                            </span>
                        }
                        <Form.Control id="lastName" className={(!_isValid.lastName ? 'invalid-field minimal pr-5' : 'minimal pr-5')} 
                            value={_item.formData.lastName} onBlur={validateForm_lastName} onChange={onChange} />
                    </Form.Group>
                </div>
            </div>
            <div className="row">
                <div className="col-md-6">
                    <Form.Group>
                        <Form.Label htmlFor="email" >Email</Form.Label>
                        {!_isValid.email &&
                            <span className="invalid-field-message inline">
                                Required
                            </span>
                        }
                        {!_isValid.emailFormat &&
                            <span className="invalid-field-message inline">
                                Invalid Format (ie. jdoe@abc.com)
                            </span>
                        }
                        <Form.Control id="email" type="email" className={(!_isValid.email || !_isValid.emailFormat ? 'invalid-field minimal pr-5' : 'minimal pr-5')} 
                            value={_item.formData.email} onBlur={validateForm_email} onChange={onChange} />
                    </Form.Group>
                </div>
            </div>
            <div className="row">
                <div className="col-md-6">
                    <Form.Group>
                        <Form.Label htmlFor="phone" >Phone</Form.Label>
                        {!_isValid.phone &&
                            <span className="invalid-field-message inline">
                                Required
                            </span>
                        }
                        <Form.Control id="phone" type="phone" className={(!_isValid.phone ? 'invalid-field minimal pr-5' : 'minimal pr-5')} 
                            value={_item.formData.phone} onBlur={validateForm_phone} onChange={onChange} />
                    </Form.Group>
                </div>
            </div>
            <hr className="my-3" />
            <div className="row">
                <div className="col-md-6">
                    <Form.Group>
                        <Form.Label htmlFor="companyName" >Company Name</Form.Label>
                        {!_isValid.companyName &&
                            <span className="invalid-field-message inline">
                                Required
                            </span>
                        }
                        <Form.Control id="companyName" className={(!_isValid.companyName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                            value={_item.formData.companyName} onBlur={validateForm_companyName} onChange={onChange} />
                    </Form.Group>
                </div>
            </div>
            <hr className="my-3" />
            <div className="row">
                <div className="col-md-12">
                    <Button variant="primary" type="button" className="ml-2" onClick={onSubmit} >Submit</Button>
                    <Button variant="text-solo" className="ml-1" onClick={onCancel} >Cancel</Button>
                </div>
            </div>
        </Form>
        </>
    )
}

export default ApogeanStartTrial;
