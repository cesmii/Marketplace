import React from 'react'

//import { useAuthState } from "./authentication/AuthContext";
import { useLoadingContext } from '../../components/contexts/LoadingContext'
import { clearSearchCriteria, toggleSearchFilterSelected } from '../../services/MarketplaceService';
import { generateLogMessageString } from '../../utils/UtilityService';
import '../../components/styles/InfoPanel.scss';

const CLASS_NAME = "MarketplaceFilterSelected";
function MarketplaceFilterSelected(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //called when an item is selected in the filter panel
    const onItemClick = (e) => {

        var criteria = JSON.parse(JSON.stringify(loadingProps.searchCriteria));

        //loop through filters and their items and find the id
        var id = e.currentTarget.getAttribute('data-id');
        toggleSearchFilterSelected(criteria, id);

        //update state for other components to see
        setLoadingProps({ searchCriteria: criteria });

        //bubble up to parent component
        if (props.onFilterChange != null) props.onFilterChange();
    }

    const onClearAll = () => {
        console.log(generateLogMessageString('onClearAll', CLASS_NAME));

        //clear out the selected, the query val
        var criteria = clearSearchCriteria(loadingProps.searchCriteria);

        //this will trigger the API call
        //update state for other components to see
        setLoadingProps({ searchCriteria: criteria });

        //bubble up to parent component
        if (props.onFilterChange != null) props.onFilterChange();
    }

    const hasSelected = () => {
        if (loadingProps == null || loadingProps.searchCriteria == null
            || loadingProps.searchCriteria.filters == null || loadingProps.searchCriteria.length === 0) return false;

        //loop through filters and if we find any with a selected, we return true
        var selected = loadingProps.searchCriteria.filters.filter(parent => {
            return parent.items.filter(x => { return x.selected; }).length > 0;
        });

        //if we get here, nothing selected 
        return selected.length > 0;
    }


    //-------------------------------------------------------------------
    // Region: Render helpders
    //-------------------------------------------------------------------
    const renderSection = (section) => {
        //only list selected
        const choices = section.items.map((item) => {
            if (item.selected) {
                return (
                    <li key={`${section.enumValue}-${item.id}`} className="m-1 d-inline-block"
                        onClick={onItemClick} data-parentid={section.enumValue} data-id={item.id} >
                        <span className="selected p-0 px-2 d-flex" >{item.name}</span>
                    </li>
                )
            }
        });
        return (
            <ul key={`${section.name}-${section.enumValue}`} className="m-0 p-0 d-inline" >
                {choices}
            </ul>
        )
    }

    const renderSections = () => {
        if (loadingProps == null || loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null) return;

        const cards = loadingProps.searchCriteria.filters.map((item) => {
            return renderSection(item);
        });

        return (
            <>
                {cards}
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (!hasSelected()) return null;

    return (
        <div className="selected-panel px-3 py-3 mb-3 align-items-center rounded d-flex" >
            <div className="d-inline" >
                {renderSections()}
            </div>
            <div className="ml-auto justify-content-end text-nowrap" >
                <span onClick={onClearAll} className="px-2 clickable hover rounded d-flex align-items-center" >Clear All<i className="pl-1 material-icons">update</i></span>
            </div>
        </div>
    )

}

export default MarketplaceFilterSelected