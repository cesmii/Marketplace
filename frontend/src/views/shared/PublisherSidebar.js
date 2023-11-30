import React from 'react'

import SocialMedia from '../../components/SocialMedia';
import { useLoadingContext } from '../../components/contexts/LoadingContext';
//import { generateLogMessageString } from '../../utils/UtilityService';

import { SvgVisibilityIcon } from '../../components/SVGIcon';
import color from '../../components/Constants';
import '../../components/styles/InfoPanel.scss';
import { getViewByPublisherUrl } from '../../services/PublisherService';

//const CLASS_NAME = "PublisherSidebar";
function PublisherSidebar(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpders
    //-------------------------------------------------------------------
    if (props.item == null) return null;

    return (
        <div className="row mb-2" >
            <div className="col-sm-12 mb-2">
                <p className="mb-2 headline-3 p-1 px-2 w-100 d-block rounded">{ props.caption == null ? "Publisher" : props.caption }</p>
                <p className="mb-2 px-2"><a href={`/publisher/${props.item.name}`} >{props.item.displayName}</a></p>
                {props.item.allowFilterBy &&
                <p className="mb-0 px-2">
                    <span className="me-1" alt="view"><SvgVisibilityIcon fill={color.link} /></span>
                    <a href={getViewByPublisherUrl(loadingProps, props.item)} >View all by this publisher</a>
                </p>
                }
            </div>
            <div className="col-sm-12">
                <SocialMedia items={props.item.socialMediaLinks} />
            </div>
        </div>
    )

}

export default PublisherSidebar