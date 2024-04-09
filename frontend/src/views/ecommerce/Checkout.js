import React from 'react'
import { Helmet } from "react-helmet"

import { AppSettings } from '../../utils/appsettings'

//const CLASS_NAME = "Checkout";

function Checkout() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _caption = 'Checkout';

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderCartItems = () => {
        return (
            <div className="row" >
                <div className="col-sm-12 mb-2">
                    <h1 className="m-0 headline-2">
                        {_caption}
                    </h1>
                </div>
                <div className="col-sm-12">
                    <p>
                        TBD - content here. 
                    </p>
                </div>
            </div>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${_caption} | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            {renderCartItems()}
        </>
    )
}

export default Checkout;