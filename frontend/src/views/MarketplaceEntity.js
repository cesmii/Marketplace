import React, { useState, useEffect, useRef } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"

import axiosInstance from "../services/AxiosService";
import { useLoginStatus } from '../components/OnLoginHandler';
import { AppSettings } from '../utils/appsettings';
import { useLoadingContext, UpdateRecentFileList } from "../components/contexts/LoadingContext";

import { MarketplaceBreadcrumbs } from './shared/MarketplaceBreadcrumbs';
import SocialMedia from "../components/SocialMedia";
import MarketplaceItemEntityHeader from './shared/MarketplaceItemEntityHeader';
import MarketplaceEntitySidebar from './shared/MarketplaceEntitySidebar';

import { convertHtmlToString, generateLogMessageString, getImageUrl, getMarketplaceIconName } from '../utils/UtilityService'
import { clearSearchCriteria, hasRelatedItems, MarketplaceRelatedItems, renderSimilarItems, renderSpecifications, toggleSearchFilterSelected } from '../services/MarketplaceService';
import { renderSchemaOrgContentMarketplaceItem } from '../utils/schemaOrgUtil';
import { SvgVisibilityIcon } from '../components/SVGIcon';

import color from '../components/Constants';
import './styles/MarketplaceEntity.scss';

const CLASS_NAME = "MarketplaceEntity";

function MarketplaceEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const _scrollToSpecs = useRef(null);
    const _scrollToRelated = useRef(null);

    const { id } = useParams();
    const [item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { isAuthenticated, isAuthorized } = useLoginStatus(null, [AppSettings.AADAdminRole]);

    ////is favorite calc
    //const [isFavorite, setIsFavorite] = useState((loadingProps.favoritesList != null && loadingProps.favoritesList.findIndex(x => x.url === history.location.pathname) > -1));

    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                const data = { id: id, isTracking: true };
                const url = `marketplace/getbyname`;
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
            const revisedList = UpdateRecentFileList(loadingProps.recentFileList, {
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
    }, [id, isAuthenticated]);

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

    const onViewSpecifications = (e) => {
        e.preventDefault();
        window.scrollTo({ top: (_scrollToSpecs.current.getBoundingClientRect().y - 80), behavior: 'smooth' });
    }

    const onViewRelatedItems = (e) => {
        e.preventDefault();
        window.scrollTo({ top: (_scrollToRelated.current.getBoundingClientRect().y - 80), behavior: 'smooth' });
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderMarketplaceBreadcrumbs = () => {
        if (item == null || item.parent == null) return;

        return (
            <>
                <MarketplaceBreadcrumbs item={item} isAuthenticated={isAuthenticated} />
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
                {isAuthorized &&
                    <a className="btn btn-icon-outline circle ml-auto" href={`/admin/library/${item.id}`} ><i className="material-icons">edit</i></a>
                }
            </>
        )
    }

    const renderSolutionDetails = () => {

        return (
            <>
                <div className="row" >
                    <div className="col-sm-12">
                        <div className="mb-3 entity-description" dangerouslySetInnerHTML={{ __html: item.description }} ></div>
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
        return (<MarketplaceItemEntityHeader key={item.id} item={item} isAuthenticated={isAuthenticated} isAuthorized={isAuthorized}
            showActions={true} cssClass="marketplace-list-item" onViewSpecifications={onViewSpecifications} onViewRelatedItems={onViewRelatedItems} />)
    }

    //
    const renderSubTitle = () => {
        return (
            <span onClick={onBack} className="px-2 btn btn-text-solo align-items-center auto-width ml-auto justify-content-end d-flex clickable hover" ><i className="material-icons">chevron_left</i>Back</span>
        );
    }

    const renderAccordion = () => {
        if (loadingProps.isLoading) return;

        return (
            <>

                <div className="accordion" id="accordionExample">
                    <div className="card mb-0">
                        <div className="card-header bg-transparent p-2 pt-3 border-bottom-0" id="headingOne">
                            <div className="col-sm-12 d-flex align-items-center">
                                <h2 className="m-0 mr-2">
                                    {(item.type == null || item.type.name === null) ?
                                        'Smart Manufacturing App Details'
                                        : `${item.type.name.replace('SM ', 'Smart Manufacturing ')} Details`
                                    }
                                </h2>
                                <a className="btn btn-primary px-1 px-md-4 auto-width ml-auto text-nowrap" href={`/more-info/app/${item.id}`} >Request More Info</a>
                            </div>
                        </div>
                        <div id="collapseOne" className="collapse show mb-3" aria-labelledby="headingOne" >
                            <div className="card-body">
                                {renderSolutionDetails()}
                            </div>
                        </div>
                    </div>
                    {(hasRelatedItems(item, "required") || hasRelatedItems(item, "recommended")) &&
                        <div className="card mb-0">
                            <div ref={_scrollToSpecs} className="card-header bg-transparent p-0 border-bottom-0" id="headingTwo">
                                <button className="btn btn-content-accordion p-3 py-2 text-left d-block w-100" type="button" data-toggle="collapse" data-target="#collapseTwo" aria-expanded="false" aria-controls="collapseTwo">
                                    <h2 className="mb-0">
                                        Specifications
                                    </h2>
                                </button>
                            </div>
                            <div id="collapseTwo" className="collapse mb-3" aria-labelledby="headingTwo" >
                                <div className="card-body">
                                    <MarketplaceRelatedItems item={item} displayMode="specifications" />
                                </div>
                            </div>
                        </div>
                    }
                    {hasRelatedItems(item, "similar") &&
                        <div className="card mb-0">
                            <div className="card-header bg-transparent p-0 border-bottom-0" id="headingThree">
                                <button ref={_scrollToRelated} className="btn btn-content-accordion p-3 py-2 text-left d-block w-100" type="button" data-toggle="collapse" data-target="#collapseThree" aria-expanded="false" aria-controls="collapseThree">
                                    <h2 className="mb-0">
                                        Similar Items
                                    </h2>
                                </button>
                            </div>
                            <div id="collapseThree" className="collapse mb-3" aria-labelledby="headingThree">
                                <div className="card-body">
                                    <MarketplaceRelatedItems item={item} displayMode="similarItems" />
                                </div>
                            </div>
                        </div>
                    }
                </div>
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    //var caption = getMarketplaceCaption(item);
    const _caption = item != null && item.displayName != null ? item.displayName : id;
    const _title = `${_caption} | ${AppSettings.Titles.Main}`;
    const _typeCaption = item.type == null || item.type.name === null ? 'Smart Manufacturing App'
        : item.type.name.replace('SM ', 'Smart Manufacturing ');
    const _description = `${convertHtmlToString(item.abstract)} ${AppSettings.MetaDescription.Abbreviated}`;

    //return final ui
    return (
        <>
            <Helmet>
                <title>{_title}</title>
                <meta name="description" content={_description} />
                <meta property="og:title" content={_title} />
                <meta property="og:description" content={_description} />
                <meta property="og:type" content={_typeCaption} />
                {item.imageLandscape &&
                    <meta property="og:image" content={getImageUrl(item.imageLandscape)} />
                }
                {renderSchemaOrgContentMarketplaceItem(_title, _description, item)}
            </Helmet>
            {(!loadingProps.isLoading && !isLoading) &&
                <>
                    {renderMarketplaceBreadcrumbs()}
                    {renderHeaderRow()}
                    <div className="row" >
                    <div className="col-sm-3 order-2 order-sm-1" >
                        <MarketplaceEntitySidebar item={item} className="light" />
                        </div>
                    <div className="col-sm-9 mb-4 order-1 order-sm-2" >
                            {(!loadingProps.isLoading && !isLoading) &&
                                <div className="marketplace-entity">
                                    {renderItemRow()}
                                    {renderAccordion()}
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
