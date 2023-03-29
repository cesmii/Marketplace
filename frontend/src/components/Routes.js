import React from 'react'
import {Switch} from "react-router-dom"

//common components
import AdminRoute from '../views/admin/shared/AdminRoute'
import { PublicRoute, PublicRouteWFilter } from './PublicRoute'

//page level imports
import PageNotFound from "../views/PageNotFound"
import MarketplaceList from '../views/MarketplaceList';
import MarketplaceEntity from '../views/MarketplaceEntity'
import ProfileEntity from '../views/ProfileEntity'
import PublisherEntity from '../views/PublisherEntity';
import AdminMarketplaceEntity from '../views/admin/AdminMarketplaceEntity'
import AdminPubisherEntity from '../views/admin/AdminPubisherEntity'
import RequestInfo from '../views/RequestInfo'
import Home from '../views/Home'
import NotAuthorized from '../views/NotAuthorized'

import AdminRequestInfoEntity from '../views/admin/AdminRequestInfoEntity'
import AdminRequestInfoList from '../views/admin/AdminRequestInfoList'
import AdminStockImageList from '../views/admin/AdminStockImageList'
import AdminLookupList from '../views/admin/AdminLookupList'
import AdminLookupEntity from '../views/admin/AdminLookupEntity'
import AdminMarketplaceList from '../views/admin/AdminMarketplaceList'
import AdminPublisherList from '../views/admin/AdminPublisherList'
import AdminJobDefinitionList from '../views/admin/AdminJobDefinitionList'
import AdminJobDefinitionEntity from '../views/admin/AdminJobDefinitionEntity'
import SitemapGenerator from '../views/admin/SitemapGenerator'
import AccountProfile from '../views/AccountProfile'
import LoginSuccess from '../views/LoginSuccess'

//const CLASS_NAME = "Routes";


function Routes() {

    //-------------------------------------------------------------------
    //  Routes
    //-------------------------------------------------------------------
    return(
        <Switch>
            {/* Route order matters in the profile/ routes* - TBD - update to admin versions of the forms... */}
            <PublicRouteWFilter exact path="/" component={Home} />
            <PublicRouteWFilter exact path="/login/success" component={LoginSuccess} />
            <PublicRouteWFilter path="/admin/returnUrl=:returnUrl" component={Home} />
            <PublicRouteWFilter exact path="/admin" component={Home} />
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
            <AdminRoute path="/admin/library/list" component={AdminMarketplaceList} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/library/copy/:parentId" component={AdminMarketplaceEntity} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/library/:id" component={AdminMarketplaceEntity} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/publisher/list" component={AdminPublisherList} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/publisher/copy/:parentId" component={AdminPubisherEntity} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/publisher/:id" component={AdminPubisherEntity} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/requestinfo/list" component={AdminRequestInfoList} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/requestinfo/:id" component={AdminRequestInfoEntity} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/images/list" component={AdminStockImageList} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/lookup/list" component={AdminLookupList} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/lookup/:id" component={AdminLookupEntity} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/jobDefinition/list" component={AdminJobDefinitionList} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/jobDefinition/:id" component={AdminJobDefinitionEntity} roles={['cesmii.marketplace.marketplaceadmin']} />
            <AdminRoute path="/admin/sitemap/generate" component={SitemapGenerator} roles={['cesmii.marketplace.marketplaceadmin']} />

            <AdminRoute path="/account" component={AccountProfile} />

            <PublicRoute path="/notpermitted" component={NotAuthorized} />
            <PublicRoute path="/notauthorized" component={NotAuthorized} />
            <PublicRoute component={PageNotFound} />
        </Switch>

    )

}

export default Routes