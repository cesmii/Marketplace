import React from "react";
import { Route, Redirect } from "react-router-dom";
import { useAuthState } from "./AuthContext";

function PrivateRoute({ component: Component, ...rest }) {

    const authTicket = useAuthState();

    //TBD - this would become more elaborate. Do more than just check for the existence of this value. Check for a token expiry, etc.
    return (
        <Route
            {...rest}
            render={props => (authTicket != null && authTicket.token != null)  ?
                (<Component {...props} />) :
                (<Redirect to="/" />)
            }
        />
    );
}

export default PrivateRoute;