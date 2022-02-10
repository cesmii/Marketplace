import React from "react";
import { Route } from "react-router-dom";
import { InlineMessage } from "./InlineMessage";

const SimpleLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid container-md" >
        <div className="main-panel mt-2 mt-md-4 w-100">
            <InlineMessage />
            {children}
        </div>
    </div>
);

const FilterLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid container-md d-flex" >
        <div className="main-panel mt-2 mt-md-4 w-100">
            <InlineMessage />
            {children}
        </div>
    </div>
);

export function PublicRoute({ component: Component, ...rest }) {

    return (
        <Route
            {...rest}
            render={props =>
                (<SimpleLayout><Component {...props} /></SimpleLayout>)
            }
        />
    );
}

export function PublicRouteWFilter({ component: Component, ...rest }) {

    return (
        <Route
            {...rest}
            render={props =>
                (<FilterLayout><Component {...props} /></FilterLayout>)
            }
        />
    );
}

