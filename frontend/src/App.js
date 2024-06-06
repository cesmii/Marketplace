import { useEffect } from "react"
import { BrowserRouter as Router } from "react-router-dom"
import { Helmet } from "react-helmet"
import { ErrorBoundary } from 'react-error-boundary'
import axios from "axios"
import { axiosInstance } from "./services/AxiosService";

import { useLoadingContext } from "./components/contexts/LoadingContext";
import Navbar from './components/Navbar'
import { LoadingOverlay } from "./components/LoadingOverlay"
import MainContent from './components/MainContent'
import Footer from './components/Footer'
import { AppSettings } from './utils/appsettings'
import { generateLogMessageString } from './utils/UtilityService'
import ErrorPage from './components/ErrorPage'
import { OnLookupLoad } from './components/OnLookupLoad'
import { OnECommerceLoad } from "./components/OnECommerceLoad"
import { useRegisterMsalEventCallback } from "./components/OnLoginHandler";

import './App.scss';

const CLASS_NAME = "App";

function App() {
    //console.log(generateLogMessageString(`init || ENV || ${process.env.NODE_ENV}`, CLASS_NAME));
    //console.log(generateLogMessageString(`init || API || ${process.env.REACT_APP_BASE_API_URL}`, CLASS_NAME));

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    //  TBD - is this the best place for this? 
    //  If a network error occurs (ie API not there), catch it here and handle gracefully.  
    //-------------------------------------------------------------------
    const OnApiResponseError = (err) => {
        //401 - unauthorized - session expired - due to token expiration or unauthorized attempt
        const url = `${err?.config?.baseURL == null ? '' : err?.config?.baseURL}${err?.config?.url == null ? '' : err?.config?.url}`;
        if (err.response && err.response.status === 401) {
            console.warn(generateLogMessageString(`axiosInstance.interceptors.response||error||${err.response.status}||Url:${url}`, CLASS_NAME));
            setLoadingProps({ isLoading: false, message: null });
        }
        //403 error - user may be allowed to log in but not permitted to perform the API call they are attempting
        else if (err.response && err.response.status === 403) {
            console.warn(generateLogMessageString(`axiosInstance.interceptors.response||error||${err.response.status}||Url:${url}`, CLASS_NAME));
            setLoadingProps({
                isLoading: false, message: null, inlineMessages: [
                    { id: new Date().getTime(), severity: "danger", body: 'You are not permitted to access this area. Please contact your system administrator.', isTimed: true }]
            });
        }
        //400 error - Bad request - some condition causes code in endpoint to return this - like can't find the record 
        else if (err.response && err.response.status === 400) {
            const msg = err?.response?.data != null ? err?.response?.data : 'An error occurred. Please contact your system administrator.';
            console.warn(generateLogMessageString(`axiosInstance.interceptors.response||error||${err.response.status}||${msg}`, CLASS_NAME));
            setLoadingProps({
                isLoading: false, message: null, inlineMessages: [
                    { id: new Date().getTime(), severity: "danger", body: msg, isTimed: true }]
            });
        }
        //no status is our only indicator the API is not up and running
        else if (!err.status) {
            console.error(generateLogMessageString(`axiosInstance.interceptors.response||error||Url:${url}||${err}`, CLASS_NAME));
            if (err.message != null && err.message.toLowerCase().indexOf('request aborted') > -1) {
                //do nothing...
            }
            else {
                // API unavailable network error
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: 'A system error has occurred. Please contact your system administrator.', isTimed: true }]
                });
            }
        }

    };

    //-------------------------------------------------------------------
    // Region: hooks
    // Wrap the interceptor response in useEffect so that it doesn't go into endless loop 
    //  on each render that occurs as a result of the exception.
    // The reason for 2 interceptors is to handle scenario where we use axiosInstance (a wrapper class)
    //  and the scenario where a component may use axios directly.
    //-------------------------------------------------------------------
    useEffect(() => {
        //Catch exceptions in the flow when we use our axiosInstance
        axiosInstance.interceptors.response.use(
            response => {
                return response
            },
            err => {
                console.error(generateLogMessageString(`axiosInstance.interceptors.response.use||event`, CLASS_NAME));
                OnApiResponseError(err);
                return Promise.reject(err)
            }
        )

        //Catch exceptions in the flow when we use axios not as part of our axiosInstance
        axios.interceptors.response.use(
            response => {
                return response
            },
            err => {
                console.error(generateLogMessageString(`axios.interceptors.response.use||event`, CLASS_NAME));
                OnApiResponseError(err);
                return Promise.reject(err)
            }
        )
    }, [])

    //-------------------------------------------------------------------
    // Region: hooks
    // check if user is logged in. If not, attempt silent login
    // if that fails, then user will have to initiate login.
    //-------------------------------------------------------------------
    //useLoginSilent();

    //-------------------------------------------------------------------
    // Region: hooks - moved into separate component
    // useEffect - get various lookup data - onlookupLoad component houses the useEffect checks
    //-------------------------------------------------------------------
    OnLookupLoad();
    OnECommerceLoad();

    //-------------------------------------------------------------------
    // Region: add an event callback handler to capture loginredirect message responses
    //              only add the callback once.
    //-------------------------------------------------------------------
    useRegisterMsalEventCallback(setLoadingProps);

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    return (

        <div>
            <Helmet>
                <title>{AppSettings.Titles.Main}</title>
                <meta name="description" content={AppSettings.MetaDescription.Default} />
                <meta property="og:title" content={AppSettings.Titles.Main} />
                <meta property="og:description" content={AppSettings.MetaDescription.Default} />
                <link href="https://fonts.googleapis.com/css2?family=Material+Icons" rel="stylesheet"></link>
            </Helmet>
            <Router>
                <Navbar />
                <LoadingOverlay />
                <ErrorBoundary
                    FallbackComponent={ErrorPage}
                    onReset={() => {
                        // reset the state of your app so the error doesn't happen again
                    }}
                >
                    <div id="--cesmii-content-wrapper">
                        <MainContent />
                    </div>
                </ErrorBoundary>
                <Footer />
            </Router>
        </div>

    );
}

export default App;
