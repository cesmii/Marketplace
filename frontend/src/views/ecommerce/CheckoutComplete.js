import React, { useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { Helmet } from "react-helmet"

import { AppSettings } from '../../utils/appsettings'
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { generateLogMessageString } from '../../utils/UtilityService';
import axiosInstance from '../../services/AxiosService';
import { useState } from 'react';
import { Button } from 'react-bootstrap';

const CLASS_NAME = "CheckoutComplete";

function CheckoutComplete() {

    //TBD
    //empty cart or mark cart with status.

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _caption = 'Checkout';
    const { checkoutSessionId } = useParams();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_checkoutSession, setCheckoutSession] = useState(null);
    const [_status, setStatus] = useState(null);
    const [_checkStatusCounter, setCheckStatusCounter] = useState(0);

    //-------------------------------------------------------------------
    // Region: hooks 
    //-------------------------------------------------------------------
    useEffect(() => {
        console.log(generateLogMessageString(`useEffect-checkoutSessionId:${checkoutSessionId}`, CLASS_NAME));

        async function fetchStatus() {

            setLoadingProps({ isLoading: true, message: "" });

            const url = `ecommerce/checkout/status`;
            axiosInstance.post(url, { id: checkoutSessionId})
                .then(resp => {

                    if (resp.data.isSuccess) {

                        if (resp.data.data == null) {
                            setStatus(AppSettings.CartStatusEnum.None);
                        }
                        else {
                            switch (resp.data.data.status) {
                                case "complete":
                                    setStatus(AppSettings.CartStatusEnum.Completed);
                                    break;
                                case "open":
                                    setStatus(AppSettings.CartStatusEnum.Pending);
                                    break;
                                case "expired":
                                default:
                                    setStatus(AppSettings.CartStatusEnum.None);
                                    break;
                            }
                        }
                        setCheckoutSession(resp.data.data);
                        setLoadingProps({
                            isLoading: false,
                            cart: resp.data.data.status === "complete" ? null : loadingProps.cart,
                            message: ""
                        });
                    }
                    else {
                        setLoadingProps({
                            isLoading: false, message: null, inlineMessages: [
                                { id: new Date().getTime(), severity: "danger", body: resp.data.message, isTimed: true }
                            ]
                        });
                    }
                })
                .catch(error => {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: `An error occurred checking status.`, isTimed: false }
                        ]
                    });
                    console.log(error);
                    //scroll back to top
                    window.scroll({
                        top: 0,
                        left: 0,
                        behavior: 'smooth',
                    });
                });
        }

        //fetch checkout completion status
        if (checkoutSessionId != null) {
            fetchStatus();
        }

    }, [checkoutSessionId, _checkStatusCounter]);


    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const getCaption = (includePipe = true) => {
        if (_status == null) return '';

        const pipeVal = includePipe ? ' | ' : '';

        switch (_status) {
            case AppSettings.CartStatusEnum.Completed:
                return `${pipeVal}Success`;
            case AppSettings.CartStatusEnum.Cancelled:
                return `${pipeVal}Cancelled`;
            case AppSettings.CartStatusEnum.Failed:
                return `${pipeVal}Fail`;
            case AppSettings.CartStatusEnum.Pending:
                return `${pipeVal}Pending`;
            case AppSettings.CartStatusEnum.Shopping:
                return `${pipeVal}Still Shopping`;
            case AppSettings.CartStatusEnum.None:
                return `${pipeVal}No Items to Checkout`;
            default:
                return "";
        }
    }

    const renderCheckoutContent = () => {

        let msg = '';
        let link = '';
        if (_status == null) {
            msg = 'There is no checkout in progress.';
            link = (<a className="btn btn-primary" href='/library'>Shop Now</a>);
        }
        else if (_checkoutSession != null)
        {
            switch (_status) {
                case AppSettings.CartStatusEnum.Completed:
                    msg = (<p>
                        We appreciate your business! Your checkout is complete.
                        {_checkoutSession?.customer?.email != null &&
                            <span className="py-1">
                            A confirmation email will be sent to {_checkoutSession?.customerDetails?.email}.
                            </span>
                        }
                        If you have any questions, please email us.
                    </p>);
                    link = (<a className="btn btn-primary" href='mailto:devops@cesmii.org'>Contact Us</a>);
                    break;
                case AppSettings.CartStatusEnum.Failed:
                    msg = (<p>
                        An error occurred processing your request. Please contact the system administrator.
                    </p>);
                    link = (<a className="btn btn-primary" href='mailto:devops@cesmii.org'>Contact Us</a>);
                    break;
                case AppSettings.CartStatusEnum.Pending:
                    msg = 'Checkout is in progress...';
                    link = (<Button className="btn btn-primary" onClick={() => { setCheckStatusCounter(_checkStatusCounter+1) }} >Update Checkout Status</Button>);
                    break;
                case AppSettings.CartStatusEnum.Shopping:
                case AppSettings.CartStatusEnum.None:
                default:
                    return null;
            }
        }
        else
        {
            return null;
        }

        return (
                <>
                <div className="d-block py-4" >
                    { msg }
                </div>
                { link }
                </>
            )
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------


    return (
        <>
            <Helmet>
                <title>{`${_caption}${getCaption()} | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            <div className="row" >
                <div className="col-sm-8 mt-4 mx-auto text-center">
                    <h1 className="m-0 headline-2">
                        {_caption}{getCaption()}
                    </h1>
                </div>
            </div>
            <div className="row" >
                <div className="col-sm-4 my-5 mx-auto text-center">
                    <span className="icon-circle primary mx-auto" ><i className="material-icons">shopping_cart</i></span>
                    <div className="d-block py-4" >
                        {renderCheckoutContent()}
                    </div>
                </div>
            </div>
        </>
    )
}

export default CheckoutComplete;