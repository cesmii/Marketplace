import { generateLogMessageString, getUserPreferences, setUserPreferences } from '../utils/UtilityService';
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
export function clearSearchCriteria(criteria) {

    var result = JSON.parse(JSON.stringify(criteria));

    //loop over parents then over children and set selected to false
    result.filters.forEach(parent => {
        parent.items.forEach(item => {
            item.selected = false;
        });
    });

    //loop over item types and set selected to false
    result.itemTypes?.forEach(item => {
        item.selected = false;
    });

    result.query = null;
    result.skip = 0;
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

export function MarketplaceRelatedItems(props) {
    //-------------------------------------------------------------------
    // Common render helpers
    //-------------------------------------------------------------------
    //-------------------------------------------------------------------
    // Render Specifications for a profile or marketplace item
    //-------------------------------------------------------------------
    const renderSpecifications = (item) => {
        if ((item.requiredItems == null || item.requiredItems.length === 0) &&
            (item.recommendedItems == null || item.recommendedItems.length === 0)) return;

        return (
            <>
                {(item.requiredItems != null && item.requiredItems.length > 0) &&
                    <>
                        <div className="row" >
                            <div className="col-sm-12 mb-3" >
                                <h3 className="m-0 small">
                                    Required SM Apps, SM Hardware & SM Profiles
                                </h3>
                            </div>
                        </div>
                        <div className="row" >
                            <div className="col-sm-12">
                                <MarketplaceTileList items={item.requiredItems} layout="banner-abbreviated" colCount={3} />
                            </div>
                        </div>
                    </>
                }
                {(item.recommendedItems != null && item.recommendedItems.length > 0) &&
                    <>
                        <div className="row" >
                            <div className="col-sm-12 my-3 pt-3 border-top" >
                                <h3 className="m-0 small">
                                    Recommended SM Apps, SM Hardware & SM Profiles
                                </h3>
                            </div>
                        </div>
                        <div className="row" >
                            <div className="col-sm-12">
                                <MarketplaceTileList items={item.recommendedItems} layout="banner-abbreviated" colCount={3} />
                            </div>
                        </div>
                    </>
                }
            </>
        );
    }

    //-------------------------------------------------------------------
    // Render Similar Items for a profile or marketplace item
    //-------------------------------------------------------------------
    const renderSimilarItems = (item) => {

        if (item.similarItems == null || item.similarItems.length === 0) return;

        return (
            <>
                <div className="row" >
                    <div className="col-sm-12">
                        <MarketplaceTileList items={item.similarItems} layout="banner" colCount={3} />
                    </div>
                </div>
            </>
        );
    }

    //-------------------------------------------------------------------
    // Render
    //-------------------------------------------------------------------
    if (props.displayMode === "similarItems") {
        return (renderSimilarItems(props.item))
    }
    if (props.displayMode === "specifications") {
        return (renderSpecifications(props.item))
    }
    return null;
}