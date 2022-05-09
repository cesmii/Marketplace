import React from 'react'
import { useHistory } from 'react-router-dom'
import { Button } from 'react-bootstrap'

import { formatDate, getImageUrl, getRandomArrayIndexes } from '../../utils/UtilityService';

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

        var randomIndexes = getRandomArrayIndexes(items, limit);
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

        var randomIndexes = getRandomArrayIndexes(items, limit);
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

    const renderImageBg = () => {

        var bgImageStyle = props.item.imagePortrait == null ? {} :
            {
                backgroundImage: `url(${getImageUrl(props.item.imagePortrait)})`
            };

        return (
            <div className="image-bg" >
                <div className="overlay-icon cover clickable" style={bgImageStyle} onClick={navigateToItem} >&nbsp;</div>
            </div>
        );
    };

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
                <div className="col-sm-6 col-md-5 p-0 d-none d-sm-block" >
                    {renderImageBg()}
                </div>
                <div className="col-sm-6 col-md-7 p-4" >
                    <div className="d-flex align-items-center mb-2" >
                        <h2 className="mb-0" >{props.item.displayName}
                        </h2>
                    </div>
                    {props.item.abstract != null &&
                        <div className="mb-0" dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                    }
                    <p className="my-4" ><Button variant="secondary" type="button" className="px-4" href={`/profile/${props.item.id}`} >More Info</Button>
                    </p>
                    <p className="mb-2" ><b className="mr-2" >Namespace:</b><a href={props.item.namespace} target="_blank" rel="noreferrer" >{props.item.namespace}</a></p>
                    <p className="mb-2" ><b className="mr-2" >Published By:</b>{props.item.publisher.displayName}</p>
                    <p className="mb-2" ><b className="mr-2" >Published:</b>{formatDate(props.item.publishDate)}</p>
                    <p className="mb-2" ><b className="mr-2" >Version:</b>{props.item.version}</p>
                    <div className="d-none d-lg-inline" >{renderIndustryVerticalItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderCategoryItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderMetaTagItem(props.item)}</div>
                </div>
            </div>
        </>
    );
}

export default ProfileItemRow;