import React, { useState, useEffect } from 'react'

import { InteractionStatus } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import { useHistory } from "react-router-dom"
import { useAuthDispatch, useAuthState } from "./authentication/AuthContext";
import { logout } from "./authentication/AuthActions";

import Dropdown from 'react-bootstrap/Dropdown'

import { generateLogMessageString } from '../utils/UtilityService';
import logo from './img/Logo-CESMII.svg'
import { SVGIcon } from './SVGIcon'
import Color from './Constants'

import './styles/Navbar.scss'
import { AppSettings } from '../utils/appsettings';
import LoginButton from './LoginButton';

const CLASS_NAME = "Navbar";

function Navbar() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { instance, inProgress } = useMsal();
    const isAuthenticated = useIsAuthenticated();

    const authTicket = useAuthState();
    const dispatch = useAuthDispatch() //get the dispatch method from the useDispatch custom hook
    const [_user, setUser] = useState(null);

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        setUser(authTicket.user);
    }, [authTicket.user]);

    //-------------------------------------------------------------------
    // Region: event handlers
    //-------------------------------------------------------------------
    const onLogoutClick = () => {
        //MSAL logout
        instance.logoutPopup();

        //Old way - keep for now
        //updates state and removes user auth ticket from local storage
        let logoutAction = logout(dispatch);
        if (!logoutAction) {
            console.error(generateLogMessageString(`onLogoutClick||logoutAction||An error occurred setting the logout state.`, CLASS_NAME));
        }
        else {
            history.push(`/`);
        }
        //setAuthTicket(null);
    }

    const renderNav = () => {
        return (
            <nav className="navbar navbar-dark bg-primary navbar-expand-md">
                <div className="container-fluid pr-0">
                    <button className="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarNav" aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
                        <span className="navbar-toggler-icon"></span>
                    </button>
                    <div className="collapse navbar-collapse mt-2 mt-md-0" id="navbarNav">
                        <ul className="navbar-nav align-items-start align-items-md-center">
                            <li className="nav-item">
                                <a className={`nav-link py-1 px-2 ${history.location.pathname === "/library" ? "active" : ""}`} href="/library">Library</a>
                            </li>
                            {/*
                            <li className="nav-item">
                                <a className={`nav-link py-1 px-2 ${history.location.pathname === "/industries" ? "active" : ""}`}
                                    href="/industries">Industries</a>
                            </li>
                            <li className="nav-item">
                                <a className={`nav-link py-1 px-2 ${history.location.pathname === "/processes" ? "active" : ""}`} href="/processes">Processes</a>
                            </li>
                            */}
                            {/*<li className="nav-item">*/}
                            {/*    <a className={`nav-link py-1 px-2 ${history.location.pathname === "/about" ? "active" : ""}`}*/}
                            {/*        href="/about">About</a>*/}
                            {/*</li>*/}
                            <li className="nav-item">
                                <a className={`nav-link py-1 px-2 ${history.location.pathname.indexOf("/contact-us/") > -1 ? "active" : ""}`}
                                    href="/contact-us/contribute">Contribute</a>
                            </li>
                            {renderLoginButton()}
                            {renderAdminMenu()}
                        </ul>
                    </div>
                </div>
            </nav>
        );
    };

    const renderAdminMenu = () => {
        if (_user == null) return;
        return (
            <>
                <li className="nav-item" >
                    <Dropdown>
                        <Dropdown.Toggle className="ml-0 ml-md-2 px-1 dropdown-custom-components d-flex align-items-center">
                            <SVGIcon name="account-circle" size="32" fill={Color.white} className="mr-2" />
                            {_user.fullName}
                        </Dropdown.Toggle>
                        <Dropdown.Menu>
                            <Dropdown.Item eventKey="1" href="/account">Account Profile</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item eventKey="2" href="/admin/library/new">Add Marketplace Item</Dropdown.Item>
                            <Dropdown.Item eventKey="3" href="/admin/publisher/new">Add Publisher</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item eventKey="4" href="/admin/library/list">Manage Marketplace Items</Dropdown.Item>
                            <Dropdown.Item eventKey="5" href="/admin/publisher/list">Manage Publishers</Dropdown.Item>
                            <Dropdown.Item eventKey="6" href="/admin/lookup/list">Manage Lookup Items</Dropdown.Item>
                            <Dropdown.Item eventKey="7" href="/admin/images/list">Manage Stock Images</Dropdown.Item>
                            <Dropdown.Item eventKey="8" href="/admin/jobdefinition/list">Manage Job Definitions</Dropdown.Item>
                            <Dropdown.Item eventKey="9" href="/admin/requestinfo/list">Manage Request Info Inquiries</Dropdown.Item>
                            <Dropdown.Divider />
                            {(inProgress !== InteractionStatus.Startup && inProgress !== InteractionStatus.HandleRedirect) &&
                                <Dropdown.Item eventKey="9" onClick={onLogoutClick} >Logout</Dropdown.Item>
                            }
                        </Dropdown.Menu>
                    </Dropdown>
                </li>
            </>
        );
    };

    const renderLoginButton = () => {
        if (isAuthenticated) return;
        return (
            <LoginButton />
        );
    };

    return (
        <header>
            <div className="container-fluid container-lg d-flex h-100" >
                <div className="col-sm-12 px-0 px-sm-1 d-flex align-content-center" >
                    <div className="d-flex align-items-center">
                        <a className="navbar-brand d-flex align-items-center" href="/">
                            <img className="mr-3 mb-2 d-none d-md-block" src={logo} alt="CESMII Logo"></img>
                            <span className="headline-2">{AppSettings.Titles.Caption}</span>
                        </a>
                    </div>
                    <div className="d-flex align-items-center ml-auto">
                        {renderNav()}
                    </div>
                </div>
            </div>
        </header>
    )

}

export default Navbar