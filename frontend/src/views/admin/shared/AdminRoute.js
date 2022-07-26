import React from "react";
import { Route, Redirect } from "react-router-dom";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import DownloadMessage from "../../../components/DownloadMessage";
import { InlineMessage } from "../../../components/InlineMessage";
import { isInRoles } from "../../../utils/UtilityService";

const AdminLayout = ({ children }) => (

    <div className="container-fluid container-md" >
        <div className="main-panel m-4">
            <InlineMessage />
            <DownloadMessage />
            {children}
        </div>
    </div>
);

function AdminRoute({ component: Component, ...rest }) {

    const { instance } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();
    //Check for is authenticated. Check individual permissions - ie can manage marketplace items.
    var isAuthorized = _isAuthenticated && _activeAccount != null && (rest.roles == null || isInRoles(_activeAccount, rest.roles));

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