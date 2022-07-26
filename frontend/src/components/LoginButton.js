import React, { useState } from "react";
import Button from 'react-bootstrap/Button'

import {InteractionRequiredAuthError,InteractionStatus} from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { useAuthDispatch } from "../components/authentication/AuthContext";
import { login } from "../components/authentication/AuthActions";
import { AppSettings } from "../utils/appsettings";

const CLASS_NAME = "LoginButton";

function LoginButton() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance, inProgress, accounts } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const dispatch = useAuthDispatch() //get the dispatch method from the useDispatch custom hook
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

                    // Acquire token success
                    let accessToken = response.accessToken;
                    var loginData = {
                        isImpersonating: false,
                        token: accessToken,
                        user: {
                            email: response.account.username,
                            firstName: response.account.name,
                            fullName: response.account.name,
                            id: null,
                            isActive: true,
                            lastLogin: new Date(),
                            lastName: response.account.name,
                            organization: { name: 'CESMII', id: '62c5c4abd0a4cb3ef4c14de5' },
                            permissionIds: [],
                            permissionNames: [],
                            registrationComplete: new Date(),
                            smipSettings: null,
                            userName: response.account.username
                        }
                    };
                    let loginAction = login(dispatch, loginData);
                    if (!loginAction) {
                        console.error(generateLogMessageString(`onLoginClick||loginAction||an error occurred setting the login state.`, CLASS_NAME));
                    }
                    console.info(generateLogMessageString(`onLoginClick||loginPopup||token||${accessToken}`, CLASS_NAME));

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
            //setLoadingProps({ isLoading: false, message: null });
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
