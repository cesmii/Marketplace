import React, { useState, useEffect } from 'react'
import { Form } from 'react-bootstrap';
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'

import axiosInstance from "../services/AxiosService";
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { generateLogMessageString } from '../utils/UtilityService';

const CLASS_NAME = "ChangePasswordModal";

function ChangePasswordModal(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_show, setShow] = useState(false);
    const [_errorMsg, setErrorMessage] = useState(null);
    const { setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState({ id: props.userId, oldPassword: null, newPassword: null, confirmPassword: null});
    const [_isValid, setIsValid] = useState({ 
        oldPassword: true, newPassword: true, confirmPassword: true, matchPassword: true
    });

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        setShow(props.show);
        setIsValid({ oldPassword: true, newPassword: true, confirmPassword: true, matchPassword: true });

    }, [props.show]);

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_oldPassword = (e) => {
        var isValid = props.OldPasswordNotRequired || e.target.value != null && e.target.value.trim().length > 0;
        setIsValid({ ..._isValid, oldPassword: isValid });
    };

    const validateForm_newPassword = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValid({ ..._isValid, newPassword: isValid });
    };

    const validateForm_confirmPassword = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        var isValidMatch = e.target.value === _item.newPassword;
        setIsValid({ ..._isValid, confirmPassword: isValid, matchPassword: isValidMatch });
    };

    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.oldPassword = props.OldPasswordNotRequired || (_item.oldPassword != null && _item.oldPassword.trim().length > 0);
        _isValid.newPassword = _item.newPassword != null && _item.newPassword.trim().length > 0;
        _isValid.confirmPassword = _item.confirmPassword != null && _item.confirmPassword.trim().length > 0;
        _isValid.matchPassword = _item.newPassword === _item.confirmPassword;

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.oldPassword && _isValid.newPassword && _isValid.confirmPassword && _isValid.matchPassword);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //on change handler to update state
    const onChange = (e) => {

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "oldPassword":
            case "newPassword":
            case "confirmPassword":
                _item[e.target.id] = e.target.value;
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

    const onCancel = (e) => {
        setErrorMessage(null);
        setShow(false);
        if (props.onCancel != null) props.onCancel();
    };

    const onDismissMessage = (e) => {
        console.log(generateLogMessageString('onDismissMessage||', CLASS_NAME));
        setErrorMessage(null);
    }

    const onSave = (e) => {
        console.log(generateLogMessageString('onSave', CLASS_NAME));
        e.preventDefault();
        setErrorMessage(null);

        //do validation
        if (!validateForm()) {
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform insert call
        var url = props.urlSave;
        axiosInstance.post(url, _item)
            .then(result => {
                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: `Password was changed`, isTimed: true
                            }
                        ]
                    });

                    if (props.onSave != null) props.onSave();

                }
                else {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "danger", body: result.data.message, isTimed: false
                            }
                        ]
                    });
                    console.error(generateLogMessageString(`onSave||Error||${result.data.message}.`, CLASS_NAME));

                    if (props.onSave != null) props.onSave();
                }

                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: []
                });
                setErrorMessage(`An error occurred updating password.`);
                console.log(generateLogMessageString('onSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
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

    const renderForm = () => {
        return (
            <>
                <div className="row">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label htmlFor="oldPassword" >Old Password</Form.Label>
                            {!_isValid.oldPassword && !props.OldPasswordNotRequired &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="oldPassword" type="password" className={(!_isValid.oldPassword && !props.OldPasswordNotRequired ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.oldPassword == null ? '' : _item.oldPassword} onBlur={validateForm_oldPassword} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label htmlFor="newPassword" >New Password</Form.Label>
                            {!_isValid.newPassword &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="newPassword" type="password" className={(!_isValid.newPassword ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.newPassword == null ? '' : _item.newPassword} onBlur={validateForm_newPassword} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label htmlFor="confirmPassword" >Confirm Password</Form.Label>
                            {!_isValid.confirmPassword &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            {!_isValid.matchPassword &&
                                <span className="invalid-field-message inline">
                                    Password and confirm password do not match
                                </span>
                            }
                            <Form.Control id="confirmPassword" type="password" className={(!_isValid.confirmPassword || !_isValid.matchPassword ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.confirmPassword == null ? '' : _item.confirmPassword} onBlur={validateForm_confirmPassword} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
            </>
        )
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {/* Add animation=false to prevent React warning findDomNode is deprecated in StrictMode*/}
            <Modal animation={false} show={_show} onHide={onCancel} centered>
                <Modal.Header closeButton>
                    <Modal.Title>
                        <i className="material-icons me-1">settings</i>
                        Change Password
                    </Modal.Title>
                </Modal.Header>
                <Modal.Body className="my-3">
                    {renderErrorMessage()}
                    {renderForm()}
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="text-solo" onClick={onCancel}>Cancel</Button>
                    <Button variant="primary" onClick={onSave} >Save</Button>
                </Modal.Footer>
            </Modal>
        </>
    );

};

export default ChangePasswordModal;