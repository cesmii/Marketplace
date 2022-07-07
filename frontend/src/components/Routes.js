import React from 'react'
import {Switch} from "react-router-dom"

//common components
import AdminRoute from '../views/admin/shared/AdminRoute'
import { PublicRoute, PublicRouteWFilter } from './PublicRoute'

//page level imports
import Login from "../views/Login"
import PageNotFound from "../views/PageNotFound"
import MarketplaceList from '../views/MarketplaceList';
import MarketplaceEntity from '../views/MarketplaceEntity'
import ProfileEntity from '../views/ProfileEntity'
import PublisherEntity from '../views/PublisherEntity';
import AdminMarketplaceEntity from '../views/admin/AdminMarketplaceEntity'
import AdminPubisherEntity from '../views/admin/AdminPubisherEntity'
import RequestInfo from '../views/RequestInfo'
import Home from '../views/Home'

import AdminRequestInfoEntity from '../views/admin/AdminRequestInfoEntity'
import AdminRequestInfoList from '../views/admin/AdminRequestInfoList'
import AdminStockImageList from '../views/admin/AdminStockImageList'
import AdminLookupList from '../views/admin/AdminLookupList'
import AdminLookupEntity from '../views/admin/AdminLookupEntity'
import AdminMarketplaceList from '../views/admin/AdminMarketplaceList'
import AdminPublisherList from '../views/admin/AdminPublisherList'
import AdminJobDefinitionList from '../views/admin/AdminJobDefinitionList'
import AdminJobDefinitionEntity from '../views/admin/AdminJobDefinitionEntity'
import AccountProfile from '../views/AccountProfile'

//const CLASS_NAME = "Routes";


function Routes() {

    //-------------------------------------------------------------------
    //  Routes
    //-------------------------------------------------------------------
    return(
        <Switch>
            {/* Rout order matters in the profile/ routes* - TBD - update to admin versions of the forms... */}
            <PublicRouteWFilter exact path="/" component={Home} />
            {/*<PublicRoute exact path="/about" component={About} />*/}
            <PublicRoute exact path="/library/:id" component={MarketplaceEntity} />
            <PublicRoute exact path="/profile/:id" component={ProfileEntity} />
            <PublicRouteWFilter exact path="/library" component={MarketplaceList} />
            <PublicRouteWFilter exact path="/all" component={MarketplaceList} />
            <PublicRoute exact path="/publisher/:id" component={PublisherEntity} />
            <PublicRoute exact path="/more-info/:itemType/:id" component={RequestInfo} />
            <PublicRoute exact path="/request-info/publisher/:publisherId" component={RequestInfo} />
            <PublicRoute exact path="/contact-us/" component={RequestInfo} />
            <PublicRoute exact path="/contact-us/:type" component={RequestInfo} />

            {/* Admin UI order matters in the profile/ routes* - TBD - update to admin versions of the forms...*/}
            <PublicRoute exact path="/login" component={Login} />
            <PublicRoute exact path="/admin" component={Login} />
            <AdminRoute path="/admin/library/list" component={AdminMarketplaceList} />
            <AdminRoute path="/admin/library/copy/:parentId" component={AdminMarketplaceEntity} />
            <AdminRoute path="/admin/library/:id" component={AdminMarketplaceEntity} />
            <AdminRoute path="/admin/publisher/list" component={AdminPublisherList} />
            <AdminRoute path="/admin/publisher/copy/:parentId" component={AdminPubisherEntity} />
            <AdminRoute path="/admin/publisher/:id" component={AdminPubisherEntity} />
            <AdminRoute path="/admin/requestinfo/list" component={AdminRequestInfoList} />
            <AdminRoute path="/admin/requestinfo/:id" component={AdminRequestInfoEntity} />
            <AdminRoute path="/admin/images/list" component={AdminStockImageList} />
            <AdminRoute path="/admin/lookup/list" component={AdminLookupList} />
            <AdminRoute path="/admin/lookup/:id" component={AdminLookupEntity} />
            <AdminRoute path="/admin/jobDefinition/list" component={AdminJobDefinitionList} />
            <AdminRoute path="/admin/jobDefinition/:id" component={AdminJobDefinitionEntity} />

            <AdminRoute path="/account" component={AccountProfile} />

            <PublicRoute component={PageNotFound} />
        </Switch>

    )

}

export default Routes