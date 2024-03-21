import React, { useEffect, useParams } from 'react'
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
        cart.status = (type?.toLowercase() === 'success' ? AppSettings.CartStatusEnum.Completed : AppSettings.CartStatusEnum.Failed);
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
            case AppSettings.CartStatusEnum.CheckoutSuccess:
                return `${pipeVal}Success`;
            case AppSettings.CartStatusEnum.CheckoutFail:
                return `${pipeVal}Fail`;
            case AppSettings.CartStatusEnum.CheckoutInProgress:
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

        if (loadingProps.cart?.status == null) return '';

        switch (loadingProps.cart?.status) {
            case AppSettings.CartStatusEnum.CheckoutSuccess:
                return renderSuccessInfo();
            case AppSettings.CartStatusEnum.CheckoutFail:
                return renderFailInfo();
            case AppSettings.CartStatusEnum.CheckoutInProgress:
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
                <title>{`${_caption}{getCaption()}| ${AppSettings.Titles.Main}`}</title>
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