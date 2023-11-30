import React, { useState } from 'react'
import Form from 'react-bootstrap/Form'
import InputGroup from 'react-bootstrap/InputGroup'
import Button from 'react-bootstrap/Button'
import axiosInstance from "../services/AxiosService";

import { generateLogMessageString, validate_Email } from '../utils/UtilityService'
import { AppSettings } from '../utils/appsettings'
import { useLoadingContext } from "../components/contexts/LoadingContext";

const CLASS_NAME = "SubscribeForm";

function SubscribeForm() { 
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState(JSON.parse(JSON.stringify(AppSettings.requestInfoNew)));
    const [_isValid, setIsValid] = useState({email: true, emailFormat: true});

    ////-------------------------------------------------------------------
    //// Region: Event Handling of child component events
    ////-------------------------------------------------------------------
    //update search state so that form submit has value
    const onChange = (e) => {
        setItem({ ..._item, email: e.target.value, requestTypeCode: "subscribe" });
    }

    const validateForm_email = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        var isValidEmail = validate_Email(e.target.value);
        setIsValid({ ..._isValid, email: isValid, emailFormat: isValidEmail });
    };

    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.email = _item.email != null && _item.email.trim().length > 0;
        _isValid.emailFormat = validate_Email(_item.email);
        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.email && _isValid.emailFormat);
    }

    //trigger post to subscribe on click
    const onSubscribeClick = (e) => {

        console.log(generateLogMessageString(`onSubscribeClick`, CLASS_NAME));
        e.preventDefault();

        //do validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform insert call
        axiosInstance.post('requestinfo/add', _item)
            .then(resp => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "success", body: `Your inquiry was submitted. `, isTimed: true }
                    ]
                });

                //now refresh the object
                setItem (JSON.parse(JSON.stringify(AppSettings.requestInfoNew)));
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred submitting your inquiry.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });
    };

    ////-------------------------------------------------------------------
    //// Region: Render helpers
    ////-------------------------------------------------------------------
    const renderValidationMessage = () => {
        if (!_isValid.email) return "Required";
        if (!_isValid.emailFormat) return "Invalid Format (ie. jdoe@abc.com)";
        return "";
    }


    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    return (
        <Form onSubmit={onSubscribeClick} className="form-inline w-100 subscribe justify-content-md-end align-items-center">
            <InputGroup className="txt-search-ui-group">
                <Form.Control id="email" type="email" className={(!_isValid.email || !_isValid.emailFormat ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                    value={_item.email != null ? _item.email : ''} onBlur={validateForm_email} onChange={onChange}
                    placeholder="Enter valid email address" aria-label="Enter valid email address"
                    title={renderValidationMessage()}
                />
                <Button variant="primary" className="auto-width px-3 btn-subscribe" onClick={onSubscribeClick} type="submit" >
                    Subscribe
                </Button>
            </InputGroup>
        </Form>
    )

}

export default SubscribeForm