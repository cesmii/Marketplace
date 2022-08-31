import React, { useEffect } from 'react'
import { useMsal } from "@azure/msal-react";
import axiosInstance from "../services/AxiosService";

import SideMenuItem from './SideMenuItem'
import { useLoadingContext } from './contexts/LoadingContext'
import color from './Constants'
import { generateLogMessageString } from '../utils/UtilityService'
import SideMenuLinkList from './SideMenuLinkList'
import './styles/SideMenu.scss';
import './styles/SideMenuItem.scss';

const CLASS_NAME = "SideMenu";
function SideMenu() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();

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
    var marketplaceSubMenu = [{ url: "/admin/library/new", caption: "New" } ];

    return (
        <div className="siderail-left" >
            <ul>
                <SideMenuItem caption="Marketplace library" bgColor={color.shark} iconName="folder-profile" navUrl="/admin/library/all" subText={marketplaceCountAllCaption()} subMenuItems={marketplaceSubMenu} />
            </ul>
            {(loadingProps.recentFileList != null && loadingProps.recentFileList.length > 0) &&
                <SideMenuLinkList caption='Recent / Open Items' iconName='access-time' items={loadingProps.recentFileList} currentUserId={_activeAccount?.username} ></SideMenuLinkList>
            }
        </div>
    )

}

export default SideMenu