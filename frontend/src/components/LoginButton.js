import React, { useState } from "react";
import Button from 'react-bootstrap/Button'

import {InteractionRequiredAuthError,InteractionStatus} from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { AppSettings } from "../utils/appsettings";

const CLASS_NAME = "LoginButton";

function LoginButton() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance, inProgress, accounts } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_error, setIsError] = useState({ success: true, message: 'An error occurred. Please try again.' });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onLoginClick = (e) => {
        console.log(generateLogMessageString('onLoginClick', CLASS_NAME));

        e.preventDefault(); //prevent form.submit action

        //show a spinner
        setIsError({ ..._error, success: true });
        setLoadingProps({ isLoading: true, message: null });

        if (inProgress === InteractionStatus.None) {

            var loginRequest = {
                scopes: AppSettings.MsalScopes, 
                account: accounts[0],
            };

            instance.loginPopup(loginRequest)
                //.acquireTokenSilent(loginRequest)
                .then((response) => {
                    //show a spinner
                    setIsError({ ..._error, success: true });
                    setLoadingProps({ isLoading: true, message: null });

                    //set the active account
                    instance.setActiveAccount(response.account);

                    //console.info(generateLogMessageString(`onLoginClick||loginPopup||token||${accessToken}`, CLASS_NAME));

                    //trigger additional actions to pull back data from mktplace once logged in
                    setLoadingProps({ refreshMarketplaceCount: true, hasSidebar: true, refreshLookupData: true, refreshFavorites: true, refreshSearchCriteria: true });
                    setLoadingProps({ isLoading: false, message: null });
                })
                .catch((error) => {
                    if (error instanceof InteractionRequiredAuthError) {
                        instance.acquireTokenRedirect(loginRequest);
                    }
                    console.error(generateLogMessageString(`onLoginClick||loginPopup||${error}`, CLASS_NAME));
                });
        }
    }

    //if already logged in, don't show button
    if (_isAuthenticated) {
        return null;
    }

    return (
        <>
            <Button variant="primary" className="mx-auto ml-2 border" type="submit" onClick={onLoginClick} disabled={loadingProps.isLoading ? "disabled" : ""} >
                Login
            </Button>
        </>
    );
}

export default LoginButton;
