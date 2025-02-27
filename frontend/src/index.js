import React from 'react';
import ReactDOM from 'react-dom';
import ReactGA from 'react-ga4';

import { PublicClientApplication } from "@azure/msal-browser";
import { MsalProvider } from "@azure/msal-react";

import App from './App';
import { LoadingContextProvider } from "./components/contexts/LoadingContext";
import reportWebVitals from './reportWebVitals';
import { generateLogMessageString } from './utils/UtilityService';
import { AppSettings } from './utils/appsettings';

import './index.css';

// require('dotenv').config()

//#region - Analytics
//Analytics - only run the initialize once, only run in prod
if (AppSettings.TrackAnalytics === "true") {
    const _analyticsId = AppSettings.GoogleAnalyticsId;
    ReactGA.initialize(_analyticsId, {
        debug: true,
        testMode: false
    });
    console.log(generateLogMessageString(`Analytics||Init`, 'Index'));

    // This would be how you check that the calls are made correctly
    //console.log(ReactGA.testModeAPI.calls);
    //expect(ReactGA.testModeAPI.calls).toEqual([
    //    ['create', _analyticsId, 'auto']
    //]);
}
//#endregion

//create PublicClientApplication instance
//export it so that our non-component code can access this instance.
export const Msal_Instance = new PublicClientApplication(AppSettings.MsalConfig);
//#endregion

//var express = require('express');
//var server = express();
//var options = {
//    index: 'index.html'
//};
//server.use('/', express.static('/home/site/wwwroot', options));
//server.listen(process.env.PORT);

ReactDOM.render(
  <React.StrictMode>
    <MsalProvider instance={Msal_Instance}>
        <LoadingContextProvider>
            <App />
        </LoadingContextProvider>
    </MsalProvider>
  </React.StrictMode>,
  document.getElementById('root')
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();

