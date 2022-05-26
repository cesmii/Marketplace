import React, { useEffect, useState }from 'react'

import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { generateLogMessageString } from '../../utils/UtilityService';

import '../../components/styles/InfoPanel.scss';

const CLASS_NAME = "MarketplaceItemTypeFilter";
function MarketplaceItemTypeFilter(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_selectAll, setSelectAll] = useState(true);

    //-------------------------------------------------------------------
    // Region: useEffect
    //-------------------------------------------------------------------
    useEffect(() => {

        if ((props.searchCriteria == null || props.searchCriteria.itemTypes == null) && loadingProps.refreshSearchCriteria) {
            //do nothing & wait for the refreshSearchCriteria action to finish, it means some other component already requested we fetch the criteria
            return;
        }
        //trigger fetch of search criteria data
        else if (props.searchCriteria == null || props.searchCriteria.itemTypes == null) {
            setLoadingProps({ refreshSearchCriteria: true });
            return;
        }
        //init the select All - default to true unless there are some item types selected
        setSelectAll(props.searchCriteria.itemTypes.filter(x => { return x.selected; }).length === 0);

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };

    }, [props.searchCriteria]);

    //-------------------------------------------------------------------
    // Region: Helper Methods
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onToggleAll = () => {
        //if you click all 
        var criteria = JSON.parse(JSON.stringify(props.searchCriteria));

        //only 
        if (!_selectAll) {
            //loop through types and toggle off selection. 
            criteria.itemTypes.forEach(item => { item.selected = false; });

            setSelectAll(true);
            //bubble up to parent component and it will save state
            if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
        }
    }

    //called when an item is selected in the filter panel
    const onToggleSelection = (e) => {

        var criteria = JSON.parse(JSON.stringify(props.searchCriteria));

        //loop through types and toggle selection. 
        var id = e.currentTarget.getAttribute('data-id');
        var item = criteria.itemTypes.find(x => { return x.id.toString() === id; });
        if (item == null) {
            console.warn(generateLogMessageString(`onToggleSelection||Could not find item with id: ${id} in search criteria`, CLASS_NAME));
            return;
        }
        //toggle the selection or set for initial scenario
        item.selected = !item.selected;

        //update selectAll based on number of items selected
        setSelectAll(props.searchCriteria.itemTypes.filter(x => { return x.selected; }).length === 0);

        //bubble up to parent component and it will save state
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
    }

    //-------------------------------------------------------------------
    // Region: Render helpders
    //-------------------------------------------------------------------
    const renderSections = () => {
        if (props.searchCriteria == null || props.searchCriteria.itemTypes == null ) {
            return;
        }
        
        const choices = props.searchCriteria.itemTypes.map((item) => {
            return (
                <li id={`${item.id}`} key={`${item.id}`} className="m-1 d-inline-block"
                    onClick={onToggleSelection} data-id={item.id} >
                    <span className={`${item.selected ? "selected" : "not-selected"} py-1 px-3 d-flex toggle`} >{item.name}</span>
                </li>
            )
        });

        return (
            <ul className="m-0 p-0 d-inline" >
                <li id={`none`} key={`none`} className="m-1 d-inline-block"
                    onClick={onToggleAll} >
                    <span className={`${_selectAll ? "selected" : "not-selected"} py-1 px-3 d-flex`} >All</span>
                </li>
                {choices}
            </ul>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    //if (!hasSelected()) return null;
    return (
        <div className={`type-filter-panel px-2 py-1 mb-1 rounded d-flex ${props.cssClass ?? ''}`} >
            <div className="px-0 align-items-start d-block d-lg-flex align-items-center" >
                <div className="d-block d-lg-inline mb-2 mb-lg-0" >
                    {renderSections()}
                </div>
            </div>
        </div>
    )

}

export default MarketplaceItemTypeFilter