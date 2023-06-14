import React, { useEffect, useState } from 'react'

import { generateLogMessageString } from '../utils/UtilityService';
import './styles/InfoPanel.scss';

const CLASS_NAME = "MultiSelect";
function MultiSelect(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_items, setItems] = useState([]);

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        setItems(props.items);

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||props.items||Cleanup', CLASS_NAME));
        };
    }, [props.items]);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //called when an item is selected in the panel
    const onItemSelect = (e) => {
        console.log(generateLogMessageString('onItemSelect', CLASS_NAME));

        var itemsCopy = JSON.parse(JSON.stringify(_items));

        //loop through filters and their items and find the id
        var id = e.currentTarget.getAttribute('data-id');
        var match = itemsCopy.find(x => { return x.id.toString() === id; });
        if (match != null) {
            match.selected = !match.selected;
        }
        console.log(generateLogMessageString(`onItemSelect||${match.name}||Selected: ${match.selected}`, CLASS_NAME));

        //update state for other components to see
        if (props.onItemSelect != null) {
            props.onItemSelect(match);
        }
    }

    //-------------------------------------------------------------------
    // Region: Render helpders
    //-------------------------------------------------------------------
    const renderSectionHeader = (caption) => {
        var headerCss = `headline-3`;
        // sectionKey = caption;
        return (
            <div className={headerCss} >
                <span key="caption" className="pr-2 w-100 d-block rounded">
                    {caption}
                </span>
            </div>
        )
    };

    const renderSection = (caption, items) => {

        const choices = items.map((item) => {
            return (
                <li key={`${item.id}`} className={item.selected ? 'selected my-2 selectable' : 'my-2 selectable'}
                    onClick={onItemSelect} data-id={item.id} >
                    <span className="section-item" >{item.name}</span>
                </li>
            )
        });
        return (
            <div key={`${caption.replace(' ', '-')}`} className="info-section mb-4 px-1 rounded">
                {renderSectionHeader(caption)}
                <ul className="section-items m-0 px-0" >
                    {choices}
                </ul>
            </div>
        )
    }

    if (_items == null) return null;

    return (
        <div className={`multi-select info-panel ${props.className == null ? '' : props.className}`} >
            {renderSection(props.caption, _items)}
        </div>
    )

}

export default MultiSelect