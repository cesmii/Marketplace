import React from 'react'
import { Helmet } from "react-helmet"

import { AppSettings } from '../utils/appsettings'

//const CLASS_NAME = "About";
//const entityInfo = {
//    name: "Marketplace Item",
//    namePlural: "Marketplace Items",
//    entityUrl: "/marketplace/:id",
//    listUrl: "/marketplace/all"
//}

function About() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _caption = 'About';

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
    //render 
    const renderMission = () => {
        return (
            <div className="row" >
                <div className="col-sm-12 d-flex align-items-center mt-3 mb-2" >
                    <h2 className="m-0">
                        CESMII Mission
                    </h2>
                </div>
                <div className="col-sm-12">
                    <p>
                        Radically accelerate the development and adoption of advanced sensors, controls, platforms, and models, to enable Smart Manufacturing (SM) to become the driving sustainable engine that delivers real-time business improvements in U.S. manufacturing.
                    </p>
                    <p>
                        CESMII is a Membership based organization.  While much of our value is shared publically, to educate and drive awareness for the benefits of Smart Manufacturing, the real guidance and value comes through access to our Member Portal.
                        <a href="/contact-us/membership" className="ms-1" >Learn how to become a member.</a>
                    </p>
                </div>
            </div>
            );
    }

    //
    const renderAboutMarketplace = () => {
        return (
            <div className="row" >
                <div className="col-sm-12 mb-2">
                    <h1 className="m-0 headline-2">
                        About SM Marketplace
                    </h1>
                </div>
                <div className="col-sm-12">
                    <p>
                        TBD - content here. TBD - content here. TBD - content here. TBD - content here.TBD - content here. TBD - content here.TBD - content here. TBD - content here.TBD - content here. TBD - content here.TBD - content here. TBD - content here.
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
            {renderAboutMarketplace()}
            {renderMission()}
        </>
    )
}

export default About;