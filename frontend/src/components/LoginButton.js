import React, { useState } from "react";
import Button from 'react-bootstrap/Button'
import { useMsal } from "@azure/msal-react";

import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { doCreateAccount, doLoginPopup, doLoginRedirect, useLoginStatus } from "./OnLoginHandler";

const CLASS_NAME = "LoginButton";

function LoginButton() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance, inProgress } = useMsal();
    const { isAuthenticated } = useLoginStatus(null, null /*[AppSettings.AADUserRole]*/);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_toggleSignUpSlideout, setToggleSignUpSlideout ] = useState(false);
    const _mode = "redirect";

    //-------------------------------------------------------------------
    // Region: API call
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onLoginClick = (e) => {
        console.log(generateLogMessageString('onLoginClick', CLASS_NAME));
        if (_mode === "popup") doLoginPopup(instance, inProgress, setLoadingProps);
        else doLoginRedirect(instance, inProgress);
    }

    const onToggleSlideout = (e) => {
        console.log(generateLogMessageString('onSlideOutToggle', CLASS_NAME));
        setToggleSignUpSlideout(!_toggleSignUpSlideout);
    }

    const onCloseSlideout = (e) => {
        console.log(generateLogMessageString('onCloseSlideout', CLASS_NAME));
        setToggleSignUpSlideout(false);
    }

    const onCreateAccountClick = (e) => {
        console.log(generateLogMessageString('onCreateAccountClick', CLASS_NAME));
        doCreateAccount(instance, inProgress);
    }

    //-------------------------------------------------------------------
    // Region: render helpers
    //-------------------------------------------------------------------
    const renderSignUpSlideOut = () => {

        return (
            <div className={`p-3 login slide-in-panel ${_toggleSignUpSlideout ? "open" : ""}`} >
                <div className="d-flex" >
                    <Button variant="icon-solo" onClick={onCloseSlideout} className="ml-auto" >
                        <span>
                            <i className="material-icons mr-1">close</i>
                        </span>
                    </Button>
                </div>
                <p className="font-weight-bold text-center mb-3">Don't have an account?</p>
                <div className="d-flex mt-auto mx-auto">
                    <Button variant="primary" className="mx-auto ml-2 border" type="submit" onClick={onCreateAccountClick} disabled={loadingProps.isLoading ? "disabled" : ""} >
                        Sign Up
                    </Button>
                </div>
                <p className="mt-3 mb-0 text-center" >
                    or email us at <a href="mailto:devops@cesmii.org" >devops@cesmii.org</a> to get registered.
                    Please provide your project name or SOPO number with your request.
                </p>
            </div>
        );
    }

    //-------------------------------------------------------------------
    // Region: final render
    //-------------------------------------------------------------------
    if (isAuthenticated) return null;

    return (
        <>
            <Button variant="link" className="d-inline-flex d-md-block link py-1 px-2 pr-3 nav-link" type="button" onClick={onToggleSlideout} >
                Sign Up
            </Button>
            <Button variant="primary" className="d-inline-flex d-md-block mx-auto mt-2 mt-md-0 border" type="button" onClick={onLoginClick} disabled={loadingProps.isLoading ? "disabled" : ""} >
                Login
            </Button>
            {renderSignUpSlideOut() }
        </>
    );
}

export default LoginButton;
