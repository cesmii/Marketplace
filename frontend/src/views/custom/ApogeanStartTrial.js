import React, { useState, useEffect } from 'react'
import { Form } from 'react-bootstrap';
import Button from 'react-bootstrap/Button'
import { AppSettings } from '../../utils/appsettings';

import { generateLogMessageString, validate_Email } from '../../utils/UtilityService'
import { RenderImageBg } from '../../services/MarketplaceService';
import '../styles/MarketplaceEntity.scss';
import '../styles/RequestInfo.scss';

const CLASS_NAME = "ApogeanStartTrial";

function ApogeanStartTrial(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_jobStatus, setJobStatus] = useState(AppSettings.JobLogStatus.NotStarted);
    const [_item, setItem] = useState(
        {
            hostUrl: window.location.host,
            formData: { firstName: '', lastName: '', email: '', phone: '', organization: {name: ''} }
        });
    const [_isValid, setIsValid] = useState({
        firstName: true, lastName: true, email: true, emailFormat: true,
        organization: true, phone: true
    });

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        if (props.jobLog?.status == null) return;

        setJobStatus(props.jobLog?.status)

        //this will execute on unmount
        return () => {
        };
    }, [props.jobLog?.status]);

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

    const validateForm_organization = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, organization: isValid });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.firstName = _item.formData.firstName != null && _item.formData.firstName.trim().length > 0;
        _isValid.lastName = _item.formData.lastName != null && _item.formData.lastName.trim().length > 0;
        _isValid.email = _item.formData.email != null && _item.formData.email.trim().length > 0;
        _isValid.emailFormat = validate_Email(_item.formData.email);
        _isValid.organization = _item.formData.organization?.name != null && _item.formData.organization?.name.trim().length > 0;
        _isValid.phone = _item.formData.phone != null && _item.formData.phone.trim().length > 0;//I'm not sure about our validation here is it just US numbers or int'l
        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.firstName && _isValid.lastName && _isValid.email && _isValid.emailFormat &&
            _isValid.organization && _isValid.phone);
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
            case "phone":
                _item.formData[e.target.id] = e.target.value;
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

    //on change handler to update state
    const onChangeOrganization = (e) => {
        switch (e.target.id) {
            case "name":
                // Initialize formData if it doesn't exist
                if (!_item.formData) _item.formData = {};
                // Initialize organization if it doesn't exist
                if (!_item.formData.organization) _item.formData.organization = {};
                // Update the correct path
                _item.formData.organization.name = e.target.value;
                break;
            default:
                return;
        }
        // Update state with a proper shallow copy
        setItem({..._item});
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderMarketplaceHeader = (item) => {
        if (item == null) return;

        return (
            <>
                <div className={`row mx-0 p-0 mb-4 marketplace-list-item border`}>
                    <div className="col-sm-5 p-0" >
                        <RenderImageBg item={item} defaultImage={item.imagePortrait} responsiveImage={item.imagePortrait} clickable={false} />
                    </div>
                    <div className="col-sm-7 p-4" >
                        {(_jobStatus !== AppSettings.JobLogStatus.Completed && _jobStatus !== AppSettings.JobLogStatus.Failed) &&
                            <>
                                {renderIntro()}
                            </>
                        }
                        {_jobStatus === AppSettings.JobLogStatus.Completed &&
                            <>
                            <h2>Nice job! </h2>
                            <p>Starting a free trial of Apogean is the first step to collecting CNC machine data.</p>
                            <p>
                                Within one business day, we'll send you a license key to activate your free trial.  Meanwhile, you can get started by following the instructions below.</p>
                            </>
                        }
                        {_jobStatus === AppSettings.JobLogStatus.Failed &&
                            renderFailContent()
                        }
                    </div>
                </div>
            </>
        );
    };

    const renderIntro = () => {
        return (
            <div className="row mb-2">
                <div className="col-12">
                    <p>
                        Complete the form for a free 30 - day trial of Apogean. We'll send you a license key and a link to download and install the app within one business day.
                        To use it, you'll need:
                    </p>
                    {renderPrerequisites() }
                </div>
            </div>
        );
    }

    const renderPrerequisites = () => {
        return (
            <ul className="p-0 m-0 ml-3">
                <li className="m-0 p-0 my-1">Purchase a Windows 10 or later edge device<br />(we tested the <a href='https://a.co/d/64V2XJE' rel='noopener' target='_blank'>GMKtec Mini PC Windows 11</a> and the <a href='https://a.co/d/eQDeobH' rel='noopener' target='_blank'>Mini PC GoLite 11</a>)
                </li>
                <li className="m-0 p-0 my-1">Decide if you need a serial cable or ethernet cable to connect the CNC machine to the Windows device<br />(we tested this <a href="https://a.co/d/fJJIcVK" rel="noopener" target="_blank">USB to RS232 DB25 serial adapter cable</a>)
                </li>
                <li className="m-0 p-0 my-1">We'll send you the installation file and activation key within one business day
                </li>
            </ul>
        );
    }

    const renderSuccessContent = () => {
        return (
            <>
                <div className="row">
                    <div className="col-12">
                        <h2>Start getting these things ready:</h2>
                        <hr className="mt-2" />
                    </div>
                </div>
                <div className="row mb-2">
                    <div className="col-12">
                        {renderPrerequisites()}
                    </div>
                </div>
                <div className="row mb-2">
                    <div className="col-12">
                        <hr className="" />
                        <div dangerouslySetInnerHTML={{ __html: props.marketplaceItem.abstract }} ></div>
                    </div>
                </div>
            </>
        );
    }

    const renderFailContent = () => {
        return (
            <>
                <div className="row mb-2">
                    <div className="col-12">
                        <h2>An error occurred submitting your information.</h2>
                        <p><a href='/contact-us/support' >Please contact the system administrator.</a></p>
                        <hr className="mt-2" />
                        <div className="mb-2" dangerouslySetInnerHTML={{ __html: props.marketplaceItem.abstract }} ></div>
                    </div>
                </div>
            </>
        );
    }

    const renderForm = () => {
        const disabled = !(_jobStatus === AppSettings.JobLogStatus.NotStarted || _jobStatus === AppSettings.JobLogStatus.Failed);
        return (
            <Form noValidate className="mx-3" >
                <div className="row mb-2">
                    <div className="col-12">
                        <h2>Collect data from HAAS CNC with Apogean&#8482;</h2>
                        <hr className="mt-3" />
                    </div>
                </div>
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
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="name" >Company Name</Form.Label>
                            {!_isValid.organization &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="name" className={(!_isValid.organization ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.formData.organization?.name} onBlur={validateForm_organization} onChange={onChangeOrganization} />
                        </Form.Group>
                    </div>
                </div>
                <hr className="my-3" />
                <div className="row">
                    <div className="col-md-12">
                        <Button variant="primary" type="button" className="ml-2" onClick={onSubmit} disabled={disabled} >Submit</Button>
                        <Button variant="text-solo" className="ml-1" onClick={onCancel} >Cancel</Button>
                    </div>
                </div>
            </Form>
        );
    }

    const renderMainContent = () => {
        //if (_jobStatus === AppSettings.JobLogStatus.Failed) return;

        return (
            <>
                <div className="marketplace-list-item border" >
                    <div className="card mb-0 border-0">
                        <div className="card-body">
                            {(_jobStatus !== AppSettings.JobLogStatus.Completed) &&
                                <>
                                    {renderForm()}
                                </>
                            }
                            {_jobStatus === AppSettings.JobLogStatus.Completed &&
                                renderSuccessContent()
                            }
                        </div>
                    </div>
                </div>
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    //return final ui
    if (props.marketplaceItem == null) return null;

    return (
        <>
            {renderMarketplaceHeader(props.marketplaceItem)}
            {renderMainContent()}
        </>
    )
}

export default ApogeanStartTrial;
