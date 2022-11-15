import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";

import { AppSettings } from '../utils/appsettings';
import { useLoadingContext, UpdateRecentFileList } from "../components/contexts/LoadingContext";
import MarketplaceItemEntityHeader from './shared/MarketplaceItemEntityHeader';
import { cleanFileName, generateLogMessageString, getImageUrl, getMarketplaceIconName, scrollTopScreen } from '../utils/UtilityService'
import MarketplaceTileList from './shared/MarketplaceTileList';

import './styles/MarketplaceEntity.scss';
import { renderSchemaOrgContentMarketplaceItem } from '../utils/schemaOrgUtil';

const CLASS_NAME = "ProfileEntity";

function ProfileEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { id } = useParams();
    const [item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
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
                var url = `profile/getbyid`;
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this profile item.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This profile item was not found.';
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

    const downloadProfile = async (req) => {
        console.log(generateLogMessageString(`downloadProfile||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        //msgs.push({ profileId: p.id, fileName: cleanFileName(p.namespace || p.displayName), immediateDownload: true });
        msgs.push({ requestInfo: req, fileName: cleanFileName(req.smProfile.namespace || req.smProfile.displayName), immediateDownload: true });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)), downloadNodesetCounter: (loadingProps.downloadNodesetCounter == null ? 0 : loadingProps.downloadNodesetCounter) + 1 });
        scrollTopScreen();
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
                    SM Profile: {item.displayName}
                </h1>
                {/*<SVGIcon name={isFavorite ? "favorite" : "favorite-border"} size="24" fill={color.forestGreen} onClick={toggleFavoritesList} />*/}
            </>
        )
    }

    const renderSolutionDetails = () => {

        return (
            <>
                <div className="row mb-2 mb-md-3" >
                    <div className="col-sm-12 d-flex align-items-center">
                        <h2 className="m-0 mr-2">
                            <span className="d-none d-md-inline">Smart Manufacturing Profile </span> Details
                        </h2>
                        <a className="btn btn-primary px-1 px-md-4 auto-width ml-auto text-nowrap" href={`/more-info/profile/${item.id}`} >Request More Info</a>
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
                            {item.publisher.displayName}
                        </span>
                    </div>
                {/*    {(item.publisher.socialMediaLinks != null && item.publisher.socialMediaLinks.length > 0) && */}
                {/*        <div className="col-sm-4 d-flex justify-content-md-end mb-2 mb-md-0 align-items-center">*/}
                {/*            <SocialMedia items={item.publisher.socialMediaLinks} />*/}
                {/*        </div>*/}
                {/*    }*/}
                </div>
            </>
        )
    }

    //render the main grid
    const renderItemRow = () => {
        if (!loadingProps.isLoading && (item == null)) {
            return;
        }
        return (
            <MarketplaceItemEntityHeader key={item.id} item={item} currentUserId={null} showActions={true} cssClass="marketplace-list-item" onDownload={downloadProfile} />
        )
    }

    //
    const renderSubTitle = () => {
        return (
            <span onClick={onBack} className="px-2 btn btn-text-solo align-items-center auto-width ml-auto justify-content-end d-flex clickable hover" ><i className="material-icons">chevron_left</i>Back</span>
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
    //var caption = getMarketplaceCaption(item);
    const _caption = item != null && item.displayName != null ? item.displayName : id;
    const _title = `${_caption} | ${AppSettings.Titles.Main}`;
    const _typeCaption = item.type == null || item.type.name === null ? 'Smart Manufacturing App'
        : item.type.name.replace('SM ', 'Smart Manufacturing ');
    const _description = `SM Profile: ${_caption}. An SM Profile defines the Information Model 
                        for a manufacturing asset or process, with a goal to arrive at common, re-usable interfaces
                        for accessing data. ${AppSettings.MetaDescription.Abbreviated}`;

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
                {renderSchemaOrgContentMarketplaceItem(item)}
            </Helmet>
            {(!loadingProps.isLoading && !isLoading) &&
                <>
                    {renderHeaderRow()}
                    <div className="row" >
                    <div className="col-sm-9 m-auto mb-4" >
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

export default ProfileEntity;
