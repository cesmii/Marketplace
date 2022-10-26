import { useEffect } from "react";
import { Redirect } from "react-router-dom";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { BrowserAuthError, InteractionRequiredAuthError, InteractionStatus } from "@azure/msal-browser";

import axiosInstance from "../services/AxiosService";
import { generateLogMessageString, isInRole, isInRoles } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { AppSettings } from "../utils/appsettings";

const CLASS_NAME = "OnLoginHandler";


//-------------------------------------------------------------------
// useLoginStatus - hook to get current login status during routing
//-------------------------------------------------------------------
export const useLoginStatus = (location = null, roles = null) => {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();
    //Check for is authenticated. Check individual permissions if needed.
    const isAuthenticated = _isAuthenticated && _activeAccount != null;
    const isAuthorized = isAuthenticated && (roles == null || isInRoles(_activeAccount, roles));

    //track a redirect url for post login
    //scenario 1: not authenticated and attempting to go to some other path than home, append the redirect url post login to that url
    //scenario 2: not authenticated and attempting to go to home page
    //scenario 3: authenticated but not permitted to this app or this area. Send them to notpermitted screen.
    let redirectUrl = '/';
    if (!isAuthenticated) {
        redirectUrl = location?.pathname && location?.pathname !== '/' ?
            `/admin/returnUrl=${encodeURIComponent(location?.pathname)}` : '/admin';
    }
    else if (!isAuthorized) {
        redirectUrl = '/notpermitted';
    }

    const result = { isAuthenticated: isAuthenticated, isAuthorized: isAuthorized, redirectUrl: redirectUrl };
    //console.log(generateLogMessageString(`useLoginStatus||loginPopup||${JSON.stringify(result)}`, CLASS_NAME));

    //-------------------------------------------------------------------
    // Region: return values
    //-------------------------------------------------------------------
    return result;
}


//-------------------------------------------------------------------
// onAfterAADLogin: after login, let API know and wire up some stuff for downstream
//-------------------------------------------------------------------
export const onAADLogin = (setLoadingProps) => {
    console.log(generateLogMessageString('onAADLogin', CLASS_NAME));

    //perform insert call
    const url = `auth/onAADLogin`;
    axiosInstance.post(url)
        .then(result => {
            if (result.data.isSuccess) {
                console.log(generateLogMessageString(`onAADLogin||${result.data.message}`, CLASS_NAME));
                setLoadingProps({ loginStatusCode: 200 });
                //if (callbackFn) callbackFn(200);
            }
            else {
                console.warn(generateLogMessageString(`onAADLogin||Initialize Failed||${result.data.message}`, CLASS_NAME));
                setLoadingProps({ loginStatusCode: 399 });
                //if (callbackFn) callbackFn(399);
            }
        })
        .catch(error => {
            //403, 401 - if you are not permitted to call this endpoint, you should not be able to get into the site.
            //you may have a CESMII AD account but that is different than having role permission here.  
            if (error?.response?.status === 401) {
                setLoadingProps({ loginStatusCode: 401 });
                //if (callbackFn) callbackFn(401);
            }
            //403, 401
            else if (error?.response?.status === 403) {
                setLoadingProps({ loginStatusCode: 403 });
                //if (callbackFn) callbackFn(403);
            }
            else {
                setLoadingProps({ loginStatusCode: 500 });
                //if (callbackFn) callbackFn(500);
                console.error(generateLogMessageString('onAADLogin||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
            }
        });
};

//-------------------------------------------------------------------
// run the post login popup code
//-------------------------------------------------------------------
//call API to setup user on API side and clear the way to get lookup data.
export const onAADLoginComplete = (instance, history, setLoadingProps, statusCode) => {
    switch (statusCode) {
        case 200:
            setLoadingProps({ refreshLookupData: true, refreshSearchCriteria: true, refreshFavorites: true });
            history.push('/');
            break;
        case 401:
            console.error(generateLogMessageString(`onAADLoginComplete||statusCode||${statusCode}`, CLASS_NAME));
            //history.push('/notauthorized');
            doLogout(history, instance, '/notauthorized');
            break;
        case 403:
            console.error(generateLogMessageString(`onAADLoginComplete||statusCode||${statusCode}`, CLASS_NAME));
            //history.push('/notpermitted');
            doLogout(history, instance, '/notpermitted', true, true);
            break;
        case 399:
        case 400:
        case 500:
        default:
            setLoadingProps({
                isLoading: false, message: null, modalMessages:
                    [{ id: new Date().getTime(), severity: "danger", body: 'An error occurred processing your login request. Please try again.', isTimed: false }]
            });
            history.push('/admin');
            break;
    }
};

//-------------------------------------------------------------------
// handleLoginError: based on type of error, handle login error
//-------------------------------------------------------------------
export const handleLoginError = (error, setLoadingProps) => {
    var msg = 'An error occurred attempting to launch the login window. Please contact the system administrator.';
    if (error instanceof InteractionRequiredAuthError) {
        console.error(generateLogMessageString(`handleLoginError||${error.errorCode}||${error}`, CLASS_NAME));
    }
    else if (error instanceof BrowserAuthError) {
        switch (error.errorCode) {
            case 'user_cancelled':
                console.warn(generateLogMessageString(`handleLoginError||${error.errorCode}||${error}`, CLASS_NAME));
                //no need to inform user they cancelled
                msg = null;
                break;
            case 'popup_window_error':
                console.warn(generateLogMessageString(`handleLoginError||${error.errorCode}||${error}`, CLASS_NAME));
                msg = "The Login UI uses a popup window. Please disable Popup blocker for this site.";
                break;
            case 'monitor_window_timeout':
                console.error(generateLogMessageString(`handleLoginError||${error.errorCode}||${error}`, CLASS_NAME));
                return; //don't show message on screen
            default:
                console.error(generateLogMessageString(`handleLoginError||${error.errorCode}||${error}`, CLASS_NAME));
                msg = error.message;
        }
    }
    else if (error.errorCode === "invalid_client") {
        console.error(generateLogMessageString(`handleLoginError||${error.errorCode}||${error}`, CLASS_NAME));
        msg = 'Contact System Administrator. A system error has occurred unrelated to your account. The login configuration settings are invalid.';
    }
    setLoadingProps({
        isLoading: false, message: null, modalMessages: msg == null ? [] :
            [{ id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
    });
}

//-------------------------------------------------------------------
// Region: MSAL Login Actions - login popup
//-------------------------------------------------------------------
//Login w/ popup - component
export const doLoginPopup = async (instance, inProgress, accounts, setLoadingProps) => {

    //show a spinner
    setLoadingProps({ isLoading: true, message: null });

    if (inProgress === InteractionStatus.None || inProgress === InteractionStatus.Startup) {

        const loginRequest = {
            scopes: AppSettings.MsalScopes,
            account: accounts[0],
            prompt: 'select_account'  //always present the account selection - even if already logged in cached.
        };

        // redirect anonymous user to login popup
        instance.loginPopup(loginRequest)
            //.acquireTokenSilent(loginRequest)
            .then((response) => {
                //check for basic role membership. You may have an AAD account but may not 
                //be granted permissions to this app
                if (!isInRole(response.account, AppSettings.AADUserRole)) {
                    setLoadingProps({
                        isLoading: false, modalMessages:
                            [{ id: new Date().getTime(), severity: "danger", body: 'Your account is not permitted to access this area. Email us at devops@cesmii.org to get registered or request assistance.', isTimed: false }]
                    });
                    forceLogout(instance);
                    return;
                }
                //set the active account
                instance.setActiveAccount(response.account);

                onAADLogin(setLoadingProps);

                //trigger additional actions to pull back data from mktplace once logged in
                setLoadingProps({ isLoading: false, message: null });
            })
            .catch((error) => {
                if (error instanceof InteractionRequiredAuthError) {
                    instance.acquireTokenRedirect(loginRequest);
                    console.error(generateLogMessageString(`onLoginClick||loginPopup||${error}`, CLASS_NAME));
                }
                else {
                    handleLoginError(error, setLoadingProps);
                }
            });
    }
}

//-------------------------------------------------------------------
// UseLoginSilent - attempt a silent login
//-------------------------------------------------------------------
export const useLoginSilent = () => {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //const history = useHistory();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { instance, inProgress, accounts } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const _isAuthenticated = useIsAuthenticated() && _activeAccount != null;

    //console.log(generateLogMessageString(`Initialize||Active Account: ${_activeAccount?.userName} ||Authenticated: ${_isAuthenticated}`, CLASS_NAME));

    //-------------------------------------------------------------------
    // Region: Hooks - if not logged in, try silently
    //-------------------------------------------------------------------
    useEffect(() => {
        //Login silently
        async function doLoginSilent() {

            if (inProgress !== InteractionStatus.None && inProgress !== InteractionStatus.Startup) return;

            const msgId = new Date().getTime();
            //setLoadingProps({
            //    isLoading: false, message: null, inlineMessages: [
            //        { id: msgId, severity: "info", body: 'Single Sign On. Attempting single sign on...', isTimed: true }]
            //});

            const loginRequest = {
                scopes: AppSettings.MsalScopes,
                account: accounts[0],
                prompt: 'select_account',  //always present the account selection - even if already logged in cached.
                redirectUri: `/loginsuccess${loadingProps.returnUrl ? '?returnUrl=' + loadingProps.returnUrl : ''}`
            };

            try {
                const response = await instance.ssoSilent(loginRequest);

                //check for basic role membership. You may have an AAD account but may not 
                //be granted permissions to this app
                if (!isInRole(response.account, AppSettings.AADUserRole)) {
                    forceLogout(instance);
                    return;
                }

                //set the active account
                instance.setActiveAccount(response.account);

                onAADLogin(setLoadingProps);

                console.info(generateLogMessageString(`doLoginSilent||SSO...Automatically signing in...`, CLASS_NAME));
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: msgId, severity: "success", body: 'Single Sign On. Success...', isTimed: true }]
                });

            } catch (error) {
                if (error instanceof InteractionRequiredAuthError) {
                    //we will have the login button and user can initiate loginPopup that way
                    //doLoginPopup();
                    console.warn(generateLogMessageString(`doLoginSilent||Auth Required||${error}`, CLASS_NAME));
                } else {
                    // handle error
                    handleLoginError(error, setLoadingProps);
                }
            }
        }

        //if on the /login page, then don't attempt login silent.
        if (window.location.href.indexOf('/admin') > -1) return;

        //if not logged in, try silently
        if (!_activeAccount || !_isAuthenticated) {
            console.info(generateLogMessageString(`useEffect||doLoginSilent`, CLASS_NAME));
            doLoginSilent();
        }
    }, [_activeAccount, _isAuthenticated]);

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    // renders nothing, since nothing is needed
    return null;
}

export const useOnLoginComplete = () => {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { isAuthenticated, isAuthorized } = useLoginStatus(null, [AppSettings.AADUserRole]);

    //-------------------------------------------------------------------
    // Region: Hooks - if logged in, determine result code and then navigate appropriately
    //-------------------------------------------------------------------
    useEffect(() => {

        //-------------------------------------------------------------------
        // run the post login popup code
        //-------------------------------------------------------------------
        //only run the loginPopup once the value is populated during onAADLogin
        if (!loadingProps.loginStatusCode) return;

        const statusCode = !isAuthenticated ? 401 :
            !isAuthorized ? 403 : loadingProps.loginStatusCode;

        switch (statusCode) {
            case 200:
                setLoadingProps({ loginStatusCode: null, refreshLookupData: true, refreshSearchCriteria: true, refreshFavorites: true });
                //history.push('/');
                break;
            case 401:
                setLoadingProps({ loginStatusCode: null });
                console.warn(generateLogMessageString(`onAADLoginComplete||statusCode||${statusCode}`, CLASS_NAME));
                //history.push('/notauthorized');
                //doLogout(history, instance, '/notauthorized');
                break;
            case 403:
                setLoadingProps({ loginStatusCode: null });
                console.warn(generateLogMessageString(`onAADLoginComplete||statusCode||${statusCode}`, CLASS_NAME));
                //history.push('/notpermitted');
                //doLogout(history, instance, '/notpermitted', true, false);
                break;
            case 399:
            case 400:
            case 500:
            default:
                console.error(generateLogMessageString(`onAADLoginComplete||statusCode||${statusCode}`, CLASS_NAME));
                setLoadingProps({
                    loginStatusCode: null, isLoading: false, message: null, modalMessages:
                        [{ id: new Date().getTime(), severity: "danger", body: 'An error occurred processing your login request. Please try again.', isTimed: false }]
                });
                //history.push('/admin');
                break;
        }

    }, [loadingProps.loginStatusCode, isAuthenticated, isAuthorized]);

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    // renders nothing, since nothing is needed
    //return null;

    if (!loadingProps.loginStatusCode) return null;

    const statusCode = !isAuthenticated ? 401 :
        !isAuthorized ? 403 : loadingProps.loginStatusCode;

    switch (statusCode) {
        case 200:
            return (<Redirect to={'/'} />);
        //history.push('/');
        //break;
        case 401:
            return (<Redirect to={'/notauthorized'} />);
        //history.push('/notauthorized');
        //doLogout(history, instance, '/notauthorized');
        //break;
        case 403:
            return (<Redirect to={'/notpermitted'} />);
        //history.push('/notpermitted');
        //doLogout(history, instance, '/notpermitted', true, false);
        //break;
        case 399:
        case 400:
        case 500:
        default:
            return (<Redirect to={'/admin'} />);
        //history.push('/admin');
        //break;
    }

}

//-------------------------------------------------------------------
// Region: doLogout
//-------------------------------------------------------------------
export const doLogout = (history, instance, redirectUrl = `/admin`, silent = true, logoutLocalOnly = true) => {
    //scenario 1 - logout of everything
    if (!logoutLocalOnly) {
        instance.logoutPopup({
            onRedirectNavigate: (url) => {
                //return false if you would like to stop after local logout - ie don't logout of server instance
                if (redirectUrl != null) {
                    history.push(redirectUrl);
                }
                return false;
            }
        });
    }
    //scenario 2 - logout of only this app
    else {
        instance.logoutRedirect({
            onRedirectNavigate: (url) => {
                //return false if you would like to stop after local logout - ie don't logout of server instance
                if (redirectUrl != null) {
                    history.push(redirectUrl);
                }
                //control 
                if (silent) return false;
            }
        });
    }
}

//-------------------------------------------------------------------
// Region: doLogout
//-------------------------------------------------------------------
export const forceLogout = (instance) => {
    instance.logoutRedirect({
        onRedirectNavigate: (url) => {
            return false;
        }
    });
}
