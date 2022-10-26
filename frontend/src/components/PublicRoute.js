import React from "react";
import { Route } from "react-router-dom";
import DownloadMessage from "./DownloadMessage";
import { InlineMessage } from "./InlineMessage";
import { JobMessage } from "./JobMessage";
import ModalMessage from "./ModalMessage";

const SimpleLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid container-md" >
        <div className="main-panel mt-2 mt-md-4 w-100">
            <InlineMessage />
            <JobMessage />
            <DownloadMessage />
            {children}
        </div>
        <ModalMessage />
    </div>
);

const FilterLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid container-md d-flex" >
        <div className="main-panel mt-2 mt-md-4 w-100">
            <InlineMessage />
            <JobMessage />
            <DownloadMessage />
            {children}
        </div>
        <ModalMessage />
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


