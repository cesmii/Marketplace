import React from 'react'
import { Routes as SwitchRoutes, Route } from 'react-router-dom';

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
import AdminProfileEntity from '../views/admin/AdminProfileEntity'
import AdminProfileList from '../views/admin/AdminProfileList'
import AdminExternalSourceList from '../views/admin/AdminExternalSourceList'
import AdminExternalSourceEntity from '../views/admin/AdminExternalSourceEntity'
import SitemapGenerator from '../views/admin/SitemapGenerator'
import AccountProfile from '../views/AccountProfile'
import LoginSuccess from '../views/LoginSuccess'
import { AppSettings } from '../utils/appsettings'
import ExternalSourceEntity from '../views/ExternalSourceEntity'

//const CLASS_NAME = "Routes";

//Upgrade from 5.2 to v6
//https://github.com/remix-run/react-router/blob/main/docs/upgrading/v5.md

function Routes() {

	//-------------------------------------------------------------------
	//  Routes
	//-------------------------------------------------------------------
	return (
		<SwitchRoutes>
			{/* Route order matters in the profile/ routes* - TBD - update to admin versions of the forms... */}
			<Route element={<PublicRoute />}>
				{/*<Route path="/about" element={<About} />*/}
				<Route path="/library/:code/:id" element={<ExternalSourceEntity />} />
				<Route path="/library/:id" element={<MarketplaceEntity />} />
				<Route path="/profile/:code/:id" element={<ProfileEntity />} />
				<Route path="/publisher/:id" element={<PublisherEntity />} />
				<Route path="/more-info/:itemType/:code/:externalId" element={<RequestInfo />} />
				<Route path="/more-info/:itemType/:id" element={<RequestInfo />} />
				<Route path="/request-info/publisher/:publisherId" element={<RequestInfo />} />
				<Route path="/contact-us/" element={<RequestInfo />} />
				<Route path="/contact-us/:type" element={<RequestInfo />} />
			</Route>

			<Route element={<PublicRouteWFilter />}>
				<Route path="/" element={<Home />} />
				<Route path="/login/success" element={<LoginSuccess />} />
				<Route path="/login/returnUrl=:returnUrl" element={<Home />} />
				<Route path="/login" element={<Home />} />
				{/*<Route path="/about" element={<About />} />*/}
				<Route path="/library" element={<MarketplaceList />} />
				<Route path="/all" element={<MarketplaceList />} />
			</Route>

			{/* Admin UI order matters in the profile/ routes* - TBD - update to admin versions of the forms...*/}

			<Route element={<AdminRoute roles={[AppSettings.AADAdminRole]} />} >
				<Route path="/admin/library/list" element={<AdminMarketplaceList />}  />
				<Route path="/admin/library/copy/:parentId" element={<AdminMarketplaceEntity />}  />
				<Route path="/admin/library/:id" element={<AdminMarketplaceEntity />}  />
				<Route path="/admin/publisher/list" element={<AdminPublisherList />}  />
				<Route path="/admin/publisher/copy/:parentId" element={<AdminPubisherEntity />}  />
				<Route path="/admin/publisher/:id" element={<AdminPubisherEntity />}  />
				<Route path="/admin/requestinfo/list" element={<AdminRequestInfoList />}  />
				<Route path="/admin/requestinfo/:id" element={<AdminRequestInfoEntity />}  />
				<Route path="/admin/images/list" element={<AdminStockImageList />}  />
				<Route path="/admin/lookup/list" element={<AdminLookupList />}  />
				<Route path="/admin/lookup/copy/:parentId" element={<AdminLookupEntity />}  />
				<Route path="/admin/lookup/:id" element={<AdminLookupEntity />}  />
				<Route path="/admin/jobDefinition/list" element={<AdminJobDefinitionList />}  />
				<Route path="/admin/jobDefinition/copy/:parentid" element={<AdminJobDefinitionEntity />}  />
				<Route path="/admin/jobDefinition/:id" element={<AdminJobDefinitionEntity />}  />
				<Route path="/admin/relateditem/list" element={<AdminProfileList />}  />
				<Route path="/admin/relateditem/:code/:id" element={<AdminProfileEntity />}  />
				<Route path="/admin/externalsource/list" element={<AdminExternalSourceList />}  />
				<Route path="/admin/externalsource/copy/:parentId" element={<AdminExternalSourceEntity />}  />
				<Route path="/admin/externalsource/:id" element={<AdminExternalSourceEntity />}  />
				<Route path="/admin/sitemap/generate" element={<SitemapGenerator />}  />
				<Route path="/account" element={<AccountProfile />} />
			</Route>
			<Route element={<PublicRoute />}>
				<Route path='/notpermitted' element={<NotAuthorized />} />
				<Route path='/notauthorized' element={<NotAuthorized />} />
				<Route element={<PageNotFound />} />
			</Route>
		</SwitchRoutes>

	)

}

export default Routes