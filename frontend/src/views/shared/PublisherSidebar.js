import React from 'react'
import { useHistory } from 'react-router-dom'

import SocialMedia from '../../components/SocialMedia';
import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { clearSearchCriteria, toggleSearchFilterSelected } from '../../services/MarketplaceService';
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
    const filterByPublisher = (e) => {

        e.preventDefault();
        console.log(generateLogMessageString('filterByPublisher', CLASS_NAME));

        //clear out the selected, the query val
        var criteria = clearSearchCriteria(loadingProps.searchCriteria);

        //loop through filters and their items and find the publisher id
        toggleSearchFilterSelected(criteria, props.item.id);

        //update state for other components to see
        setLoadingProps({ searchCriteria: criteria });

        //navigate to marketplace list
        history.push({ pathname: `/library` });
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
                <p className="mb-0 px-2"><button className="btn btn-link" onClick={filterByPublisher} ><span className="mr-1" alt="view"><SvgVisibilityIcon fill={color.link} /></span>All by this publisher</button></p>
            </div>
            <div className="col-sm-12">
                <SocialMedia items={props.item.socialMediaLinks} />
            </div>
        </div>
    )

}

export default PublisherSidebar