import React from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";

import DownloadMessage from "../../../components/DownloadMessage";
import { InlineMessage } from "../../../components/InlineMessage";
import ModalMessage from "../../../components/ModalMessage";
import { useLoginStatus } from "../../../components/OnLoginHandler";

function AdminLayout() {
    return (
        <div className="container-fluid container-md" >
            <div className="main-panel m-4">
                <InlineMessage />
                <DownloadMessage />
                <Outlet />
            </div>
            <ModalMessage />
        </div>
    );
}


function AdminRoute(props) {

    let location = useLocation();

    const { isAuthenticated, isAuthorized, redirectUrl } = useLoginStatus(location, props.roles);

    return isAuthenticated && isAuthorized ? AdminLayout() : (<Navigate to={redirectUrl} />);
}

export default AdminRoute;
