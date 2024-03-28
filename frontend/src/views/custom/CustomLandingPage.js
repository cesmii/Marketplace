import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString, scrollTopScreen, formatItemPublishDate } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import ApogeanStartTrial from './ApogeanStartTrial'
import '../styles/RequestInfo.scss';

const CLASS_NAME = "CustomLandingPage";

function CustomLandingPage() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    //can access this form for performing start trial of the Apogean ontime edge application
    const { name, jobName } = useParams();
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
    const [_caption, setCaption] = useState(null);

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
                const data = { id: jobName };
                const url = `job/getbyname`;
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
        if (jobName != null) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [jobName]);


    //get marketplace item record
    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));

            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                const data = { id: name, isTracking: true };
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
        if (name != null) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [name]);


    //get marketplace item record
    useEffect(() => {
        if (_marketplaceItem == null || _jobDef == null) return;
        setCaption(
            `${_marketplaceItem.displayName} - ${_jobDef.displayName}`);

        //this will execute on unmount
        return () => {
        };
    }, [_marketplaceItem, _jobDef]);

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onCancel = () => {
        //raised from header nav
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        history.goBack();
    };

    // Called from child component
    const onExecute = (payload) => {

        //raised from child component
        console.log(generateLogMessageString('onExecute', CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        const url = `job/execute`;
        console.log(generateLogMessageString(`onExecute||${url}`, CLASS_NAME));

        setLoadingProps({
            isLoading: true, message: `Initiating ${_jobDef.displayName}...This may take a few minutes.`
        });

        //payload specific for this job
        const data = {
            marketplaceItemId: _marketplaceItem.id,
            jobDefinitionId: _jobDef.id,
            payload: payload == null ? null : JSON.stringify(payload)
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
                    {name != null &&
                        renderSolutionDetails()
                    }
                </div>
            </div>
        );
    }

    const renderHeaderBlock = () => {
        return (
            <h1 className="m-0 mr-2">
                { _caption }
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

    //render a UI specific to the job loaded
    const renderJobSpecificUI = () => {

        if (_jobDef == null) return;

        switch (_jobDef.name.toLowerCase()) {
            case 'ontimeedge-free-trial':
                return (
                    <ApogeanStartTrial jobDef={_jobDef} marketplaceItem={_marketplaceItem} onExecute={onExecute} onCancel={onCancel} />
                    );
            default: return null;
        }
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
                    {renderHeaderBlock()}
                </div>
            </div>

            <div className="row" >
                <div className="col-sm-3 order-2 order-sm-1" >
                    {renderLeftRail()}
                </div>
                <div className="col-sm-9 mb-4 order-1 order-sm-2" >
                    <hr className="my-2" />
                    { renderJobSpecificUI() }
                </div>
            </div>
        </>
    )
}

export default CustomLandingPage;
