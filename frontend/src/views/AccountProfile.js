import React, { useState, useEffect } from 'react'
import { useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import { useMsal } from "@azure/msal-react";
import { axiosInstance } from "../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

import { useLoadingContext } from "../components/contexts/LoadingContext";
import { AppSettings } from '../utils/appsettings';
import { generateLogMessageString } from '../utils/UtilityService'

import ChangePasswordModal from '../components/ChangePasswordModal';

const CLASS_NAME = "AccountProfile";

function AccountProfile() {

    //TBD - Remove the remaining fields
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState({});
    const [_isValid, setIsValid] = useState({
        userName: true, smipSettings: true
    });
    const [_changePasswordModal, setChangePasswordModal] = useState({ show: false, url: null, updateToken: false});
    var caption = 'My Profile';

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            //get my latest profile info
            var result = null;
            try {
                var url = `user/profile/mine/msal`
                result = await axiosInstance.post(url);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this user.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This user was not found.';
                    history.push('/404');
                }
                //403 error - user may be allowed to log in but not permitted to perform the API call they are attempting
                else if (err != null && err.response != null && err.response.status === 403) {
                    console.log(generateLogMessageString('useEffect||fetchData||Permissions error - 403', CLASS_NAME, 'error'));
                    msg += ' You are not permitted to edit account profile.';
                    history.goBack();
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            //set item state value
            setItem(result.data);
            setLoadingProps({ isLoading: false, message: null });
        }

        fetchData();

    }, []);


    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.smipSettings = validateFormSmipSettings();

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (true);
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
        console.log(generateLogMessageString('onSave', CLASS_NAME));

        //do validation
        if (!validateForm()) {
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform insert call
        var url = `user/profile/update`;
        axiosInstance.post(url, _item)
            .then(result => {
                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: `Account profile was saved`, isTimed: true
                            }
                        ]
                    });
                }
                else {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: `An error occurred saving account profile.`, isTimed: true }
                        ]
                    });
                    console.error(generateLogMessageString(`onSave||Error||${result.data.message}.`, CLASS_NAME));
                }
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred saving account profile.`, isTimed: false }
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

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        //switch (e.target.id) {
            //case "userName":
            //case "firstName":
            //case "lastName":
            //case "email":
            //    _item[e.target.id] = e.target.value;
            //    _item.fullName = `${_item.firstName} ${_item.lastName}`;
            //    break;
        //    default:
        //        return;
        //}
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

    const onChangePasswordOpen = (e) => {
        console.log(generateLogMessageString(`onChangePasswordOpen`, CLASS_NAME));
        var url = e.currentTarget.getAttribute("data-url");
        var updateToken = e.currentTarget.getAttribute("data-updatetoken");
        setChangePasswordModal({ show: true, url: url, updateToken: updateToken === "true" });
    };

    const onChangePasswordClose = () => {
        console.log(generateLogMessageString(`onChangePasswordClose`, CLASS_NAME));
        setChangePasswordModal({ show: false, url: null, updateToken: false });
    };


    //-------------------------------------------------------------------
    // Region: SMIP Settings - Keep together so we can easily remove when needed
    //-------------------------------------------------------------------
    const [_isValidSmipSettings, setIsValidSmipSettings] = useState({
        userName: true, graphQlUrl: true, authenticator: true, authenticatorRole: true
    });

    const validateFormSmipSettings_userName = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValidSmipSettings({ ..._isValidSmipSettings, userName: isValid });
    };

    const validateFormSmipSettings_graphQlUrl = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValidSmipSettings({ ..._isValidSmipSettings, graphQlUrl: isValid });
    };

    const validateFormSmipSettings_authenticator = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValidSmipSettings({ ..._isValidSmipSettings, authenticator: isValid });
    };

    const validateFormSmipSettings_authenticatorRole = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValidSmipSettings({ ..._isValidSmipSettings, authenticatorRole: isValid });
    };

    const validateFormSmipSettings = () => {
        console.log(generateLogMessageString(`validateFormSmipSettings`, CLASS_NAME));

        _isValidSmipSettings.userName = _item.smipSettings.userName != null && _item.smipSettings.userName.trim().length > 0;
        _isValidSmipSettings.graphQlUrl = _item.smipSettings.graphQlUrl != null && _item.smipSettings.graphQlUrl.trim().length > 0;
        _isValidSmipSettings.authenticator = _item.smipSettings.authenticator != null && _item.smipSettings.authenticator.trim().length > 0;
        _isValidSmipSettings.authenticatorRole = _item.smipSettings.authenticatorRole != null && _item.smipSettings.authenticatorRole.trim().length > 0;

        setIsValidSmipSettings(JSON.parse(JSON.stringify(_isValidSmipSettings)));
        return (_isValidSmipSettings.userName && _isValidSmipSettings.graphQlUrl && _isValidSmipSettings.authenticator
            && _isValidSmipSettings.authenticatorRole );
    }

    //on change handler to update state
    const onChangeSmipSettings = (e) => {

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "smipSettings.userName":
            case "smipSettings.password":
            case "smipSettings.graphQlUrl":
            case "smipSettings.authenticator":
            case "smipSettings.authenticatorRole":
                _item.smipSettings[e.target.id.replace("smipSettings.", "")] = e.target.value;
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

    const renderSMIPSettings = () => {
        return (
            <>
                <div className="row">
                    <div className="col-md-12">
                        <h2>SMIP Settings</h2>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="smipSettings.userName" >User Name</Form.Label>
                            {!_isValidSmipSettings.userName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="smipSettings.userName" className={(!_isValidSmipSettings.userName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.smipSettings?.userName == null ? '' : _item.smipSettings.userName} onBlur={validateFormSmipSettings_userName} onChange={onChangeSmipSettings} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="smipSettings.graphQlUrl" >GraphQl Url</Form.Label>
                            {!_isValidSmipSettings.graphQlUrl &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="smipSettings.graphQlUrl" className={(!_isValidSmipSettings.graphQlUrl ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.smipSettings?.graphQlUrl == null ? '' : _item.smipSettings.graphQlUrl} onBlur={validateFormSmipSettings_graphQlUrl} onChange={onChangeSmipSettings}
                                readOnly={_item.smipSettings == null || _item.smipSettings.userName == null ? 'readonly' : ''} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="smipSettings.authenticator" >Authenticator</Form.Label>
                            {!_isValidSmipSettings.authenticator &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="smipSettings.authenticator" className={(!_isValidSmipSettings.authenticator ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.smipSettings?.authenticator == null ? '' : _item.smipSettings.authenticator} onBlur={validateFormSmipSettings_authenticator} onChange={onChangeSmipSettings}
                                readOnly={_item.smipSettings == null || _item.smipSettings.userName == null ? 'readonly' : ''} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="smipSettings.authenticatorRole" >Authenticator Role</Form.Label>
                            {!_isValidSmipSettings.authenticatorRole &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="smipSettings.authenticatorRole" className={(!_isValidSmipSettings.authenticatorRole ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.smipSettings?.authenticatorRole == null ? '' : _item.smipSettings.authenticatorRole} onBlur={validateFormSmipSettings_authenticatorRole} onChange={onChangeSmipSettings}
                                readOnly={_item.smipSettings == null || _item.smipSettings.userName == null ? 'readonly' : ''} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6 my-2">
                        <Button variant="secondary" onClick={onChangePasswordOpen} data-url="user/smipSettings/changepassword" data-updatetoken={false} >Update SMIP Password</Button>
                    </div>
                </div>
            </>
        )
    }

    //-------------------------------------------------------------------
    // END Region: SMIP Settings
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderButtons = () => {
        return (
            <>
                <Button variant="text-solo" className="ml-1" onClick={onCancel} >Cancel</Button>
                <Button variant="secondary" type="button" className="ml-2" onClick={onSave} >Save</Button>
            </>
        );
    }


    const renderForm = () => {
        return (
            <>
                <div className="row">
                    <div className="col-md-12">
                        <h2>Azure Account Info</h2>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="userName" >User Name</Form.Label>
                            <Form.Control id="userName" className={'minimal pr-5'}
                                value={_activeAccount?.username == null ? '' : _activeAccount?.username} readOnly='readonly' />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="userName" >Display Name</Form.Label>
                            <Form.Control id="userName" className={'minimal pr-5'}
                                value={_activeAccount?.name == null ? '' : _activeAccount?.name} readOnly='readonly' />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="userName" >Organization</Form.Label>
                            <Form.Control id="organization.name" className={(!_isValid.userName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.organization == null ? "": _item.organization.name} readOnly='readonly' />
                        </Form.Group>
                    </div>
                </div>
                <hr />
                {renderSMIPSettings()}
            </>
        )
    }

    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-4">
                <div className="mx-auto col-sm-9 d-flex align-items-center" >
                    {renderHeaderBlock()}
                </div>
            </div>
        );
    };

    const renderHeaderBlock = () => {

        return (
            <>
                <h1 className="m-0 mr-2">
                    {caption}
                </h1>
                <div className="ml-auto d-flex align-items-center" >
                    {renderButtons()}
                </div>
            </>
        )
    }


    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (loadingProps.isLoading) return null;

    //return final ui
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + caption}</title>
            </Helmet>
            <Form noValidate>
                {renderHeaderRow()}
                <div className="row" >
                    <div className="mx-auto col-sm-9 mb-4" >
                        {renderForm()}
                    </div>
                </div>
            </Form>
            <ChangePasswordModal userId={_item.id} onSave={onChangePasswordClose} onCancel={onChangePasswordClose}
                show={_changePasswordModal.show} urlSave={_changePasswordModal.url} updateToken={_changePasswordModal.updateToken} />
        </>
    )
}

export default AccountProfile;
