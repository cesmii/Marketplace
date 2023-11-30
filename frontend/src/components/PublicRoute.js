import React from "react";
import { Outlet } from "react-router-dom";

import DownloadMessage from "./DownloadMessage";
import { InlineMessage } from "./InlineMessage";
import { JobMessage } from "./JobMessage";
import ModalMessage from "./ModalMessage";

export function PublicRoute() {
    return (
        <div id="--routes-wrapper" className="container-fluid container-md" >
            <div className="main-panel mt-2 mt-md-4 w-100">
                <InlineMessage />
                <JobMessage />
                <DownloadMessage />
                <Outlet />
            </div>
            <ModalMessage />
        </div>
    );
}

export function PublicRouteWFilter() {
    return (
        <div id="--routes-wrapper" className="container-fluid container-md d-flex" >
            <div className="main-panel mt-2 mt-md-4 w-100">
                <InlineMessage />
                <JobMessage />
                <DownloadMessage />
                <Outlet />
            </div>
            <ModalMessage />
        </div>
    );
}


