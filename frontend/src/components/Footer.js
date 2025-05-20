import React from 'react'
import { AppSettings, LookupData } from '../utils/appsettings';
import SocialMedia from './SocialMedia';
import HubSpotTracking from './HubSpotTracking';

import './styles/Footer.scss';
import SubscribeForm from './SubscribeForm';

function Footer() {
    return (
        <footer className="p-4">
            <div className="container-fluid container-md my-4">
                <div className="row mb-5">
                    <div className="col-lg-6 mb-2">
                        <span className="headline-3 d-block mb-1">Stay up to date</span>
                        <span className="muted" >Subscribe to our newsletter for updates</span>
                    </div>
                    <div className="col-lg-6 d-flex mb-2">
                        <SubscribeForm />
                    </div>
                </div>
                <hr className="pb-5" />
                <div className="row mb-2">
                    <div className="col-6 col-sm-3 mb-3">
                        <span className="headline-3 d-block mb-2">Company</span>
                        <ul className="links">
                            <li>
                                <a href="https://www.cesmii.org/" target="_blank" rel="noreferrer" >About CESMII</a>
                            </li>
                            <li>
                                <a href="https://www.cesmii.org/what-is-smart-manufacturing-the-smart-manufacturing-definition/" target="_blank" rel="noreferrer" >About Smart Manufacturing</a>
                            </li>
                            <li>
                                <a href="https://www.cesmii.org/blog/" target="_blank" rel="noreferrer" >Blog</a>
                            </li>
                            <li>
                                <a href="/contact-us/membership" >Become a member</a>
                            </li>
                        </ul>
                    </div>
                    <div className="col-6 col-sm-3">
                        <span className="headline-3 d-block mb-2">Information</span>
                        <ul className="links">
                            <li>
                                <a href="/contact-us/request-demo" >Request a demo</a>
                            </li>
                            <li>
                                <a href="/contact-us/support" >Support</a>
                            </li>
                            <li>
                                <a href="https://www.ucla.edu/terms-of-use" target="_blank" rel="noreferrer" >Privacy Policy</a>
                            </li>
                            <li>
                                <a href="/admin" >Admin Login</a>
                            </li>
                        </ul>
                    </div>
                    <div className="col-sm-6 justify-content-sm-center d-flex">
                        <div>
                            <span className="headline-3 d-block mb-2">{AppSettings.Titles.Caption}</span>
                            <SocialMedia items={LookupData.socialMediaLinks} />
                            <span className="muted d-block mt-2" >Copyright &copy;2021-2023 CESMII. All rights reserved.</span>
                        </div>
                    </div>
                </div>
            </div>
            <HubSpotTracking />
        </footer>
    )
}

export default Footer
