import React from "react";
import { Route, Redirect } from "react-router-dom";

import { useAuthState } from "./AuthContext";
import AdminSideMenu from "../../views/admin/shared/AdminSideMenu";
import { InlineMessage } from "../InlineMessage";


const AdminLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid sidebar p-0 d-flex" >
        <AdminSideMenu />
        <div className="main-panel m-4 w-100">
            <InlineMessage />
            {children}
        </div>
    </div>
);

function AdminRoute({ component: Component, ...rest }) {

    const authTicket = useAuthState();

    //TBD - this would become more elaborate. Do more than just check for the existence of this value. Check for a token expiry, etc.
    return (
        <Route
            {...rest}
            render={props => (authTicket != null && authTicket.token != null)  ?
                (<AdminLayout><Component {...props} /></AdminLayout>) :
                (<Redirect to="/" />)
            }
        />
    );
}

export default AdminRoute;