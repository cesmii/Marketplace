import React, { useEffect } from 'react'
import axiosInstance from "../../../services/AxiosService";

import SideMenuItem from '../../../components/SideMenuItem'
import { useAuthState } from "../../../components/authentication/AuthContext"; 
import { useLoadingContext } from '../../../components/contexts/LoadingContext'
import color from '../../../components/Constants'
import { generateLogMessageString } from '../../../utils/UtilityService'
import SideMenuLinkList from '../../../components/SideMenuLinkList'

const CLASS_NAME = "AdminSideMenu";
function AdminSideMenu() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const authTicket = useAuthState();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Load Profile Counts if some part of the app indicates the need to do so.
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchMarketplaceCounts() {
            console.log(generateLogMessageString('useEffect||fetchMarketplaceCounts||async', CLASS_NAME));
            //console.log(authTicket);

            var url = `marketplace/count`;
            await axiosInstance.get(url).then(result => {
                if (result.status === 200) {
                    setLoadingProps({ marketplaceCount: result.data, refreshMarketplaceCount: null });
                } else {
                    setLoadingProps({ marketplaceCount: result.data, refreshMarketplaceCount: null });
                }
            }).catch(e => {
                if (e.response && e.response.status === 401) {
                    setLoadingProps({isLoading: false, message: null, refreshMarketplaceCount: null});
                }
                else {
                    console.log(generateLogMessageString('useEffect||fetchMarketplaceCounts||' + JSON.stringify(e), CLASS_NAME, 'error'));
                    console.log(e);
                    setLoadingProps({isLoading: false, message: null, refreshMarketplaceCount: null });
                }
            });
        }

        //if this is changed to true, then go get new profile counts. 
        //this would be set to true on Add Profile save or import or login. 
        if (loadingProps.refreshMarketplaceCount) {
            fetchMarketplaceCounts();
        }
    }, [loadingProps.refreshMarketplaceCount]);

    const marketplaceCountAllCaption = () => {
        if (loadingProps.marketplaceCount == null ) return;
        if (loadingProps.marketplaceCount === 1) return ('1 item'); 
        return (`${loadingProps.marketplaceCount} items`);
    };

    //sub menu items
    var marketplaceSubMenu = [{ url: "/admin/marketplace/new", caption: "New" } ];

    return (
        <div className="siderail-left" >
            <ul>
                <SideMenuItem caption="Home" bgColor={color.shark} iconName="home" navUrl="/" />
                <SideMenuItem caption="Marketplace Items" bgColor={color.shark} iconName="folder-profile" navUrl="/admin/marketplace/all" subText={marketplaceCountAllCaption()} subMenuItems={marketplaceSubMenu} />
            </ul>
            {(loadingProps.recentFileList != null && loadingProps.recentFileList.length > 0) &&
                <SideMenuLinkList caption='Recent / Open Items' iconName='access-time' items={loadingProps.recentFileList} currentUserId={authTicket.user.id} ></SideMenuLinkList>
            }
        </div>
    )

}

export default AdminSideMenu