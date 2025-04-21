import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString, scrollTopScreen } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import MarketplaceEntitySidebar from '../shared/MarketplaceEntitySidebar';
//custom components
import ApogeanStartTrial from './ApogeanStartTrial'
//styles
import '../styles/MarketplaceEntity.scss';
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
    const [_jobLog, setJobLog] = useState(null);
    const [_caption, setCaption] = useState(null);
    const [_pollJobLog, setPollJobLog] = useState(0); //increment this value to cause a re-get of the latest job log.

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
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


    //-------------------------------------------------------------------
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


    //-------------------------------------------------------------------
    //update caption once we have the data loaded initially
    useEffect(() => {
        if (_marketplaceItem == null || _jobDef == null) return;
        setCaption(
            `${_marketplaceItem.displayName} - ${_jobDef.displayName}`);

        //this will execute on unmount
        return () => {
        };
    }, [_marketplaceItem, _jobDef]);


    //-------------------------------------------------------------------
    // Region: Monitor JobLog here. There is a common job log message component to handle this. However, 
    //  we also want to monitor job status here so we can direct child component 
    //  with latest latest job status
    // Query job log for specific id and get job status. Update state variable with job status
    //-------------------------------------------------------------------
    useEffect(() => {

        async function fetchData() {
            var data = { id: _jobLog?.id };
            var url = `job/log/getbyid`;
            console.log(generateLogMessageString(`useEffect||fetchJobLogData||${url}`, CLASS_NAME));

            await axiosInstance.post(url, data).then(result => {
                if (result.status === 200) {

                    //if not complete, initiate a polling mech to check until completed. 
                    if (!result.data.completed) {
                        setTimeout(() => {
                            setPollJobLog(_pollJobLog + 1);
                        }, 4000);
                        console.log(generateLogMessageString(`useEffect||fetchJobLog||polling ${_jobLog.id}||job in progress.`, CLASS_NAME));
                    }
                    else {
                        console.log(generateLogMessageString(`useEffect||fetchJobLog||polling ${_jobLog.id}||job complete`, CLASS_NAME));
                    }

                    setJobLog(result.data);

                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving job status.', isTimed: true }]
                    });
                }

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving job status.', isTimed: true }]
                    });
                }
            });
        } //end fetch

        //only call if polling has started and we have joblog id
        if (_pollJobLog > 0 && _jobLog?.id != null && _jobLog.id.length > 0) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
        };
    }, [_pollJobLog, _jobLog?.id]);

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
        //call execute job
        axiosInstance.post(url, data).then(result => {
            if (result.status === 200 && result.data.isSuccess) {
                //asynch flow - we kick off job and then a 2nd component polls to look for updated progress messages. 
                var jobLogs = loadingProps.jobLogs == null || loadingProps.jobLogs.length === 0 ? [] :
                    JSON.parse(JSON.stringify(loadingProps.jobLogs));
                const initialLogMsg = { id: result.data.data, status: AppSettings.JobLogStatus.InProgress, message: null };
                jobLogs.push(initialLogMsg);
                setLoadingProps({
                    isLoading: false, message: null,
                    jobLogs: jobLogs,
                    activateJobLog: true,
                    isImporting: false
                });
                setJobLog(initialLogMsg);
                setPollJobLog(_pollJobLog + 1);
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
    const renderRail = () => {
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

    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-3">
                <div className="col-sm-3" >
                    <div className="header-title-block d-flex align-items-center">
                        <span className="headline-1 d-none d-md-block">Library</span>
                        {renderSubTitle()}
                    </div>
                </div>
                <div className="col-sm-9 d-flex align-items-center" >
                    {renderHeaderBlock()}
                </div>
            </div>
        );
    };

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
                <div className="row mb-2" >
                    <div className="col-sm-12">
                        <p className="mb-2 headline-3 p-1 px-2 w-100 d-block rounded">
                            About {_marketplaceItem.displayName}
                        </p>
                        <div className="px-2 mb-2" dangerouslySetInnerHTML={{ __html: _marketplaceItem.abstract }} ></div>
                        <p><a href={`/library/${_marketplaceItem.name}`}>More Info</a></p>
                    </div>
                </div>
            </div>
        )
    }

    //
    const renderSubTitle = () => {
        return (
            <span onClick={onBack} className="px-2 btn btn-text-solo align-items-center ml-auto mr-2 auto-width d-flex clickable hover" ><i className="material-icons">chevron_left</i>Back</span>
        );
    }

    //render a UI specific to the job loaded
    const renderJobSpecificUI = () => {

        if (_jobDef == null) return;

        switch (_jobDef.name.toLowerCase()) {
            case 'ontimeedge-free-trial':
                return (
                    <ApogeanStartTrial jobDef={_jobDef} marketplaceItem={_marketplaceItem} jobLog={_jobLog} onExecute={onExecute} onCancel={onCancel} />
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
            <div className="row py-2 pb-2" >
                <div className="col-sm-3 d-flex align-items-center" >
                    {renderSubTitle()}
                </div>
                <div className="col-sm-9 d-flex align-items-center" >
                    {renderHeaderBlock()}
                </div>
            </div>

            <div className="row" >
                <div className="col-md-3 order-2 order-md-1" >
                    {_marketplaceItem != null &&
                        <MarketplaceEntitySidebar item={_marketplaceItem} className="light" />
                    }
                </div>
                <div className="col-md-9 mb-4 order-1 order-md-2" >
                    <div className="marketplace-entity">
                        {renderJobSpecificUI()}
                    </div>
                </div>
            </div>
        </>
    )
}

export default CustomLandingPage;
