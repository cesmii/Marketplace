import React from 'react'
import { useHistory } from 'react-router-dom'
import { Button } from 'react-bootstrap'

import { formatItemPublishDate, getImageUrl, getRandomArrayIndexes } from '../../utils/UtilityService';
import { RenderImageBg } from '../../services/MarketplaceService';
import { AppSettings } from '../../utils/appsettings';

//const CLASS_NAME = "ProfileItemRow";

function ProfileItemRow(props) { //props are item, showActions

    const history = useHistory();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const navigateToItem = (e) => {
        history.push({
            pathname: `/profile/${props.item.id}`,
            state: { id: `${props.item.id}` }
        });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderMetaTagsRandom = (items, limit) => {
        if (items == null) return;

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

    const renderCategoryTagsRandom = (items, limit) => {
        if (items == null) return;

        const randomIndexes = getRandomArrayIndexes(items, limit);
        if (randomIndexes == null || randomIndexes.length === 0) return;
        return (
            randomIndexes.map((i) => {
                return (
                    <span key={items[i].id} className="metatag badge meta border">
                        {items[i].name}
                    </span>
                )
            })
        )
    }

    const renderMetaTagItem = (item) => {
        if (item == null || item.metaTags == null) return;
        //return renderMetaTags(item.metaTags, 6);
        return renderMetaTagsRandom(item.metaTags, 3);
    }

    const renderCategoryItem = (item) => {
        if (item == null || item.categories == null) return;
        //return renderCategoryTags(item.categories, 6);
        return renderCategoryTagsRandom(item.categories, 3);
    }
    const renderIndustryVerticalItem = (item) => {
        if (item == null || item.industryVerticals == null) return;
        //return renderCategoryTags(item.industryVerticals, 6);
        return renderCategoryTagsRandom(item.industryVerticals, 3);
    }

    const renderImageCompact = () => {

        const style = 
            {
                width: '100%'
            };

        return (
            <img src={getImageUrl(props.item.imagePortrait)} className="p-0" style={style} onClick={navigateToItem} alt={`icon-profile-${props.item.displayName}`} />
        );
    };


    //-------------------------------------------------------------------
    // Render row normal - more comparable to marketplace app rows
    const renderRow = () => {
        return (
            <div className={`row mx-0 p-0 ${props.cssClass}`}>
                <div className="col-sm-5 p-0" >
                    <RenderImageBg item={props.item} defaultImage={props.item.imagePortrait} responsiveImage={props.item.imageBanner} itemType={AppSettings.itemTypeCode.smProfile} clickable={true} />
                </div>
                <div className="col-sm-7 p-4" >
                    <div className="d-flex align-items-center mb-2" >
                        <h2 className="mb-0" >SM Profile: {props.item.displayName}</h2>
                    </div>
                    {props.item.abstract != null &&
                        <div className="mb-0" dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                    }
                    <p className="my-4" ><Button variant="secondary" type="button" className="px-4" href={`/profile/${props.item.externalSource.code}/${props.item.id}`} >More Info</Button>
                    </p>
                    {(props.item.namespace != null && props.item.namespace !== '') &&
                        <p className="mb-2" ><b className="mr-2" >Namespace:</b>
                            <span style={{ wordBreak: "break-word" }} >{props.item.namespace}</span>
                        </p>
                    }
                    <p className="mb-2" ><b className="mr-2" >Published By:</b>{props.item.publisher.displayName}</p>
                    <p className="mb-2" ><b className="mr-2" >Published:</b>{formatItemPublishDate(props.item)}</p>
                    <p className="mb-2" ><b className="mr-2" >Version:</b>{props.item.version}</p>
                    <div className="d-none d-lg-inline" >{renderIndustryVerticalItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderCategoryItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderMetaTagItem(props.item)}</div>
                </div>
            </div>
        );
    };

    //-------------------------------------------------------------------
    // Render row compact - smaller, less info, less lines
    const renderRowCompact = () => {
        return (
            <div className={`row mx-0 p-0 py-3 ${props.cssClass} ${props.displayMode}`}>
                <div className="col-2 col-md-1" >
                    {renderImageCompact()}
                </div>
                <div className="col-10 col-md-11 d-flex" >
                    <div className="d-inline">
                        <h2 className="mb-2" >SM Profile: {props.item.displayName}</h2>
                        {(props.item.namespace != null && props.item.namespace !== '') &&
                            <p className="mb-1" ><b className="mr-2" >Namespace:</b>
                                <span style={{ wordBreak: "break-word" }} >{props.item.namespace}</span>
                            <span className="ml-2" >(v.{props.item.version}) </span>
                            </p>
                        }
                        <p className="mb-0" >
                            <b className="mr-2" >Published By:</b>{props.item.publisher.displayName},
                            <b className="mx-2" >on:</b>{formatItemPublishDate(props.item)}
                        </p>
                    </div>
                    <div className="ml-auto" >
                        <Button variant="secondary" type="button" className="text-nowrap" href={`/profile/${props.item.externalSource.code}/${props.item.id}`} >More Info</Button>
                    </div>
                </div>
            </div>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (props.item === null || props.item === {}) return null;
    if (props.item.name == null) return null;

    return props.displayMode === 'compact' ?
            renderRowCompact() : renderRow();
}

export default ProfileItemRow;