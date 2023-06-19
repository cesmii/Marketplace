import React from 'react'
import { useHistory } from 'react-router-dom'

import SocialMedia from '../../components/SocialMedia';
import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { clearSearchCriteria, generateSearchQueryString, toggleSearchFilterSelected } from '../../services/MarketplaceService';
import { generateLogMessageString } from '../../utils/UtilityService';

import { SvgVisibilityIcon } from '../../components/SVGIcon';
import color from '../../components/Constants';
import '../../components/styles/InfoPanel.scss';

const CLASS_NAME = "PublisherSidebar";
function PublisherSidebar(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const getViewByPublisherUrl = () => {

        //clear out the selected, the query val
        var criteria = clearSearchCriteria(loadingProps.searchCriteria);

        //loop through filters and their items and find the publisher id
        toggleSearchFilterSelected(criteria, props.item.id);

        //return url that will filter by publisher
        return `/library?${generateSearchQueryString(criteria, 1)}`;
    };


    //-------------------------------------------------------------------
    // Region: Render helpders
    //-------------------------------------------------------------------
    if (props.item == null) return null;

    return (
        <div className="row mb-2" >
            <div className="col-sm-12 mb-2">
                <p className="mb-2 headline-3 p-1 px-2 w-100 d-block rounded">{ props.caption == null ? "Publisher" : props.caption }</p>
                <p className="mb-2 px-2"><a href={`/publisher/${props.item.name}`} >{props.item.displayName}</a></p>
                <p className="mb-0 px-2">
                    <span className="mr-1" alt="view"><SvgVisibilityIcon fill={color.link} /></span>
                    <a href={getViewByPublisherUrl()} >View all by this publisher</a>
                </p>
            </div>
            <div className="col-sm-12">
                <SocialMedia items={props.item.socialMediaLinks} />
            </div>
        </div>
    )

}

export default PublisherSidebar