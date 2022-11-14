import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import { AppSettings } from '../utils/appsettings';
import { generateLogMessageString, getMarketplaceIconName, getMarketplaceCaption, isInRole, convertHtmlToString } from '../utils/UtilityService'
import { MarketplaceBreadcrumbs } from './shared/MarketplaceBreadcrumbs';
import { useLoadingContext, UpdateRecentFileList } from "../components/contexts/LoadingContext";

import { SvgVisibilityIcon } from "../components/SVGIcon";
import color from "../components/Constants";
import SocialMedia from "../components/SocialMedia";
import { clearSearchCriteria, toggleSearchFilterSelected } from '../services/MarketplaceService';
import MarketplaceTileList from './shared/MarketplaceTileList';
import PublisherEntitySidebar from './shared/PublisherEntitySidebar';
import { renderSchemaOrgContentPublisher } from '../utils/schemaOrgUtil';

const CLASS_NAME = "PublisherEntity";

function PublisherEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const { id } = useParams();
    const [item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { instance } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();
    //Check for is authenticated. Check individual permissions - ie can manage marketplace items.
    const _isAuthorized = _isAuthenticated && _activeAccount != null && (isInRole(_activeAccount, "cesmii.marketplace.marketplaceadmin"));

    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                var data = { id: id };
                var url = `publisher/getbyname`;
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
    }, [id]);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onBack = () => {
        //raised from header nav
        console.log(generateLogMessageString('onBack', CLASS_NAME));
        history.goBack();
    };

    const filterByPublisher = (e) => {

        e.preventDefault();
        console.log(generateLogMessageString('filterByPublisher', CLASS_NAME));

        //clear out the selected, the query val
        var criteria = clearSearchCriteria(loadingProps.searchCriteria);

        //loop through filters and their items and find the publisher id
        toggleSearchFilterSelected(criteria, item.id);

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
                <MarketplaceBreadcrumbs item={item} isAuthenticated={_isAuthenticated && _activeAccount != null} />
            </>
        );
    };

    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-3">
                <div className="col-sm-3" >
                    <div className="header-title-block d-flex align-items-center">
                        <span className="headline-1 d-none d-md-block">Publisher</span>
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
                <a className="btn btn-secondary ml-2 px-1 px-md-4 auto-width ml-auto text-nowrap" href={`/request-info/publisher/${item.id}`} >Request Info</a>
                {_isAuthorized &&
                    <a className="btn btn-icon-outline circle ml-2" href={`/admin/publisher/${item.id}`} ><i className="material-icons">edit</i></a>
                }
            </>
        )
    }

    const renderSolutionDetails = () => {

        return (
            <>
                <div className="row mb-3 mb-md-4" >
                    <div className="col-sm-12">
                        <div className="mb-0" dangerouslySetInnerHTML={{ __html: item.description }} ></div>
                    </div>
                </div>
                <div className="row mb-2" >
                    <div className="col-sm-12">
                        <span className="headline-4">More </span>
                    </div>

                </div>
                <div className="row mb-3 mb-md-5" >
                    <div className="col-sm-6" >
                        <span className="material-icons-outlined d-flex align-items-left"><i className="material-icons mr-1" style={{ color: color.selectedBg }}>language</i><a href={item.companyUrl} className="a-text">Website </a></span>
                    </div>
                    <div className="col-sm-6" >
                        <span className="d-flex justify-content-end"><SocialMedia items={item.socialMediaLinks} /> </span>
                    </div>
                    <div className="col-sm-12 d-flex align-items-center">
                        <SvgVisibilityIcon fill={color.link} />
                        <button className="btn btn-link" onClick={filterByPublisher} >
                            View all by this publisher</button>
                    </div>
                </div>

            </>
        )
    }

    const renderMarketplaceItems = () => {
        if (item.marketplaceItems == null || item.marketplaceItems.length === 0) return;
        return (
            <>
                <div className="row" >
                    <div className="col-sm-12 d-flex align-items-center mb-2" >
                        <h2 className="m-0">
                            More from this Publisher
                        </h2>
                    </div>
                </div>
                <div className="row" >
                    <div className="col-sm-12">
                        <MarketplaceTileList items={item.marketplaceItems} layout="thumbnail" colCount={2} />
                    </div>
                </div>
            </>
        )
    }

    //
    const renderSubTitle = () => {
        return (
            <span onClick={onBack} className="px-2 btn btn-text-solo align-items-center auto-width ml-auto justify-content-end d-flex clickable hover" ><i className="material-icons">chevron_left</i>Back</span>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    const _title = `${item.displayName ?? item.name} | Publisher | ${AppSettings.Titles.Main}`;
    //this might be long.
    var _description = convertHtmlToString(item.description).trimStart().trimEnd();
    _description = _description.length > 300 ? _description.substring(0, 300) + '...' : _description;

    //return final ui
    return (
        <>
            <Helmet>
                <title>{_title}</title>
                <meta name="description" content={_description} />
                <meta property="og:title" content={_title} />
                <meta property="og:description" content={_description} />
                <meta property="og:type" content={'publisher'} />
                {renderSchemaOrgContentPublisher(item)}
            </Helmet>
            {(!loadingProps.isLoading && !isLoading) &&
                <>
                    {renderMarketplaceBreadcrumbs()}
                    {renderHeaderRow()}
                    <div className="row" >
                    <div className="col-sm-3 order-2 order-sm-1" >
                        <PublisherEntitySidebar item={item} className="light" />
                        </div>
                    <div className="col-sm-9 mb-2 mb-md-4 order-1 order-sm-2" >
                            <div className="publisher-entity">
                                {renderSolutionDetails()}
                                <hr className="my-4" />
                                {renderMarketplaceItems()}
                            </div>
                        </div>
                    </div>
                </>
            }
        </>
    )
}

export default PublisherEntity;
