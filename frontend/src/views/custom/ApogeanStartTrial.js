import React, { useState, useEffect } from 'react'
import { Form } from 'react-bootstrap';
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import Button from 'react-bootstrap/Button'

import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString, validate_Email, scrollTopScreen, formatItemPublishDate } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import '../styles/RequestInfo.scss';

const CLASS_NAME = "ApogeanStartTrial";

function ApogeanStartTrial() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    //can access this form for performing start trial of the Apogean ontime edge application
    const { id, jobId } = useParams();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_jobDef, setJobDef] = useState(null);
    const [_marketplaceItem, setMarketplaceItem] = useState(null);
    const [_item, setItem] = useState(
        {
            hostUrl: null,
            marketplaceItem: null,
            formData: { firstName: '', lastName: '', email: '', phone: '', companyName: '' }
        });
    const [_isValid, setIsValid] = useState({
        firstName: true, lastName: true, email: true, emailFormat: true,
        companyName: true, phone: true
        
    });
    const [_forceReload, setForceReload] = useState(0); //increment this value to cause a re-init of the page.
    const _caption = 'Start Trial';

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    //get job def record
    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));

            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                const data = { id: id };
                const url =  `job/getbyid`;
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this item.';
                console.error(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This item was not found.';
                    history.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            setJobDef(result.data);

            setLoadingProps({ isLoading: false, message: null });
        }

        //fetch referrer data 
        if (jobId != null) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [jobId]);


    //get marketplace item record
    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));

            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                const data = { id: id, isTracking: true };
                const url = `marketplace/getbyname`;
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this item.';
                console.error(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This item was not found.';
                    history.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            setMarketplaceItem(result.data);

            setLoadingProps({ isLoading: false, message: null });
        }

        //fetch referrer data 
        if (id != null) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id]);

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

        _isValid.firstName = _item.firstName != null && _item.firstName.trim().length > 0;
        _isValid.lastName = _item.lastName != null && _item.lastName.trim().length > 0;
        _isValid.email = _item.email != null && _item.email.trim().length > 0;
        _isValid.emailFormat = validate_Email(_item.email);
        _isValid.companyName = _item.companyName != null && _item.companyName.trim().length > 0;
        _isValid.phone = _item.phone != null && _item.phone.trim().length > 0;//I'm not sure about our validation here is it just US numbers or int'l
        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.firstName && _isValid.lastName && _isValid.email && _isValid.emailFormat &&
            _isValid.companyName && _isValid.phone);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onCancel = () => {
        //raised from header nav
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        history.goBack();
    };

    // Load lookup data upon certain triggers in the background
    const onExecuteJob = () => {

        //raised from header nav
        console.log(generateLogMessageString('onSave', CLASS_NAME));

        //do validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        const url = `job/execute`;
        console.log(generateLogMessageString(`onExecuteJob||${url}`, CLASS_NAME));

        setLoadingProps({
            isLoading: true, message: `Initiating ${_jobDef.name}...This may take a few minutes.`
        });

        //payload specific for this job
        const data = {
            marketplaceItemId: _marketplaceItem.id,
            jobDefinitionId: _jobDef.id,
            payload: _item
        }
        axiosInstance.post(url, data).then(result => {
            if (result.status === 200 && result.data.isSuccess) {
                //asynch flow - we kick off job and then a 2nd component polls to look for updated progress messages. 
                var jobLogs = loadingProps.jobLogs == null || loadingProps.jobLogs.length === 0 ? [] :
                    JSON.parse(JSON.stringify(loadingProps.jobLogs));
                jobLogs.push({ id: result.data.data, status: AppSettings.JobLogStatus.InProgress, message: null });
                setLoadingProps({
                    isLoading: false, message: null,
                    jobLogs: jobLogs,
                    activateJobLog: true,
                    isImporting: false
                });

                scrollTopScreen();

            } else {
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred initializing this job. Please contact a system administrator.`, isTimed: true }]
                });
            }
        }).catch(e => {
            if (e.response && e.response.status === 401) {
            }
            else {
                console.log(generateLogMessageString('useEffect||executeActivationWorkflow||' + JSON.stringify(e), CLASS_NAME, 'error'));
                console.log(e);
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred initializing this job. Please contact a system administrator.`, isTimed: true }]
                });
            }
        });
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
                _item[e.target.id] = e.target.value;
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }


    const onBack = () => {
        //raised from header nav
        console.log(generateLogMessageString('onBack', CLASS_NAME));
        history.goBack();
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderLeftRail = () => {
        if (!loadingProps.isLoading && (_marketplaceItem == null)) {
            return;
        }
        return (
            <div className="info-panel d-none d-sm-block">
                <div className="info-section py-3 px-1 rounded">
                    {id != null &&
                        renderSolutionDetails()
                    }
                </div>
            </div>
        );
    }

    const renderHeaderBlock = () => {
        return (
            <h1 className="m-0 mr-2">
                {_caption}
                {_marketplaceItem != null &&
                    ` - ${_marketplaceItem.displayName}`
                }
            </h1>
        )
    }

    const renderSolutionDetails = () => {
        if (_marketplaceItem == null) return;
        return (
            <div className="px-2">
                <div className="row mb-3" >
                    <div className="col-sm-12">
                        <p className="mb-2 headline-3 p-1 px-2 w-100 d-block rounded">
                            {_marketplaceItem.displayName}
                        </p>
                        <div className="px-2 mb-2" dangerouslySetInnerHTML={{ __html: _marketplaceItem.abstract }} ></div>
                        {_marketplaceItem.publishDate != null &&
                            <p className="px-2 mb-0">Published: {formatItemPublishDate(_marketplaceItem)}</p>
                        }
                    </div>
                </div>
            </div>
        )
    }

    //
    const renderSubTitle = () => {
        return (
            <span onClick={onBack} className="px-2 btn btn-text-solo align-items-center ml-auto ml-sm-0 auto-width d-flex clickable hover" ><i className="material-icons">chevron_left</i>Back</span>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    //return final ui
    const _title = `${_caption} | ${AppSettings.Titles.Main}`;

    return (
        <>
            <Helmet>
                <title>{_title}</title>
                <meta property="og:title" content={_title} />
            </Helmet>
            <div className="row py-2 pb-3" >
                <div className="col-sm-3 d-flex align-items-center" >
                    {renderSubTitle()}
                </div>
                <div className="col-sm-9 d-flex align-items-center" >
                    { renderHeaderBlock() }
                </div>
            </div>

            <div className="row" >
                <div className="col-sm-3 order-2 order-sm-1" >
                    {renderLeftRail()}
                </div>
                <div className="col-sm-9 mb-4 order-1 order-sm-2" >
                    <hr className="my-2" />
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
                                        value={_item.firstName} onBlur={validateForm_firstName} onChange={onChange} />
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
                                        value={_item.lastName} onBlur={validateForm_lastName} onChange={onChange} />
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
                                        value={_item.email} onBlur={validateForm_email} onChange={onChange} />
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
                                        value={_item.phone} onBlur={validateForm_phone} onChange={onChange} />
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
                                        value={_item.companyName} onBlur={validateForm_companyName} onChange={onChange} />
                                </Form.Group>
                            </div>
                        </div>
                        <hr className="my-3" />
                        <div className="row">
                            <div className="col-md-12">
                                <Button variant="primary" type="button" className="ml-2" onClick={onExecuteJob} >Submit</Button>
                                <Button variant="text-solo" className="ml-1" onClick={onCancel} >Cancel</Button>
                            </div>
                        </div>
                    </Form>
                </div>
            </div>
        </>
    )
}

export default ApogeanStartTrial;
