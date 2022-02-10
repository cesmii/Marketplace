import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";

import { AppSettings } from '../utils/appsettings';
import { useLoadingContext, UpdateRecentFileList, toggleFavoritesList } from "../components/contexts/LoadingContext";
import { useAuthState } from "../components/authentication/AuthContext";

import { MarketplaceBreadcrumbs } from './shared/MarketplaceBreadcrumbs';
import SocialMedia from "../components/SocialMedia";
import MarketplaceItemEntityHeader from './shared/MarketplaceItemEntityHeader';
import MarketplaceEntitySidebar from './shared/MarketplaceEntitySidebar';

import { generateLogMessageString, getMarketplaceIconName, getMarketplaceCaption } from '../utils/UtilityService'
import { clearSearchCriteria, toggleSearchFilterSelected } from '../services/MarketplaceService';
import MarketplaceTileList from './shared/MarketplaceTileList';
import { SvgVisibilityIcon } from '../components/SVGIcon';
import color from '../components/Constants';

const CLASS_NAME = "MarketplaceEntity";

function MarketplaceEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const { id } = useParams();
    const [item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const authTicket = useAuthState();
    ////is favorite calc
    //const [isFavorite, setIsFavorite] = useState((loadingProps.favoritesList != null && loadingProps.favoritesList.findIndex(x => x.url === history.location.pathname) > -1));

    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                var data = { id: id, isTracking: true };
                var url = `marketplace/getbyname`;
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this marketplace item.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This marketplace item was not found.';
                    history.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            //convert collection to comma separated list
            //special handling of meta tags which shows as a concatenated list in an input box
            result.data.metaTagsConcatenated = result.data == null || result.data.metaTags == null ? "" : result.data.metaTags.join(', ');
            //set item state value
            setItem(result.data);
            setIsLoading(false);
            setLoadingProps({ isLoading: false, message: null });

            //add to the recent file list to keep track of where we have been
            var revisedList = UpdateRecentFileList(loadingProps.recentFileList, {
                url: history.location.pathname, caption: result.data.displayName, iconName: getMarketplaceIconName(result.data),
                authorId: result.data.author != null ? result.data.author.id : null
            });
            setLoadingProps({ recentFileList: revisedList });

        }

        //fetch our data 
        fetchData();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id, authTicket.user]);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onBack = () => {
        //raised from header nav
        console.log(generateLogMessageString('onBack', CLASS_NAME));
        history.goBack();
    };

    //const onToggleFavorite = () => {
    //    console.log(generateLogMessageString('onToggleFavorite', CLASS_NAME));

    //    //add to the favorite list to keep track of where we have been
    //    var revisedList = toggleFavoritesList(loadingProps.favoritesList, { url: history.location.pathname, caption: item.displayName, iconName: getMarketplaceIconName(item), authorId: item.author?.id });
    //    setLoadingProps({ favoritesList: revisedList });
    //    setIsFavorite(revisedList != null && revisedList.findIndex(x => x.url === history.location.pathname) > -1);
    //};

    const filterByPublisher = (e) => {

        e.preventDefault();
        console.log(generateLogMessageString('filterByPublisher', CLASS_NAME));

        //clear out the selected, the query val
        var criteria = clearSearchCriteria(loadingProps.searchCriteria);

        //loop through filters and their items and find the publisher id
        toggleSearchFilterSelected(criteria, item.publisher.id);

        //update state for other components to see
        setLoadingProps({ searchCriteria: criteria });

        //navigate to marketplace list
        history.push({ pathname: `/library` });
    };

 
    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderMarketplaceBreadcrumbs = () => {
        if (item == null || item.parent == null) return;

        return (
            <>
                <MarketplaceBreadcrumbs item={item} currentUserId={authTicket.currentUserId} />
            </>
        );
    };

    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-3">
                <div className="col-sm-3" >
                    <div className="header-title-block d-flex align-items-center">
                        <span className="headline-1 d-none d-md-block">Library</span>
                        {renderSubTitle()}
                    </div>
                </div>
                <div className="col-sm-9 d-flex align-items-center" >
                    {renderHeaderBlock()}
                </div>
            </div>
        );
    };

    const renderHeaderBlock = () => {

        return (
            <>
                <h1 className="m-0 mr-2">
                    {item.displayName}
                </h1>
                {/*<SVGIcon name={isFavorite ? "favorite" : "favorite-border"} size="24" fill={color.forestGreen} onClick={toggleFavoritesList} />*/}
                {authTicket.user != null &&
                    <a className="btn btn-icon-outline circle ml-auto" href={`/admin/library/${item.id}`} ><i className="material-icons">edit</i></a>
                }
            </>
        )
    }

    const renderSolutionDetails = () => {

        return (
            <>
                <div className="row mb-2 mb-md-3" >
                    <div className="col-sm-12 d-flex align-items-center">
                        <h2 className="m-0 mr-2">
                            <span className="d-none d-md-inline">Smart Manufacturing App </span> Details
                        </h2>
                        <a className="btn btn-primary px-1 px-md-4 auto-width ml-auto text-nowrap" href={`/add-to-platform/${item.id}`} >Add to SM Platform</a>
                    </div>
                </div>
                <div className="row" >
                    <div className="col-sm-12">
                        <div className="mb-3" dangerouslySetInnerHTML={{ __html: item.description }} ></div>
                    </div>
                </div>
                <div className="row" >
                    <div className="col-sm-8">
                        <span className="m-0 mr-2 mb-2 mb-md-0">
                            <b className="mr-1" >Published By:</b>
                            <br className="d-block d-md-none" />
                            <a href={`/publisher/${item.publisher.name}`} >{item.publisher.displayName}</a></span>
                        <span className="m-0 mr-2 my-2 mb-md-0 d-flex align-items-center">
                            <SvgVisibilityIcon fill={color.link} />
                            <button className="btn btn-link" onClick={filterByPublisher} >
                            View all by this publisher</button>
                        </span>
                    </div>
                    {(item.publisher.socialMediaLinks != null && item.publisher.socialMediaLinks.length > 0) && 
                        <div className="col-sm-4 d-flex justify-content-md-end mb-2 mb-md-0 align-items-center">
                            <SocialMedia items={item.publisher.socialMediaLinks} />
                        </div>
                    }
                </div>
            </>
        )
    }

    //render the main grid
    const renderItemRow = () => {
        if (!loadingProps.isLoading && (item == null)) {
            return;
        }
        return (<MarketplaceItemEntityHeader key={item.id} item={item} currentUserId={authTicket.user == null ? null : authTicket.user.id} showActions={true} cssClass="marketplace-list-item" />)
    }

    //
    const renderSubTitle = () => {
        return (
            <span onClick={onBack} className="btn btn-text-solo auto-width ml-auto justify-content-end d-flex clickable hover" ><i className="material-icons">chevron_left</i>Back</span>
        );
    }

    //render new
    const renderSimilarItems = () => {
        if (loadingProps.isLoading) return;

        if (item.similarItems == null || item.similarItems.length === 0) return;

        return (
            <>
                <div className="row" >
                    <div className="col-sm-12 mt-5 mb-3" >
                        <h3 className="m-0">
                            Related
                        </h3>
                    </div>
                </div>
                <div className="row" >
                    <div className="col-sm-12">
                        <MarketplaceTileList items={item.similarItems} layout="banner" colCount={3} />
                    </div>
                </div>
            </>
        );
    }


    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    var caption = getMarketplaceCaption(item);

    //return final ui
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            {(!loadingProps.isLoading && !isLoading) &&
                <>
                    {renderMarketplaceBreadcrumbs()}
                    {renderHeaderRow()}
                    <div className="row" >
                    <div className="col-sm-3 order-2 order-sm-1" >
                            <MarketplaceEntitySidebar item={item} />
                        </div>
                    <div className="col-sm-9 mb-4 order-1 order-sm-2" >
                            {(!loadingProps.isLoading && !isLoading) &&
                                <div className="marketplace-entity">
                                    {renderItemRow()}
                                    {renderSolutionDetails()}
                                    {renderSimilarItems()}
                                </div>
                            }
                        </div>
                    </div>
                </>
            }
        </>
    )
}

export default MarketplaceEntity;