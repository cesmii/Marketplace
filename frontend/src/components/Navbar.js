import React, { useState, useEffect } from 'react'
import { useHistory } from "react-router-dom"
import { InteractionStatus } from "@azure/msal-browser";
import { useMsal } from "@azure/msal-react";

import Dropdown from 'react-bootstrap/Dropdown'

import logo from './img/Logo-CESMII.svg'
import { SVGIcon } from './SVGIcon'
import Color from './Constants'

import { useLoadingContext } from './contexts/LoadingContext';
import { AppSettings } from '../utils/appsettings';
import { doLogout, useLoginStatus } from './OnLoginHandler';
import LoginButton from './LoginButton';
import CartIcon from './eCommerce/CartIcon';

import './styles/Navbar.scss'

//const CLASS_NAME = "Navbar";

function Navbar() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { instance, inProgress } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const { isAuthenticated, isAuthorized } = useLoginStatus(null, [AppSettings.AADAdminRole]);
    const { loadingProps } = useLoadingContext();
    const [_urlLibrary, setUrlLibrary] = useState('/library');

    //-------------------------------------------------------------------
    // Region: Hook - Dynamically set the library url to include page num and page size to avoid 
    // double load on library page
    //-------------------------------------------------------------------
    useEffect(() => {
        if (loadingProps.searchCriteria == null) {
            return;
        }
        setUrlLibrary(`/library?p=1&t=${loadingProps.searchCriteria.take}`);
    }, [loadingProps.searchCriteria]);

    //-------------------------------------------------------------------
    // Region: event handlers
    //-------------------------------------------------------------------
    const onLogoutClick = (e) => {
        doLogout(history, instance, '/', true, true);
        e.preventDefault();
    }

    //-------------------------------------------------------------------
    // Region: render helpers
    //-------------------------------------------------------------------
    const renderNav = () => {
        return (
            <nav className="navbar navbar-expand-md navbar-dark bg-primary">
                <div className="container-fluid container-lg">
                    <a className="navbar-brand d-flex align-items-center" href="/">
                        <img className="mr-3 mb-2 d-none d-md-block" src={logo} alt="CESMII Logo"></img>
                        <span className="headline-2">{AppSettings.Titles.Caption}</span>
                    </a>
                    <button className="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarMain" aria-controls="navbarMain" aria-expanded="false" aria-label="Toggle navigation">
                        <span className="navbar-toggler-icon"></span>
                    </button>
                    <div className="navbar-collapse collapse" id="navbarMain">
                        <div className="ml-auto my-2 my-lg-0 nav navbar-nav  align-items-md-center" >
                            <a className={`nav-link py-1 px-2 ${history.location.pathname === "/library" ? "active" : ""}`} href={_urlLibrary}>Library</a>
                            {/*
                            <a className={`nav-link py-1 px-2 ${history.location.pathname === "/industries" ? "active" : ""}`}
                                href="/industries">Industries</a>
                            <a className={`nav-link py-1 px-2 ${history.location.pathname === "/processes" ? "active" : ""}`} href="/processes">Processes</a>
                            */}
                            {/*    <a className={`nav-link py-1 px-2 ${history.location.pathname === "/about" ? "active" : ""}`}*/}
                            {/*        href="/about">About</a>*/}
                            <a className={`nav-link py-1 px-2 ${history.location.pathname.indexOf("/contact-us/") > -1 ? "active" : ""}`}
                                href="/contact-us/contribute">Contribute</a>
                            <CartIcon />
                            <LoginButton />
                            {renderAdminMenu()}
                        </div>
                    </div>
                </div>
            </nav>
        );
    };

    const renderAdminMenu = () => {
        if (!isAuthenticated && !isAuthorized) return;
        return (
            <div className="nav-item" >
            <Dropdown>
                <Dropdown.Toggle className="ml-0 ml-md-2 px-1 dropdown-custom-components d-flex align-items-center">
                    <SVGIcon name="account-circle" size="32" fill={Color.white} className="mr-2" />
                    {_activeAccount?.name}
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    <Dropdown.Item eventKey="1" href="/account">Account Profile</Dropdown.Item>
                    <Dropdown.Divider />
                    {isAuthorized &&
                        <>
                            <Dropdown.Item eventKey="2" href="/admin/library/new">Add Marketplace Item</Dropdown.Item>
                            <Dropdown.Item eventKey="3" href="/admin/publisher/new">Add Publisher</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item eventKey="4" href="/admin/library/list">Manage Marketplace Items</Dropdown.Item>
                            <Dropdown.Item eventKey="5" href="/admin/publisher/list">Manage Publishers</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item eventKey="6" href="/admin/externalsource/list">Manage External Sources</Dropdown.Item>
                            <Dropdown.Item eventKey="7" href="/admin/relateditem/list">Manage Related Items</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item eventKey="8" href="/admin/lookup/list">Manage Lookup Items</Dropdown.Item>
                            <Dropdown.Item eventKey="9" href="/admin/images/list">Manage Stock Images</Dropdown.Item>
                            <Dropdown.Item eventKey="10" href="/admin/requestinfo/list">Manage Request Info Inquiries</Dropdown.Item>
                            <Dropdown.Divider />
                        </>
                    }
                    {isAuthorized &&
                        <>
                            <Dropdown.Item eventKey="11" href="/admin/jobdefinition/list">Manage Job Definitions</Dropdown.Item>
                            <Dropdown.Divider />
                        </>
                    }
                    {isAuthorized &&
                        <>
                            <Dropdown.Item eventKey="12" href="/admin/sitemap/generate">Generate Sitemap...</Dropdown.Item>
                            <Dropdown.Divider />
                        </>
                    }
                    {(inProgress !== InteractionStatus.Startup && inProgress !== InteractionStatus.HandleRedirect) &&
                        <Dropdown.Item eventKey="10" onClick={onLogoutClick} >Logout</Dropdown.Item>
                    }
                </Dropdown.Menu>
            </Dropdown>
            </div>
        );
    };


    return (
        <>
            <header>
                {renderNav()}
            </header>
        </>
    )

}

export default Navbar