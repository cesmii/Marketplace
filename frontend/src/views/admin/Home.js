import React from 'react'
import { Helmet } from "react-helmet"

import { AppSettings } from '../../utils/appsettings'
import { renderTitleBlock } from '../../utils/UtilityService';

function AdminHome() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const caption = 'Admin';

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (
            <div className="row pb-3">
                <div className="col-sm-12 d-flex">
                    {renderTitleBlock("Admin Home", null, null)}
                </div>
            </div>
        );
    };


    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | Admin "+ " | " + caption}</title>
            </Helmet>
            {renderHeaderRow()}
            <div id="--cesmii-main-content">
                <div id="--cesmii-left-content">
                    TBD - lower priority but create an admin area for CESMII personnel to add, edit, hide, show marketplace items. 
                </div>
            </div>
        </>
    )
}

export default AdminHome;