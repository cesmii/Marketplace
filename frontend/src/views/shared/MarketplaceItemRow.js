import React from 'react'
import { useHistory } from 'react-router-dom'
import { Button } from 'react-bootstrap'

import { formatDate, generateLogMessageString, getImageUrl } from '../../utils/UtilityService';
import { clearSearchCriteria, toggleSearchFilterSelected } from '../../services/MarketplaceService'
import { useLoadingContext } from '../../components/contexts/LoadingContext'
import { SvgVisibilityIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'

const CLASS_NAME = "MarketplaceItemRow";

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

    const filterByPublisher = (e) => {

        e.preventDefault();
        console.log(generateLogMessageString('filterByPublisher', CLASS_NAME));

        //clear out the selected, the query val
        var criteria = clearSearchCriteria(loadingProps.searchCriteria);

        //loop through filters and their items and find the publisher id
        toggleSearchFilterSelected(criteria, props.item.publisher.id);

        //update state for other components to see
        setLoadingProps({ searchCriteria: criteria });

        //navigate to marketplace list
        history.push({pathname: `/library`});
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderMetaTagItem = (item) => {
        if (item.metaTags == null) return;
        return (
            item.metaTags.map((tag) => {
                return (
                    <span key={tag} className="metatag badge meta">
                        {tag}
                    </span>
                )
            })
        )
    }
    const renderCategoryItem = (item) => {
        return (
            item.categories.map((tag) => {
                return (
                    <span key={tag.id} className="metatag badge meta">
                        {tag.name}
                    </span>
                )
            })
        )
    }
    const renderIndustryVerticalItem = (item) => {
        return (
            item.industryVerticals.map((tag) => {
                return (
                    <span key={tag.id} className="metatag badge meta">
                        {tag.name}
                    </span>
                )
            })
        )
    }

    const renderImageBg = () => {

        /*
        var imgSrc = props.item.imagePortrait == null ? "" : getImageUrl(props.item.imagePortrait);
        return (
            <div className="image-bg" >
                <div className="clickable d-flex" onClick={navigateToMarketplaceItem} >
                    <img className="z" src={imgSrc} alt={`${props.item.name}-${getImageAlt(props.item.imagePortrait)}`} />
                </div>
            </div>
        );
        */
        var bgImageStyle = props.item.imagePortrait == null ? {} :
            {
                backgroundImage: `url(${getImageUrl(props.item.imagePortrait)})`
            };

        return (
            <div className="image-bg" >
                <div className="overlay-icon cover clickable" style={bgImageStyle} onClick={navigateToMarketplaceItem} >&nbsp;</div>
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
                        {(props.currentUserId != null) &&
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
                        <button className="btn btn-link" onClick={filterByPublisher} >
                            View all by this publisher</button>
                    </p>
                    <p className="mb-3" ><b className="mr-2" >Published:</b>{formatDate(props.item.publishDate)}</p>
                    <div className="d-none d-lg-inline" >{renderIndustryVerticalItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderCategoryItem(props.item)}</div>
                    <div className="d-none d-lg-inline" >{renderMetaTagItem(props.item)}</div>
                </div>
            </div>
        </>
    );
}

export default MarketplaceItemRow;