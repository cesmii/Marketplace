import React, { useState, useEffect } from 'react'
import { Form } from 'react-bootstrap';
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";

import Button from 'react-bootstrap/Button'

import { AppSettings, LookupData } from '../utils/appsettings';
import { generateLogMessageString, validate_Email, scrollTopScreen, formatItemPublishDate } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";

import PublisherSidebar from './shared/PublisherSidebar';
import SocialMedia from '../components/SocialMedia';
import './styles/RequestInfo.scss';

const CLASS_NAME = "RequestInfo";

function RequestInfo() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    //can access this form for specific marketplace item (id) or for a specific publisher (publisherId)
    //TBD - update form to pull publisher info if coming from that angle.
    const { id, publisherId, type, itemType } = useParams();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState(JSON.parse(JSON.stringify(AppSettings.requestInfoNew)));
    const [_referrerItem, setReferrerItem] = useState(null);
    const [_isValid, setIsValid] = useState({
        firstName: true, lastName: true, email: true, emailFormat: true,
        companyName: true, companyUrl: true, phone: true, description: true,
        membershipStatus: true
    });
    const [_formDisplay, setFormDisplay] = useState({
        caption: 'Request Info', captionDescription: 'Message', requireCompanyInfo: true, showMembershipStatus: false, showIndustry: false
    });
    const [_forceReload, setForceReload] = useState(0); //increment this value to cause a re-init of the page.

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    //get referrer item
    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));

            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                var data = { id: id != null ? id : publisherId };
                var url = id != null ? `marketplace/getbyid` : `publisher/getbyid`;
                //if profile item, override url
                if (itemType != null && itemType === 'profile' && id != null) url = `profile/getbyid`;
                result = await axiosInstance.post(url, data);
               // console.log("Log result ",result);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this item.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
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

            //special handling for marketplace mode vs publisher mode
            if (id != null) {
                result.data.metaTagsConcatenated = result.data == null || result.data.metaTags == null ? "" : result.data.metaTags.join(', ');
            }
            //set item state value - this is either marketplace item, sm profile or publisher item based on mode
            setReferrerItem(result.data);

            //for SmProfile, update namespace value in request info object
            //if (itemType === "profile") {
            //    setItem({ ..._item, smProfile: { id: result.data.id, namespace: result.data.namespace } });
            //}

            setLoadingProps({ isLoading: false, message: null });
        }

        //fetch referrer data 
        if (id != null || publisherId != null) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id, publisherId]);


    //-------------------------------------------------------------------
    // Trigger get lookup data from server (if necessary)
    //-------------------------------------------------------------------
    useEffect(() => {
        //fetch referrer data 
        if (loadingProps.lookupDataStatic == null) {
            setLoadingProps({ refreshLookupData: true });
        }
    }, [loadingProps.lookupDataStatic]);

    //-------------------------------------------------------------------
    // Set caption, request type
    //-------------------------------------------------------------------
    useEffect(() => {

        //init to blank object
        var item = JSON.parse(JSON.stringify(AppSettings.requestInfoNew));

        //itemType - either marketplace or sm profile
        if (itemType === "app" && id != null) {
            item.marketplaceItemId = id;
            item.requestTypeCode = "marketplaceitem";
            setFormDisplay({
                ..._formDisplay, caption: 'Request More Info', captionDescription: 'Tell Us About Your Project(s)'
                , showMembershipStatus: true, showIndustry: true
            });
        }
        else if (itemType === "profile" && id != null) {
            item.smProfileId = id;
            item.requestTypeCode = "smprofile";
            setFormDisplay({
                ..._formDisplay, caption: 'Request More Info', captionDescription: 'Tell Us About Your Project(s)'
                , showMembershipStatus: true, showIndustry: true
            });
        }
        else if (publisherId != null)
        {
            item.publisherId = publisherId; 
            item.requestTypeCode = "publisher";
            setFormDisplay({
                ..._formDisplay, caption: 'Request Info', captionDescription: 'Message'
                , showMembershipStatus: true, showIndustry: true
            });
        }
        else if (type != null && type === "contribute")
        {
            item.requestTypeCode = "contribute";
            setFormDisplay({
                ..._formDisplay, caption: 'Contact Us - Become a Publisher', captionDescription: 'Tell Us About Your Project(s)'
                , requireCompanyInfo: true, showMembershipStatus: true, showIndustry: true
            });
        }
        else if (type != null && type === "membership") {
            item.requestTypeCode = "membership";
            setFormDisplay({
                ..._formDisplay, caption: 'Contact Us - Become a CESMII Member'
                , requireCompanyInfo: true, showMembershipStatus: true
            });
        }
        else if (type != null && type === "request-demo") {
            item.requestTypeCode = "request-demo";
            setFormDisplay({
                ..._formDisplay, caption: 'Contact Us - Request a Demo'
                , requireCompanyInfo: true, showMembershipStatus: true, showIndustry: true
            });
        }
        else if (type != null && type === "support") {
            item.requestTypeCode= "support";
            setFormDisplay({
                ..._formDisplay, caption: 'Contact Us - Support'
                , requireCompanyInfo: false
            });
        }
        else {
            item.requestTypeCode = "general";
            setFormDisplay({
                ..._formDisplay, caption: 'Contact Us'
                , requireCompanyInfo: false, showMembershipStatus: true, showIndustry: true
            });
        }

        //update state
        setItem(item);

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id, publisherId, type, itemType, _forceReload]);


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
        var isValid = !_formDisplay.requireCompanyInfo || (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, companyName: isValid });
    };

    const validateForm_companyUrl = (e) => {
        var isValid = true; //!_formDisplay.requireCompanyInfo || (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, companyUrl: isValid });
    };
    const validateForm_description = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, description: isValid });
    };

    const validateForm_membershipStatus = (e) => {
        var isValid = !_formDisplay.showMembershipStatus || (e.target.value.toString() !== "-1");
        setIsValid({ ..._isValid, membershipStatus: isValid });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.firstName = _item.firstName != null && _item.firstName.trim().length > 0;
        _isValid.lastName = _item.lastName != null && _item.lastName.trim().length > 0;
        _isValid.email = _item.email != null && _item.email.trim().length > 0;
        _isValid.emailFormat = validate_Email(_item.email);
        _isValid.companyName = !_formDisplay.requireCompanyInfo || (_item.companyName != null && _item.companyName.trim().length > 0);
        _isValid.companyUrl = true; //!_formDisplay.requireCompanyInfo || (_item.companyUrl != null && _item.companyUrl.trim().length > 0);
        _isValid.description = _item.description != null && _item.description.trim().length > 0;
        _isValid.phone = _item.phone != null && _item.phone.trim().length > 0;//I'm not sure about our validation here is it just US numbers or int'l
        _isValid.membershipStatus = !_formDisplay.showMembershipStatus || (_item.membershipStatus != null && _item.membershipStatus.id.toString() !== "-1");
        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.firstName && _isValid.lastName && _isValid.email && _isValid.emailFormat &&
            _isValid.companyName && _isValid.companyUrl && _isValid.description && _isValid.phone && _isValid.membershipStatus);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onCancel = () => {
        //raised from header nav
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        history.goBack();
    };

    const onSave = () => {
        //raised from header nav
        console.log(generateLogMessageString('onSave', CLASS_NAME));

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
                        { id: new Date().getTime(), severity: "success", body: `Your inquiry was submitted. A CESMII representative will contact you regarding this inquiry.`, isTimed: false }
                    ]
                });

                scrollTopScreen();

                //now refresh page with empty form
                setForceReload(_forceReload + 1);
                //history.goBack();
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

    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "firstName":
            case "lastName":
            case "email":
            case "description":
            case "companyName":
            case "companyUrl":
            case "phone":
            case "industries":
                _item[e.target.id] = e.target.value;
                break;
            //drop down example
            case "membershipStatus":
                if (e.target.value.toString() === "-1") _item.membershipStatus = null;
                else {
                    _item.membershipStatus = { id: e.target.value, name: e.target.options[e.target.selectedIndex].text };
                }
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
    //(function () {
    //    'use strict'

    //    // Fetch all the forms we want to apply custom Bootstrap validation styles to
    //    var forms = document.querySelectorAll('.needs-validation')

    //    // Loop over them and prevent submission
    //    Array.prototype.slice.call(forms)
    //        .forEach(function (form) {
    //            form.addEventListener('submit', function (event) {
    //                if (!form.checkValidity()) {
    //                    alert("not valid");
    //                   // setFormIsValid(false);
    //                    event.preventDefault()
    //                    event.stopPropagation()
    //                }
    //                alert("valid");
    //                form.classList.add('was-validated')
    //               // setFormIsValid(true);

    //            }, false)
    //        })
    //})()

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderLeftRail = () => {
        if (!loadingProps.isLoading && (_referrerItem == null)) {
            return;
        }
        return (
            <div className="info-panel d-none d-sm-block">
                <div className="info-section py-3 px-1 rounded">
                    {id != null &&
                        renderSolutionDetails()
                    }
                    {publisherId != null &&
                        renderPublisherDetails()
                    }
                </div>
            </div>
        );
    }

    const renderHeaderBlock = () => {
        return (
            <h1 className="m-0 mr-2">
                {_formDisplay.caption}
                {_referrerItem != null &&
                    ` - ${_referrerItem.displayName}`
                }
            </h1>
        )
    }

    const renderSolutionDetails = () => {
        if (_referrerItem == null) return;
        return (
            <div className="px-2">
                <div className="row mb-3" >
                    <div className="col-sm-12">
                        <p className="mb-2 headline-3 p-1 px-2 w-100 d-block rounded">
                            {_referrerItem.displayName}
                        </p>
                        <div className="px-2 mb-2" dangerouslySetInnerHTML={{ __html: _referrerItem.abstract }} ></div>
                        {_referrerItem.publishDate != null &&
                            <p className="px-2 mb-0">Published: {formatItemPublishDate(_referrerItem)}</p>
                        }
                    </div>
                </div>
                {itemType !== "profile" && 
                    <PublisherSidebar item={_referrerItem.publisher} />
                }
            </div>
        )
    }

    const renderPublisherDetails = () => {
        if (_referrerItem == null) return;
        return (
            <PublisherSidebar item={_referrerItem} />
        )
    }

    const renderAdditionalResources = () => {
        return (
            <div className="info-panel">
                <div className="info-section mb-3 pb-3 px-1 rounded">
                    <div className="px-2">
                        <div className="row mb-3" >
                            <div className="col-sm-12">
                                <p className="mb-2 headline-3 p-1 px-2 w-100 d-block rounded">
                                    Additional Resources
                                </p>
                                <ul className="sidebar-list">
                                    <li className="mb-2 px-2">
                                        <a href="https://www.cesmii.org/" target="_blank" rel="noreferrer" >About CESMII</a>
                                    </li>
                                    <li className="mb-2 px-2">
                                        <a href="https://www.cesmii.org/membership-information/" target="_blank" rel="noreferrer" >Membership Information</a>
                                    </li>
                                    <li className="mb-2 px-2">
                                        <a href="https://www.cesmii.org/what-is-smart-manufacturing-the-smart-manufacturing-definition/" target="_blank" rel="noreferrer" >About Smart Manufacturing</a>
                                    </li>
                                    <li className="mb-2 px-2">
                                        <a href="https://github.com/cesmii" target="_blank" rel="noreferrer" >CESMII Github</a>
                                    </li>
                                    <li className="mb-2 px-2">
                                        <SocialMedia items={LookupData.socialMediaLinks} />
                                    </li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    //
    const renderSubTitle = () => {
        return (
            <span onClick={onBack} className="px-2 btn btn-text-solo align-items-center ml-auto ml-sm-0 auto-width d-flex clickable hover" ><i className="material-icons">chevron_left</i>Back</span>
        );
    }

    const renderMembershipStatus = () => {
        if (!_formDisplay.showMembershipStatus) return;
        if (loadingProps.lookupDataStatic == null) return;

        //show drop down list for edit, copy mode
        var items = loadingProps.lookupDataStatic.filter((g) => {
            return g.lookupType.enumValue === AppSettings.LookupTypeEnum.MembershipStatus
        });
        const options = items.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label htmlFor="membershipStatus" >Membership Status</Form.Label>
                {!_isValid.membershipStatus &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="membershipStatus" as="select" className={(!_isValid.membershipStatus ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={_item.membershipStatus == null ? "-1" : _item.membershipStatus.id}
                    onBlur={validateForm_membershipStatus} onChange={onChange} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };


    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    //return final ui
    const _title = `${_formDisplay.caption} | ${AppSettings.Titles.Main}`;

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
                    {renderAdditionalResources()}
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
                        {_formDisplay.showMembershipStatus &&
                            <div className="row">
                                <div className="col-md-6">
                                    {renderMembershipStatus()}
                                </div>
                            </div>
                        }
                        {_formDisplay.requireCompanyInfo &&
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
                        }
                        {_formDisplay.showIndustry &&
                            <div className="row">
                                <div className="col-md-12">
                                <Form.Group>
                                    <Form.Label htmlFor="industries" >Industry(s)</Form.Label>
                                    <Form.Control id="industries" placeholder="List industries related to your inquiry."
                                        value={_item.industries} onChange={onChange} />
                                </Form.Group>
                                </div>
                            </div>
                        }
                        {_formDisplay.requireCompanyInfo &&
                            <div className="row">
                                <div className="col-md-12">
                                    <Form.Group>
                                        <Form.Label htmlFor="companyUrl" >Company Website</Form.Label>
                                        {!_isValid.companyUrl &&
                                            <span className="invalid-field-message inline">
                                                Required
                                            </span>
                                        }
                                        <Form.Control id="companyUrl" className={(!_isValid.companyUrl ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                            value={_item.companyUrl} onBlur={validateForm_companyUrl} onChange={onChange} />
                                    </Form.Group>
                                </div>
                            </div>
                        }
                        <div className="row">
                            <div className="col-md-12">
                                <Form.Group>
                                    <Form.Label htmlFor="description" >{_formDisplay.captionDescription}</Form.Label>
                                    {!_isValid.description &&
                                        <span className="invalid-field-message inline">
                                            Required
                                        </span>
                                    }
                                    <Form.Control id="description" as="textarea" style={{ height: '100px' }}
                                        className={(!_isValid.description ? 'invalid-field minimal pr-5' : 'minimal pr-5')} 
                                        value={_item.description} onBlur={validateForm_description} onChange={onChange} />
                                </Form.Group>
                            </div>
                        </div>
                        <hr className="my-3" />
                        <div className="row">
                            <div className="col-md-12">
                                <Button variant="primary" type="button" className="ml-2" onClick={onSave} >Submit</Button>
                                <Button variant="text-solo" className="ml-1" onClick={onCancel} >Cancel</Button>
                            </div>
                        </div>
                    </Form>
                </div>
            </div>
        </>
    )
}

export default RequestInfo;
