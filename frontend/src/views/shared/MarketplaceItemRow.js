import React from 'react'
import { useHistory } from 'react-router-dom'
import { Button } from 'react-bootstrap'

import { formatItemPublishDate, getImageUrl, getRandomArrayIndexes } from '../../utils/UtilityService';
import { clearSearchCriteria, generateSearchQueryString, RenderImageBg, toggleSearchFilterSelected } from '../../services/MarketplaceService'
import { useLoadingContext } from '../../components/contexts/LoadingContext'
import { SvgVisibilityIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'

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

    const getViewByPublisherUrl = () => {

        //clear out the selected, the query val
        var criteria = clearSearchCriteria(loadingProps.searchCriteria);

        //loop through filters and their items and find the publisher id
        toggleSearchFilterSelected(criteria, props.item.publisher.id);

        //return url that will filter by publisher
        return `/library?${generateSearchQueryString(criteria, 1)}`;
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

    return (
        <>
            <div className={`row mx-0 p-0 ${props.cssClass}`}>
                <div className="col-sm-6 col-md-5 p-0" >
                    <RenderImageBg item={props.item} defaultImage={props.item.imagePortrait} responsiveImage={props.item.imageBanner} />
                </div>
                <div className="col-sm-6 col-md-7 p-4" >
                    <div className="d-flex align-items-center mb-2" >
                        <h2 className="mb-0" >{props.item.displayName}
                        </h2>
                        {(props.isAuthorized) &&
                            <a className="btn btn-icon-outline circle ml-auto" href={`/admin/library/${props.item.id}`} ><i className="material-icons">edit</i></a>
                        }
                    </div>
                    {props.item.abstract != null &&
                        <div className="mb-0" dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                    }
                    <p className="my-4" ><Button variant="secondary" type="button" className="px-4" href={`/library/${props.item.name}`} >More Info</Button>
                    </p>
                    <p className="mb-2" ><b className="mr-2" >Published By:</b><a href={`/publisher/${props.item.publisher.name}`} >{props.item.publisher.displayName}</a></p>
                    <p className="mb-2 d-flex align-items-center" >
                        <SvgVisibilityIcon fill={color.link} />
                        <a href={getViewByPublisherUrl()} >View all by this publisher</a>
                    </p>
                    <p className="mb-3" ><b className="mr-2" >Published:</b>{formatItemPublishDate(props.item)}</p>
                    <div className="d-none d-lg-inline" >{renderIndustryVerticalItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderCategoryItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderMetaTagItem(props.item)}</div>
                </div>
            </div>
        </>
    );
}

export default MarketplaceItemRow;