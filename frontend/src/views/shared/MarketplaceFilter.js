import React, { useState, useEffect } from 'react'
import { Button } from 'react-bootstrap';

import { toggleSearchFilterSelected } from '../../services/MarketplaceService';
import { generateLogMessageString } from '../../utils/UtilityService';
import '../../components/styles/InfoPanel.scss';
import { SvgExpandLessIcon, SvgExpandMoreIcon } from '../../components/SVGIcon';
import color from '../../components/Constants';

const CLASS_NAME = "MarketplaceFilter";
function MarketplaceFilter(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_toggleState, setToggleState] = useState(null);
    //doing it this way so that the parent component can either use the global search criteria or use a local version unique to 
    //their component
    const [_searchCriteriaLocal, setSearchCriteriaLocal] = useState(props.searchCriteria);
    const _viewAllMax = 10;

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        //compare objects for reference equality
        if (props.searchCriteria !== _searchCriteriaLocal) {
            setSearchCriteriaLocal(props.searchCriteria);
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };

    }, [props.searchCriteria, _searchCriteriaLocal]);


    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    ////expand/collapse section
    const toggleChildren = (e) => {
        var id = e.currentTarget.getAttribute('data-id');
        var toggleState = _toggleState.find(x => { return x.enumValue.toString() === id; });
        toggleState.expanded = !toggleState.expanded;
        setToggleState(JSON.parse(JSON.stringify(_toggleState)));
    }

    //called when an item is selected in the filter panel
    const onItemClick = (e) => {

        var criteria = JSON.parse(JSON.stringify(_searchCriteriaLocal));

        //loop through filters and their items and find the id
        var id = e.currentTarget.getAttribute('data-id');
        toggleSearchFilterSelected(criteria, id);

        //bubble up to parent and they can choose to set the global search criteria
        if (props.onItemClick != null) props.onItemClick(criteria);
    }

    const toggleViewAll = (e) => {
        console.log(generateLogMessageString('toggleViewAll', CLASS_NAME));
        var id = e.currentTarget.getAttribute('data-id');
        var toggleState = _toggleState.find(x => { return x.enumValue.toString() === id; });
        toggleState.viewAll = !toggleState.viewAll;
        setToggleState(JSON.parse(JSON.stringify(_toggleState)));
    };

    //-------------------------------------------------------------------
    // Region: Render helpders
    //-------------------------------------------------------------------
    const renderSectionHeader = (caption, toggleState, itemCount) => {
        var toggleCss = `headline-3 ${toggleState.expanded ? "expanded" : ""}`;
        //var toggleIcon = toggleState.expanded ? "expand_less" : "expand_more";
        var toggleTitle = toggleState.expanded ? "collapse" : "expand";
        var svgToggleIcon = toggleState.expanded ?
            (<SvgExpandLessIcon color={color.textPrimary} /> )
            : (<SvgExpandMoreIcon color={color.textPrimary} />);
        // sectionKey = caption;
        return (
            <div className={toggleCss} onClick={toggleChildren} data-id={toggleState.enumValue} >
                <span key="caption" className="pr-2 w-100 d-block rounded">
                    {caption}
                </span>
                {(itemCount > 0) &&
                    <span key="toggle" className="ml-auto">
                    <Button variant="accordion" className="btn p-0" title={toggleTitle} >
                        <span className="d-flex align-items-center">
                            {svgToggleIcon}
                        </span>
                    </Button>
                    </span>
                }
            </div>
        )
    };

    const renderSection = (section) => {

        //get toggle state for this item
        var toggleState = _toggleState == null || _toggleState.length === 0 ? {
            enumValue: section.enumValue, name: section.name, expanded: true,
            viewAll: !props.showLimited || section.items.length <= _viewAllMax} :
            _toggleState.find(x => { return x.enumValue === section.enumValue; });

        const choices = section.items.map((item, counter) => {
            if (counter < _viewAllMax) {  //0-based
                return (
                    <li key={`${section.enumValue}-${item.id}`} className={`${props.selectMode == null ? "selectable" : props.selectMode} my-1 ${item.selected ? 'selected' : ''}`}
                        onClick={onItemClick} data-parentid={section.enumValue} data-id={item.id} >
                        <button className="btn section-item" title={item.selected ? `Remove ${item.name} filter` : `Filter by ${item.name}`} >
                            <span className="" >{item.name}</span>
                        </button>
                    </li>
                )
            }
            else {
                return (
                    <li key={`${section.enumValue}-${item.id}`} className={`${props.selectMode == null ? "selectable" : props.selectMode} my-1 ${item.selected ? 'selected' : ''} ${toggleState.viewAll ? '' : 'd-none'}`}
                        onClick={onItemClick} data-parentid={section.enumValue} data-id={item.id} >
                        <button className="btn section-item" title={item.selected ? `Remove ${item.name} filter` : `Filter by ${item.name}`} >
                            <span className="" >{item.name}</span>
                        </button>
                    </li>
                )
            }
        });

        return (
            <div key={`${section.name}-${section.enumValue}`} className="info-section mb-4 px-1 rounded">
                {renderSectionHeader(section.name, toggleState, section.items.length)}
                <ul className={toggleState.expanded ? "section-items m-0 pt-1 px-0" : "section-items collapsed m-0 p-0"} >
                    {choices}
                </ul>
                {(props.showLimited && section.items.length > _viewAllMax) &&
                    <button className="btn btn-link mr-2 ml-auto justify-content-end align-items-center d-flex" data-id={section.enumValue} onClick={toggleViewAll} >
                        {toggleState.viewAll ? '- See less' : '+ See all'}
                    </button>
                }
            </div>
        )
    }

    const renderSections = () => {
        if (_searchCriteriaLocal == null || _searchCriteriaLocal.filters == null) return;

        //init toggle state
        if (_toggleState == null) {
            var initialState = []
            _searchCriteriaLocal.filters.forEach((item) => {
                initialState.push({ enumValue: item.enumValue, id: item.name, expanded: true });
            });
            setToggleState(initialState);
        }

        const cards = _searchCriteriaLocal.filters.map((item) => {
            return renderSection(item);
        });

        return (
            <>
                {cards}
            </>
        );
    }



    return (
        <div className="info-panel" >
            {renderSections()}
        </div>
    )

}

export default MarketplaceFilter