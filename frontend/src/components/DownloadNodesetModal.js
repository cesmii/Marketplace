import React, { useEffect, useState } from 'react'
import Modal from 'react-bootstrap/Modal'

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

import { generateLogMessageString, validate_Email } from '../utils/UtilityService';
import { AppSettings } from '../utils/appsettings';
import _icon from './img/icon-cesmii-white.png'
import './styles/Modal.scss';

const CLASS_NAME = "DownloadNodesetModal";

function DownloadNodesetModal(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [showModal, setShowModal] = useState(props.showModal);
    const [_item, setItem] = useState(JSON.parse(JSON.stringify(AppSettings.requestInfoNew)));
    const [_isValid, setIsValid] = useState({
        firstName: true, lastName: true, email: true, emailFormat: true, companyName: true
    });
    const [_errorMsg, setErrorMessage] = useState(null);

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    // Trigger get lookup data from server (if necessary)
    useEffect(() => {
        //set item's sm profile value and request type
        setItem({ ..._item, externalSource: props.item.externalSource, externalId: props.item.id, externalItem: props.item, requestTypeCode: "smprofile-download" })

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
            case "firstName":
            case "lastName":
            case "email":
            case "companyName":
            //case "description":
            //case "companyUrl":
            //case "phone":
            //case "industries":
                _item[e.target.id] = e.target.value;
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

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

    const validateForm_companyName = (e) => {
        //var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        //setIsValid({ ..._isValid, companyName: isValid });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.firstName = _item.firstName != null && _item.firstName.trim().length > 0;
        _isValid.lastName = _item.lastName != null && _item.lastName.trim().length > 0;
        _isValid.email = _item.email != null && _item.email.trim().length > 0;
        _isValid.emailFormat = validate_Email(_item.email);
        //_isValid.companyName = _item.companyName != null && _item.companyName.trim().length > 0;
        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.firstName && _isValid.lastName && _isValid.email && _isValid.emailFormat && _isValid.companyName);
    }


    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onCancel = () => {
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        setShowModal(false);
        setErrorMessage(null);
        if (props.onCancel != null) props.onCancel();
    };

    const onDownload = () => {
        console.log(generateLogMessageString('onDownload', CLASS_NAME));
        setErrorMessage(null);

        //do validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //call parent form which will combine request info and sm profile and submit to server.
        if (props.onDownload) props.onDownload(_item);
    };

    const onDismissMessage = (e) => {
        console.log(generateLogMessageString('onDismissMessage||', CLASS_NAME));
        setErrorMessage(null);
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderForm = () => {
        return (
            <Form noValidate >
                <div className="row">
                    <div className="col-12">
                        <Form.Group>
                            <Form.Label htmlFor="firstName" >First Name</Form.Label>
                            {!_isValid.firstName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="firstName" className={(!_isValid.firstName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.firstName} onBlur={validateForm_firstName} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-12">
                        <Form.Group>
                            <Form.Label htmlFor="lastName" >Last Name</Form.Label>
                            {!_isValid.lastName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="lastName" className={(!_isValid.lastName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.lastName} onBlur={validateForm_lastName} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-12">
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
                                value={_item.email} onBlur={validateForm_email} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-12">
                        <Form.Group>
                            <Form.Label htmlFor="companyName" >Company Name</Form.Label>
                            {!_isValid.companyName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="companyName" className={(!_isValid.companyName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.companyName} onBlur={validateForm_companyName} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
            </Form>
        );
    };

    const renderErrorMessage = () => {
        if (_errorMsg == null || _errorMsg === '') return;

        return (
            <div className="alert alert-danger my-2" >
                <div className="dismiss-btn">
                    <Button variant="icon-solo square" onClick={onDismissMessage} className="align-items-center" ><i className="material-icons">close</i></Button>
                </div>
                <div className="text-center" >
                    {_errorMsg}
                </div>
            </div>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (_item == null) return;

    //return final ui
    return (
        <>
            {/* Add animation=false to prevent React warning findDomNode is deprecated in StrictMode*/}
            <Modal animation={false} show={showModal} onHide={onCancel} centered>
                <Modal.Header className="py-2 pb-1 d-flex align-items-center" closeButton>
                    <Modal.Title className="d-flex align-items-center py-2">
                        <img className="mr-2 icon" src={_icon} alt="CESMII icon"></img>
                        <span className="headline-3 d-none d-md-block">SM Marketplace - </span>
                        <span className="headline-3">Download Nodeset XML</span>
                    </Modal.Title>
                </Modal.Header>
                <Modal.Body className="my-1 py-2">
                    {renderErrorMessage()}
                    <p className="mb-2 pb-2 border-bottom">
                        <span className="font-weight-bold mr-2">Nodeset:</span>
                        <span>{props.item.displayName}</span>
                    </p>
                    <p className="mb-2 text-muted">
                        Enter your name, email and organization to begin the download. 
                    </p>
                    {renderForm()}
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="text-solo" className="mx-1" onClick={onCancel} >Cancel</Button>
                    <Button variant="secondary" type="button" className="mx-3" onClick={onDownload} >Download</Button>
                </Modal.Footer>
            </Modal>
        </>
    )
}

export default DownloadNodesetModal;


