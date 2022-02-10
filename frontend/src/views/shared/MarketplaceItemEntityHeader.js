import React from 'react'

import { formatDate, getImageUrl } from '../../utils/UtilityService';

//const CLASS_NAME = "MarketplaceItemEntityHeader";

function MarketplaceItemEntityHeader(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderImageBg = () => {

        var bgImageStyle = props.item.imageLandscape == null ? {} :
            {
                backgroundImage: `url(${getImageUrl(props.item.imageLandscape)})`
        };

        return (
            <div className="image-bg" >
                <div className="overlay-icon contain" style={bgImageStyle} >&nbsp;</div>
            </div>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (props.item === null || props.item === {}) return null;
    if (props.item.name == null) return null;

    return (
        <>
            <div className={`row mx-0 p-0 ${props.cssClass} mb-4`}>
                <div className="col-sm-6 col-md-5 p-0 d-none d-sm-block" >
                    { renderImageBg()}
                </div>
                <div className="col-sm-6 col-md-7 p-4" >
                    {/*<h2>{props.item.name}</h2>*/}
                    {props.item.abstract != null &&
                        <div className="mb-2" dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                    }
                    <p className="mb-0" ><b className="mr-2" >Published:</b>{formatDate(props.item.publishDate)}</p>
                    {/*<div className="d-none d-lg-inline" >{renderIndustryVerticalItem(props.item)}</div>*/}
                    {/*<div className="d-none d-lg-inline" >{renderCategoryItem(props.item)}</div>*/}
                    {/*<div className="d-none d-lg-inline" >{renderMetaTagItem(props.item)}</div>*/}
                </div>
            </div>
        </>
    );
}

export default MarketplaceItemEntityHeader;