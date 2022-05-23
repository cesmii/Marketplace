import React from "react";
import { Route, Redirect } from "react-router-dom";

import { useAuthState } from "../../../components/authentication/AuthContext";
import DownloadMessage from "../../../components/DownloadMessage";
//import AdminSideMenu from "./AdminSideMenu";
import { InlineMessage } from "../../../components/InlineMessage";

const AdminLayout = ({ children }) => (

//    <div id="--routes-wrapper" className="container-fluid sidebar p-0 d-flex" >
//        <AdminSideMenu />
//        <div className="main-panel m-4 w-100">
//            <InlineMessage />
//            {children}
//        </div>
//    </div>
    <div className="container-fluid container-md" >
        <div className="main-panel m-4">
            <InlineMessage />
            <DownloadMessage />
            {children}
        </div>
    </div>
);

function AdminRoute({ component: Component, ...rest }) {

    const authTicket = useAuthState();
    //TBD - this would become more elaborate. Do more than just check for the existence of this value. Check for a token expiry, etc.
    var isAuthorized = (authTicket != null && authTicket.token != null);
    //TBD - check individual permissions - ie can manage marketplace items.
    return (
        <Route
            {...rest}
            render={props => isAuthorized  ?
                (<AdminLayout><Component {...props} /></AdminLayout>) :
                (<Redirect to="/" />)
            }
        />
    );
}

export default AdminRoute;