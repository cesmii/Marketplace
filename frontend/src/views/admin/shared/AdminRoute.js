import React from "react";
import { Route, Redirect } from "react-router-dom";

import DownloadMessage from "../../../components/DownloadMessage";
import { InlineMessage } from "../../../components/InlineMessage";
import ModalMessage from "../../../components/ModalMessage";
import { useLoginStatus } from "../../../components/OnLoginHandler";

const AdminLayout = ({ children }) => (

    <div className="container-fluid container-md" >
        <div className="main-panel m-4">
            <InlineMessage />
            <DownloadMessage />
            {children}
        </div>
        <ModalMessage />
    </div>
);

function AdminRoute({ component: Component, ...rest }) {

    const { isAuthenticated, isAuthorized, redirectUrl } = useLoginStatus(rest.location, rest.roles);

    return (
        <Route
            {...rest}
            render={props => isAuthenticated && isAuthorized ?
                (<AdminLayout><Component {...props} /></AdminLayout>) :
                (<Redirect to={redirectUrl} />)
            }
        />
    );
}

export default AdminRoute;