import React from 'react'

import axiosInstance from '../../services/AxiosService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString, renderMenuColorMaterialIcon } from '../../utils/UtilityService';
import color from '../../components/Constants';

const CLASS_NAME = "MarketplaceItemJobLauncher";

// Component that handles scenario when f5 / refresh happens
// We want to turn off processing flag in that scenario as protection against
// scenario where exception occurs and isLoading remains true.
// renders nothing, just attaches side effects
export const MarketplaceItemJobLauncher = (props) => {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - check if user triggers activation
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: event handlers
    //-------------------------------------------------------------------
    // Load lookup data upon certain triggers in the background
    const onExecuteJob = () => {

        var url = `job/execute`;
        console.log(generateLogMessageString(`onExecuteJob||${url}`, CLASS_NAME));

        setLoadingProps({
            isLoading: true, message: `Initiating ${props.jobName}...This may take a few minutes.`
        });

        var data = {
            marketplaceItemId: props.marketplaceItemId,
            jobDefinitionId: props.jobDefinitionId,
            payload: props.payload
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

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    // renders nothing if not logged in
    if (!props.isAuthenticated) return null;

    return (
        <>
            <button className={`btn btn-link d-flex align-items-center ${props.className}`} onClick={onExecuteJob}>
                {renderMenuColorMaterialIcon(props.iconName == null || props.iconName === '' ? 'system_update' : props.iconName, color.cornflower, 'me-1')}
                {props.jobName}
            </button>
        </>
    )

};