import React, { useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { Helmet } from "react-helmet"

import { AppSettings } from '../../utils/appsettings'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

//const CLASS_NAME = "CheckoutComplete";

function CheckoutComplete() {

    //TBD
    //empty cart or mark cart with status.

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _caption = 'Checkout';
    const { type } = useParams();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: hooks 
    //-------------------------------------------------------------------
    useEffect(() => {
        //update cart status based on type
        let cart = loadingProps.cart;
        if (cart == null) cart = {};
        let status = '' + type;

        let checkout = loadingProps.checkout;
        //console.log(checkout);

        if (status.toLocaleLowerCase() === 'success') {
            cart.status = AppSettings.CartStatusEnum.Completed;
            cart.items = [];
        }
        else {
            cart.status = AppSettings.CartStatusEnum.Failed;
        }

        setLoadingProps({ cart: cart });
    }, [type]);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderInvalidInfo = () => {
        return (
            <div className="row" >
                <div className="col-sm-12">
                    <p>
                        TBD - cannot goto checkout complete with cart status of {loadingProps.cart?.status}
                        Add link to return to continue shopping.
                    </p>
                </div>
            </div>
        );
    }

    const renderSuccessInfo = () => {
        return (
            <div className="row" >
                <div className="col-sm-12">
                    <p>
                        TBD - render cart details, render confirmation id if returned, etc..
                    </p>
                </div>
            </div>
        );
    }

    const renderFailInfo = () => {
        return (
            <div className="row" >
                <div className="col-sm-12">
                    <p>
                        TBD - render failure reason, etc.
                    </p>
                </div>
            </div>
        );
    }

    const getCaption = (includePipe = true) => {
        if (loadingProps.cart?.status == null) return '';

        const pipeVal = includePipe ? ' | ' : '';

        switch (loadingProps.cart?.status) {
            case AppSettings.CartStatusEnum.Completed:
                return `${pipeVal}Success ${pipeVal}${AppSettings.Titles.Main}`;
            case AppSettings.CartStatusEnum.Cancelled:
                return `${pipeVal}Cancelled ${pipeVal}${AppSettings.Titles.Main}`;
            case AppSettings.CartStatusEnum.Failed:
                return `${pipeVal}Fail ${pipeVal}${AppSettings.Titles.Main}`;
            case AppSettings.CartStatusEnum.Pending:
                return `${pipeVal}Pending ${pipeVal}${AppSettings.Titles.Main}`;
            case AppSettings.CartStatusEnum.Shopping:
                return `${pipeVal}Still Shopping ${pipeVal}${AppSettings.Titles.Main}`;
            case AppSettings.CartStatusEnum.None:
                return `${pipeVal}No Items to Checkout ${pipeVal}${AppSettings.Titles.Main}`;
            default:
                return "";
        }
    }

    const renderCheckoutContent = () => {

        if (loadingProps.cart?.status == null) return '';

        switch (loadingProps.cart?.status) {
            case AppSettings.CartStatusEnum.Completed:
                return renderSuccessInfo();
            case AppSettings.CartStatusEnum.Failed:
                return renderFailInfo();
            case AppSettings.CartStatusEnum.Pending:
            case AppSettings.CartStatusEnum.Shopping:
            case AppSettings.CartStatusEnum.None:
            default:
                return renderInvalidInfo();
        }
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------


    return (
        <>
            <Helmet>
                <title>{`${_caption}${getCaption()}`}</title>
            </Helmet>
            <div className="row" >
                <div className="col-sm-12 mb-2">
                    <h1 className="m-0 headline-2">
                        {_caption}{getCaption()}
                    </h1>
                </div>
                {renderCheckoutContent()}
            </div>
        </>
    )
}

export default CheckoutComplete;