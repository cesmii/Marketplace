import { useHistory } from 'react-router-dom'
import { AppSettings } from '../utils/appsettings';

import { generateLogMessageString, getImageUrl, getUserPreferences, setUserPreferences } from '../utils/UtilityService';
import MarketplaceTileList from '../views/shared/MarketplaceTileList';

const CLASS_NAME = "MarketplaceService";

//-------------------------------------------------------------------
// getMarketplacePreferences, setMarketplacePageSize - get/set commonly shared user preferences for a profile (ie page size)
//-------------------------------------------------------------------
export function getMarketplacePreferences() {
    var item = getUserPreferences();
    return item.marketplacePreferences;
}

export function setMarketplacePageSize(val) {
    var item = getUserPreferences();
    item.marketplacePreferences.pageSize = val;
    setUserPreferences(item);
}

//-------------------------------------------------------------------
// Common search criteria helpers
//-------------------------------------------------------------------
//Clear out all search criteria values
export function clearSearchCriteria(criteria, keepSkip) {

    var result = JSON.parse(JSON.stringify(criteria));
    if (result == null) result = {};

    //loop over parents then over children and set selected to false
    result?.filters?.forEach(parent => {
        parent.items.forEach(item => {
            item.selected = false;
        });
    });

    //loop over item types and set selected to false
    result?.itemTypes?.forEach(item => {
        item.selected = false;
    });

    if (result.query != null) {
        result.query = null;
    }
    if (!keepSkip) {
        result.skip = 0;
    }
    return result;
}

//Find a filter item and set the selected value
export function toggleSearchFilterSelected(criteria, id) {

    //loop through filters and their items and find the id
    var item = null;
    //note it won't stop the foreach loop even if it finds it. Account for that.
    criteria.filters.forEach(parent => {
        if (item != null) return;
        item = parent.items.find(x => { return x.id.toString() === id; });
    });
    if (item == null) {
        console.warn(generateLogMessageString(`toggleSearchFilterValue||Could not find item with id: ${id} in lookup data`, CLASS_NAME));
        return;
    }
    //toggle the selection or set for initial scenario
    item.selected = !item.selected;
}

//-------------------------------------------------------------------
// Region: Generate a new query string based on the selections
//-------------------------------------------------------------------
export function generateSearchQueryString (criteria, currentPage) {
    let result = [];
    //query
    if (criteria?.query != null && criteria.query !== '') {
        result.push(`q=${criteria.query}`);
    }
    //sm types
    if (criteria?.itemTypes != null) {
        const selTypes = criteria.itemTypes.filter(x => x.selected).map(x => x.code);
        if (selTypes != null && selTypes.length > 0) {
            result.push(`sm=${selTypes.join(',')}`);
        }
    }
    //verts, processes, etc. 
    if (criteria?.filters != null) {
        let resultFilters = [];
        criteria.filters.forEach((x) => {
            const selFilters = x.items.filter(x => x.selected).map(x => x.id);
            if (selFilters != null && selFilters.length > 0) {
                resultFilters.push(`${x.enumValue}::${selFilters.join(',')}`);
            }
        });
        if (resultFilters.length > 0) {
            result.push(`f=${resultFilters.join('|')}`);
        }
    }
    //page
    result.push(`p=${currentPage == null ? 0 : currentPage}`);

    //page size
    if (criteria != null) {
        result.push(`t=${criteria.take}`);
    }
    return result.join('&');
}

//-------------------------------------------------------------------
// Web part - Render Related Items - used by marketplace entity or profile entity
//-------------------------------------------------------------------
export function MarketplaceRelatedItems(props) {
    //-------------------------------------------------------------------
    // Common render helpers
    //-------------------------------------------------------------------
    //-------------------------------------------------------------------
    // Render Similar Items for a profile or marketplace item
    //-------------------------------------------------------------------
    const renderSections = (items) => {
        return items.map((itm, i) => {
            const key = `${itm.relatedType?.code}-${i}`;
            const collapseTargetId = `collapse-${key}`;
            return (
                <div key={key} className="card mb-0">
                    <div className="card-header bg-transparent p-0 border-bottom-0" id={`heading-${i}-${itm.relatedType?.code}`} >
                        <button className="btn btn-content-accordion p-3 py-2 text-left d-block w-100" type="button" data-toggle="collapse" data-target={`#${collapseTargetId}`} aria-expanded="false" aria-controls={`${collapseTargetId}`} >
                            <h3 className="mb-0">
                                {itm.relatedType?.name}
                            </h3>
                        </button>
                    </div>
                    <div id={`${collapseTargetId}`} className="collapse mb-0" aria-labelledby={`heading-${i}-${itm.relatedType?.code}`} >
                        <div className="card-body">
                            <div className="row" >
                                <div className="col-sm-12">
                                    <MarketplaceTileList items={itm.items} layout="banner-abbreviated" colCssClass="col-lg-3 col-md-6" />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            );
        });
    }

    //-------------------------------------------------------------------
    // Render
    //-------------------------------------------------------------------
    //props.items == relatedItemsGrouped

    if (props.items == null || props.items.length === 0) return;

    return (
        <div className="accordion" >
            {renderSections(props.items)}
        </div>
    );

}


//-------------------------------------------------------------------
// Web part - Render Image BG for featured panel, marketplace item row, marketplace item header
//-------------------------------------------------------------------
export function RenderImageBg (props) {

    const history = useHistory();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const navigateToMarketplaceItem = (e) => {
        if (props.itemType != null && props.itemType === AppSettings.itemTypeCode.smProfile) {
            history.push({
                pathname: `/${props.item.externalSource.code}/profile/${props.item.id}`,
                state: { id: `${props.item.id}` }
            });
        }
        else if (props.item.isExternal) {
            history.push({
                pathname: `/library/${props.item.externalSource.code.toLowerCase()}/${props.item.id}`,
                state: { id: `${props.item.id}` }
            });
        }
        else {
            history.push({
                pathname: `/library/${props.item.name}`,
                state: { id: `${props.item.name}` }
            });
        }
    };

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

    /*
        Example of displaying diff images based on screen size.
        <div className="d-flex align-items-center mb-2" >
            <picture>
                <source srcset={getImageUrl(props.item.imageLandscape)} media="(max-width: 767px)" />
                <img src={getImageUrl(props.item.imagePortrait)} alt="" />
            </picture>
        </div>
     */
    const bgImageStyleDefault = props.defaultImage == null ? {} :
        {
            backgroundImage: `url(${getImageUrl(props.defaultImage)})`
        };

    const bgImageStyleResponsive = props.responsiveImage == null ? {} :
        {
            backgroundImage: `url(${getImageUrl(props.responsiveImage)})`
        };

    //backward compatability - banner null, transpose the portrait and show as a banner
    //if banner not null, then show both images and hide visiblity based on screen size
    return (
        <>
            <div className={`image-bg ${props.responsiveImage != null ? 'd-none d-sm-block' : 'transpose-image-horizontal-sm'}`} >
                <div className={`overlay-icon cover ${props.clickable ? 'clickable' : ''} `} style={bgImageStyleDefault} onClick={navigateToMarketplaceItem} >&nbsp;</div>
            </div>
            {props.responsiveImage != null &&
                <div className="image-bg d-block d-sm-none transpose-image-horizontal-sm" >
                    <div className="overlay-icon cover clickable" style={bgImageStyleResponsive} onClick={navigateToMarketplaceItem} >&nbsp;</div>
                </div>
            }
        </>
    )
};
