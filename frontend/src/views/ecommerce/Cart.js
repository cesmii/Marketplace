import React from 'react'
import { Helmet } from "react-helmet"

import { AppSettings } from '../../utils/appsettings'
import CartPreview from '../../components/eCommerce/CartPreview';

//const CLASS_NAME = "Cart";

function Cart() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _caption = 'Shopping Cart';

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

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${_caption} | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            <div className="row" >
                <div className="col-sm-12 mb-2">
                    <h1 className="m-0 headline-2">
                        {_caption}
                    </h1>
                </div>
            </div>
            <CartPreview />
        </>
    )
}

export default Cart;