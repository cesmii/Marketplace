import React, { useState, useEffect } from 'react'
import { useLoadingContext } from "./contexts/LoadingContext";
import axiosInstance from "../services/AxiosService";

import { generateLogMessageString } from "../utils/UtilityService";
import Button from 'react-bootstrap/Button'
import { LoadingIcon } from "./SVGIcon";
import { AppSettings } from '../utils/appsettings';

const CLASS_NAME = "JobMessage";

function JobMessage() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_dataRows, setDataRows] = useState(null);
    const [_forceReload, setForceReload] = useState(0); //increment this value to cause a re-get of the latest data.
    const [_processingCss, setProcessingCss] = useState(""); //used to create animation effect

    //-------------------------------------------------------------------
    // Region: hooks
    //  trigger from some other component to kick off an job log refresh and start tracking job status
    //-------------------------------------------------------------------
    useEffect(() => {

        if (loadingProps.activateJobLog === true) {
            console.log(generateLogMessageString('useEffect||activateJobLog||Trigger fetch', CLASS_NAME));
            setForceReload(_forceReload + 1);
            //once it is activated, it will then know when to stop itself.
            setLoadingProps({ activateJobLog: null });
        }

        //type passed so that any change to this triggers useEffect to be called again
        //_nodesetPreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [loadingProps.activateJobLog]);

    //-------------------------------------------------------------------
    // Region: hooks
    //  track progress of job by periodically querying the API for updated status
    //-------------------------------------------------------------------
    useEffect(() => {

        async function fetchJobLogData() {
            var data = { Query: null, Skip: 0, Take: 999999 };
            var url = `job/log/mine`;
            console.log(generateLogMessageString(`useEffect||fetchJobLogData||${url}`, CLASS_NAME));

            await axiosInstance.post(url, data).then(result => {
                if (result.status === 200) {

                    //find num not completed. If any not completed, kick off timer to re-check again in 6 seconds
                    var numIncomplete = result.data.data.filter((x) => { return x.completed == null; });
                    if (numIncomplete.length > 0) {
                        setTimeout(() => {
                            setForceReload(_forceReload + 1);
                        }, 4000);
                        console.log(generateLogMessageString(`useEffect||fetchJobLogData||${numIncomplete.length} jobs in progress.`, CLASS_NAME));
                        setProcessingCss("spin");
                    }
                    else {
                        console.log(generateLogMessageString(`useEffect||fetchJobLogData||All jobs complete`, CLASS_NAME));
                        setProcessingCss("");
                        //update lookup data and search criteria on complete.
                        setLoadingProps({
                            refreshProfileList: true,
                            refreshLookupData: true,
                            refreshSearchCriteria: true
                        });
                    }

                    setDataRows(result.data.data);

                    //compare a new job log list to previous list. If it has changed, then trigger a profile list refresh
                    //var jobLogs = numIncomplete.length === 0 ? [] : numIncomplete.map((x) => { return { id: x.id, status: x.status, message: x.message }; });
                    //keep completed bu failed around so other components can use this info for their needs, only get most recent msg
                    var logs = result.data.data
                        .map((x) => { return { id: x.id, status: x.status, message: x.messages != null && x.messages.length > 0 ? x.messages[0].message : null }; })
                        .filter((x) => { return x.completed == null || x.status !== AppSettings.JobLogStatus.Completed; });

                    var diff1 = loadingProps.jobLogs.filter(x => !logs.includes(x));
                    var diff2 = logs.filter(x => !loadingProps.jobLogs.includes(x));

                    //update list of in progress item ids. Set refresh trigger, update lookup data to get latest data types
                    setLoadingProps({
                        jobLogs: logs,
                        //refreshProfileList: (diff1.length > 0 || diff2.length > 0),
                        //refreshLookupData: (diff1.length > 0 || diff2.length > 0) ? true : loadingProps.refreshLookupData,
                        //refreshSearchCriteria: (diff1.length > 0 || diff2.length > 0) ? true : loadingProps.refreshSearchCriteria
                    });

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
        }

        if (_forceReload > 0) {
            fetchJobLogData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||fetchJobLogData||Cleanup', CLASS_NAME));
        };
    }, [_forceReload]);

    //-------------------------------------------------------------------
    // Region: Events
    //-------------------------------------------------------------------
    const dismissMessage = (msgId, warnOnNotFound = false) => {
        //if msg is in complete status, call API and delete (set inactive)
        var item = _dataRows.find(msg => { return msg.id.toString() === msgId.toString(); });
        if (item != null && item.completed != null) {
            deleteItem(item);
        }

        var x = _dataRows.findIndex(msg => { return msg.id.toString() === msgId.toString(); });
        //no item found
        if (x < 0 && warnOnNotFound) {
            console.warn(generateLogMessageString(`dismissMessage||no item found to dismiss with this id`, CLASS_NAME));
            return;
        }

        //delete the message locally
        _dataRows.splice(x, 1);

        //update state
        setDataRows(JSON.parse(JSON.stringify(_dataRows)));
    }

    const onDismiss = (e) => {
        console.log(generateLogMessageString('onDismiss||', CLASS_NAME));
        var id = e.currentTarget.getAttribute("data-id");
        dismissMessage(id);
    }

    const deleteItem = (item) => {
        console.log(generateLogMessageString(`deleteItem`, CLASS_NAME));

        //perform delete call
        var data = { id: item.id };
        var url = `job/log/delete`;
        axiosInstance.post(url, data)  
        .then(result => {

            if (result.data.isSuccess) {
            }
            else {
                console.log(generateLogMessageString(`deleteItem||error||${result.data.message}`, CLASS_NAME, 'error'));
            }
        })
        .catch(error => {
            //hide a spinner, show a message
            setLoadingProps({
                isLoading: false, message: null, inlineMessages: [
                    { id: new Date().getTime(), severity: "danger", body: `An error occurred dismissing this message.`, isTimed: true }
                ]
            });
            console.log(generateLogMessageString('deleteItem||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
            console.log(error);
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
    const getSeverity = (msg) => {
        switch (msg.status) {
            case AppSettings.JobLogStatus.Failed:
            case AppSettings.JobLogStatus.Cancelled:
                return "danger";
            case AppSettings.JobLogStatus.Completed:
                return "success";
            case AppSettings.JobLogStatus.InProgress:
            default:
                return "info-custom";
        }
    };

    const getMessage = (msg) => {
        //messages sorted by date descending
        var msgAppend = '';
        if (msg.messages != null && msg.messages.length > 0) {
            msgAppend = msg.messages[0].message;
        }
        else {
        }


        switch (msg.status) {
            case AppSettings.JobLogStatus.Failed:
                return `The job failed. ${msgAppend}`;
            case AppSettings.JobLogStatus.Cancelled:
                return `The job was cancelled. ${msgAppend}`;
            case AppSettings.JobLogStatus.Completed:
                return `The job completed. ${msgAppend}`;
            case AppSettings.JobLogStatus.InProgress:
            default:
                return `Processing... ${msgAppend}`;
        }
    };

    const renderMessage = (msg) => {
        //apply special handling for sev="processing"
        var isProcessing = msg.status === AppSettings.JobLogStatus.InProgress;
        var sev = getSeverity(msg);
        var caption = getMessage(msg);

        return (
            <div key={`inline-msg-${msg.id}`} className="row mb-1" >
                <div className={"col-sm-12 alert alert-" + sev + ""} >
                    {!isProcessing &&
                        <div className="dismiss-btn">
                            <Button id={`btn-inline-msg-dismiss-${msg.id}`} variant="icon-solo square small" data-id={msg.id} onClick={onDismiss} className="align-items-center" ><i className="material-icons">close</i></Button>
                        </div>
                    }
                    <div className="text-center" >
                        {isProcessing &&
                            <span className={`processing ${_processingCss}`} >
                                <LoadingIcon size="20" />
                            </span>
                        }
                        <span className={isProcessing ? 'ml-1' : ''} dangerouslySetInnerHTML={{ __html: caption }} />
                    </div>
                </div>
            </div >
        )    };

    //TBD - check for dup messages and don't show.
    const renderMessages = _dataRows?.map((msg) => {
        return (renderMessage(msg));
    });

    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    if (_dataRows == null || _dataRows.length === 0) return null;

    return (renderMessages);
}

export { JobMessage };
