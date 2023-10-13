import React, { useState, useEffect, useRef } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";

import { AppSettings } from '../utils/appsettings';
import { useLoginStatus } from '../components/OnLoginHandler';
import { useLoadingContext, UpdateRecentFileList } from "../components/contexts/LoadingContext";
import MarketplaceItemEntityHeader from './shared/MarketplaceItemEntityHeader';
import { cleanFileName, generateLogMessageString, getImageUrl, getMarketplaceIconName, getRandomArrayIndexes, scrollTopScreen } from '../utils/UtilityService'
import { renderSchemaOrgContentMarketplaceItem } from '../utils/schemaOrgUtil';
import { MarketplaceRelatedItems} from '../services/MarketplaceService';

import './styles/MarketplaceEntity.scss';

const CLASS_NAME = "ExternalSourceEntity";

function ExternalSourceEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const _scrollToSpecs = useRef(null);
    const _scrollToRelated = useRef(null);

    const { id, sourceId } = useParams();
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
                const data = { id: id, sourceId: sourceId };
                const url = `externalsource/getbyid`;
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this external source item.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This external source item was not found.';
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
    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-3">
                <div className="col-sm-9 m-auto d-flex align-items-center" >
                    {renderHeaderBlock()}
                    {renderSubTitle()}
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
            </>
        )
    }

    const renderMetaTags = (items, limit) => {
        if (items == null) return;

        if (limit == null) limit = items.length;
        const randomIndexes = getRandomArrayIndexes(items, limit);
        if (randomIndexes == null || randomIndexes.length === 0) return;
        return (
            randomIndexes.map((i) => {
                return (
                    <span key={items[i]} className="metatag badge meta border">
                        {items[i]}
                    </span>
                )
            })
        )
    }

    const renderSolutionDetails = () => {
        const showPub = false;
        return (
            <>
                <div className="row" >
                    <div className="col-sm-12">
                        <div className="mb-3" dangerouslySetInnerHTML={{ __html: item.description }} ></div>
                    </div>
                </div>
                {item.metaTags != null &&
                    <div className="row pt-2 no-gutters" >
                        <div className="col-sm-12">
                            {renderMetaTags(item.metaTags, null)}
                        </div>
                    </div>
                }
                {(showPub && item.publisher?.displayName) &&
                    <div className="row" >
                        <div className="col-sm-12">
                            <span className="m-0 mr-2 mb-2 mb-md-0">
                                <b className="mr-1" >Published By:</b>
                                <br className="d-block d-md-none" />
                                {item.publisher.displayName}
                            </span>
                        </div>
                    </div>
                }
            </>
        )
    }

    //render the main grid
    const renderItemRow = () => {
        if (!loadingProps.isLoading && (item == null)) {
            return;
        }
        return (
            <MarketplaceItemEntityHeader key={item.id} item={item} isAuthenticated={isAuthenticated}
                isAuthorized={isAuthorized} showActions={true} cssClass="marketplace-list-item"
                showProfileDesignerLink={false}
                onViewSpecifications={onViewSpecifications} onViewRelatedItems={onViewRelatedItems} />
        )
    }

    //
    const renderSubTitle = () => {
        return (
            <span onClick={onBack} className="px-2 btn btn-text-solo align-items-center auto-width ml-auto justify-content-end d-flex clickable hover" ><i className="material-icons">chevron_left</i>Back</span>
        );
    }

    const renderMainContent = () => {
        if (loadingProps.isLoading) return;

        return (
            <>
                <div className="marketplace-list-item border" >
                    <div className="card mb-0 border-0">
                        <div className="card-header bg-transparent p-2 pt-3 border-bottom-0" id="headingOne">
                            <div className="col-sm-12 d-flex align-items-center">
                                <h2>
                                {(item.type == null || item.type.name === null) ?
                                    'Details'
                                    : `${item.type.name.replace('SM ', 'Smart Manufacturing ')} Details`
                                }
                                </h2>
                                <a className="btn btn-primary px-1 px-md-4 auto-width ml-auto text-nowrap" href={`/more-info/external/${item.externalSourceId}/${item.id}`} >Request More Info</a>
                            </div>
                        </div>
                        <div id="collapseOne" className="collapse show mb-3" aria-labelledby="headingOne" >
                            <div className="card-body">
                                {renderSolutionDetails()}
                            </div>
                        </div>
                    </div>
                </div>

                {(item.relatedItemsGrouped != null && item.relatedItemsGrouped.length > 0) &&
                    <>
                        <h2 ref={_scrollToSpecs} className="m-3 mt-4" >Specifications</h2>
                        <MarketplaceRelatedItems items={item.relatedItemsGrouped} />
                    </>
                }
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
    const _description = `SM Profile: ${_caption}. An SM Profile defines the Information Model
                        for a manufacturing asset or process, with a goal to arrive at common, re-usable interfaces
                        for accessing data.`;

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
                    {renderHeaderRow()}
                    <div className="row" >
                    <div className="col-sm-9 m-auto mb-4" >
                            {(!loadingProps.isLoading && !isLoading) &&
                                <div className="marketplace-entity">
                                    {renderItemRow()}
                                    {renderMainContent()}
                                </div>
                            }
                        </div>
                    </div>
                </>
            }
        </>
    )
}

export default ExternalSourceEntity;
