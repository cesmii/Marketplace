import React from 'react'
import { useHistory } from 'react-router-dom'
import { Button } from 'react-bootstrap'

import { formatItemPublishDate, getRandomArrayIndexes } from '../../utils/UtilityService';
import { RenderImageBg } from '../../services/MarketplaceService'
import { useLoadingContext } from '../../components/contexts/LoadingContext'
import { SvgVisibilityIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import { getViewByPublisherUrl } from '../../services/PublisherService';

//const CLASS_NAME = "MarketplaceItemRow";

function MarketplaceItemRow(props) { //props are item, showActions

    const history = useHistory();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const navigateToMarketplaceItem = (e) => {
        history.push({
            pathname: `/library/${props.item.name}`,
            state: { id: `${props.item.name}` }
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

    /*
    const renderMetaTags = (items, limit) => {
        if (items == null) return;
        return (
            items.map((tag, counter) => {
                if (counter < limit) {
                    return (
                        <span key={tag} className="metatag badge meta">
                            {tag}
                        </span>
                    )
                }
            })
        )
    }

    const renderCategoryTags = (items, limit) => {
        if (items == null) return;
        return (
            items.map((tag, counter) => {
                if (counter < limit) {
                    return (
                        <span key={tag.id} className="metatag badge meta">
                            {tag.name}
                        </span>
                    )
                }
            })
        )
    }
    */

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

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //TBD - improve this check
    if (props.item === null || props.item === {}) return null;
    if (props.item.name == null) return null;
    const url = (!props.item.isExternal ? `/library/${props.item.name}` : `/library/${props.item.externalSourceId.toLowerCase()}/${props.item.id}`)

    return (
        <>
            <div className={`row mx-0 p-0 ${props.cssClass}`}>
                <div className="col-sm-6 col-md-5 p-0" >
                    <RenderImageBg item={props.item} defaultImage={props.item.imagePortrait} responsiveImage={props.item.imageBanner} clickable={true} />
                </div>
                <div className="col-sm-6 col-md-7 p-4" >
                    <div className="d-flex align-items-center mb-2" >
                        <h2 className="mb-0" >{props.item.displayName}
                        </h2>
                        {(props.isAuthorized && !props.item.isExternal) &&
                            <a className="btn btn-icon-outline circle ml-auto" href={`/admin/library/${props.item.id}`} ><i className="material-icons">edit</i></a>
                        }
                    </div>
                    {props.item.abstract != null &&
                        <div className="mb-0" dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                    }
                    <p className="my-4" ><Button variant="secondary" type="button" className="px-4" href={url} >More Info</Button>
                    </p>
                    <p className="mb-2" ><b className="mr-2" >Published By:</b><a href={`/publisher/${props.item.publisher.name}`} >{props.item.publisher.displayName}</a></p>
                    {props.item.publisher?.allowFilterBy &&
                        <p className="mb-2 d-flex align-items-center" >
                            <SvgVisibilityIcon fill={color.link} />
                            <a href={getViewByPublisherUrl(loadingProps, props.item.publisher)} >View all by this publisher</a>
                        </p>
                    }
                    {props.item.publishDate != null &&
                        <p className="mb-3" ><b className="mr-2" >Published:</b>{formatItemPublishDate(props.item)}</p>
                    }
                    <div className="d-none d-lg-inline" >{renderIndustryVerticalItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderCategoryItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderMetaTagItem(props.item)}</div>
                </div>
            </div>
        </>
    );
}

export default MarketplaceItemRow;