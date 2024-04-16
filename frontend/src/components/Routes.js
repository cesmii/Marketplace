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
import CustomLandingPage from '../views/custom/CustomLandingPage'

import AdminRequestInfoEntity from '../views/admin/AdminRequestInfoEntity'
import AdminRequestInfoList from '../views/admin/AdminRequestInfoList'
import AdminStockImageList from '../views/admin/AdminStockImageList'
import AdminLookupList from '../views/admin/AdminLookupList'
import AdminLookupEntity from '../views/admin/AdminLookupEntity'
import AdminMarketplaceList from '../views/admin/AdminMarketplaceList'
import AdminPublisherList from '../views/admin/AdminPublisherList'
import AdminJobDefinitionList from '../views/admin/AdminJobDefinitionList'
import AdminJobDefinitionEntity from '../views/admin/AdminJobDefinitionEntity'
import AdminProfileEntity from '../views/admin/AdminProfileEntity'
import AdminProfileList from '../views/admin/AdminProfileList'
import AdminExternalSourceList from '../views/admin/AdminExternalSourceList'
import AdminExternalSourceEntity from '../views/admin/AdminExternalSourceEntity'
import SitemapGenerator from '../views/admin/SitemapGenerator'
import AccountProfile from '../views/AccountProfile'
import LoginSuccess from '../views/LoginSuccess'
//eCommerce 
import Cart from '../views/ecommerce/Cart'
import Checkout from '../views/ecommerce/Checkout'
import CheckoutComplete from '../views/ecommerce/CheckoutComplete'

import { AppSettings } from '../utils/appsettings'
import ExternalSourceEntity from '../views/ExternalSourceEntity'

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
            <PublicRouteWFilter path="/login/returnUrl=:returnUrl" component={Home} />
            <PublicRouteWFilter exact path="/login" component={Home} />
            {/*<PublicRoute exact path="/about" component={About} />*/}
            <PublicRoute exact path="/library/:code/:id" component={ExternalSourceEntity} />
            <PublicRoute exact path="/library/:id" component={MarketplaceEntity} />
            <PublicRoute exact path="/profile/:code/:id" component={ProfileEntity} />
            <PublicRouteWFilter exact path="/library" component={MarketplaceList} />
            <PublicRouteWFilter exact path="/all" component={MarketplaceList} />
            <PublicRoute exact path="/publisher/:id" component={PublisherEntity} />
            <PublicRoute exact path="/more-info/:itemType/:code/:externalId" component={RequestInfo} />
            <PublicRoute exact path="/more-info/:itemType/:id" component={RequestInfo} />
            <PublicRoute exact path="/request-info/publisher/:publisherId" component={RequestInfo} />
            <PublicRoute exact path="/contact-us/" component={RequestInfo} />
            <PublicRoute exact path="/contact-us/:type" component={RequestInfo} />
            
            {/* Custom pages to support one off scenarios...*/}
            <PublicRoute exact path="/custom/action/:name/:jobName" component={CustomLandingPage} />

            {/* Admin UI order matters in the profile/ routes* - TBD - update to admin versions of the forms...*/}
            <AdminRoute path="/admin/library/list" component={AdminMarketplaceList} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/library/copy/:parentId" component={AdminMarketplaceEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/library/:id" component={AdminMarketplaceEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/publisher/list" component={AdminPublisherList} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/publisher/copy/:parentId" component={AdminPubisherEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/publisher/:id" component={AdminPubisherEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/requestinfo/list" component={AdminRequestInfoList} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/requestinfo/:id" component={AdminRequestInfoEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/images/list" component={AdminStockImageList} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/lookup/list" component={AdminLookupList} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/lookup/copy/:parentId" component={AdminLookupEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/lookup/:id" component={AdminLookupEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/jobDefinition/list" component={AdminJobDefinitionList} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/jobDefinition/copy/:parentid" component={AdminJobDefinitionEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/jobDefinition/:id" component={AdminJobDefinitionEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/relateditem/list" component={AdminProfileList} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/relateditem/:code/:id" component={AdminProfileEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/externalsource/list" component={AdminExternalSourceList} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/externalsource/copy/:parentId" component={AdminExternalSourceEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/externalsource/:id" component={AdminExternalSourceEntity} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/admin/sitemap/generate" component={SitemapGenerator} roles={[AppSettings.AADAdminRole]} />
            <AdminRoute path="/account" component={AccountProfile} />

            {/* eCommerce Routes */}
            <PublicRoute exact path="/checkout/:type" component={CheckoutComplete} />
            <PublicRoute exact path="/cart" component={Cart} />
            <PublicRoute exact path="/checkoutold" component={Checkout} />

            <PublicRoute path="/notpermitted" component={NotAuthorized} />
            <PublicRoute path="/notauthorized" component={NotAuthorized} />
            <PublicRoute component={PageNotFound} />
        </Switch>

    )

}

export default Routes