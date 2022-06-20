import React, { useState } from 'react'

import { generateLogMessageString} from '../../utils/UtilityService'
import '../../components/styles/InfoPanel.scss';

const CLASS_NAME = "MarketplaceEntitySidebar";

function MarketplaceEntitySidebar(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _viewAllMax = 7;
    const [_viewAll, setViewAll] = useState({
        industryVerticals: props.item.industryVerticals == null || props.item.industryVerticals.length <= _viewAllMax,
        categories: props.item.categories == null || props.item.categories.length <= _viewAllMax,
        metaTags: props.item.metaTags == null || props.item.metaTags.length <= _viewAllMax
    });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const toggleViewAll = (e) => {
        console.log(generateLogMessageString('toggleViewAll', CLASS_NAME));
        var enumValue = parseInt(e.currentTarget.getAttribute("data-enumvalue"));

        switch (enumValue) {
            case 1:
                setViewAll({ ..._viewAll, industryVerticals: !_viewAll.industryVerticals });
                return;
            case 2:
                setViewAll({ ..._viewAll, categories: !_viewAll.categories });
                return;
            case 3:
                setViewAll({ ..._viewAll, metaTags: !_viewAll.metaTags });
                return;
            default:
                return;
        }
    };


    //-------------------------------------------------------------------
    // Region: Render helpders
    //-------------------------------------------------------------------
    const renderSectionHeader = (caption) => {
        return (
            <div className="headline-3" >
                <span key="caption" className="caption">
                    {caption}
                </span>
            </div>
        )
    };

    const renderSection = (section) => {
        if (section.items == null || section.items.length === 0) return;

        const choices = section.items.map((item, counter) => {
            if (counter < _viewAllMax) {  //0-based
                return (
                    <li key={`${section.enumValue}-${item.id}`} className='my-1 tag' >
                        <span className="section-item">{item.name}</span>
                    </li>
                )
            }
            else {
                return (
                    <li key={`${section.enumValue}-${item.id}`} className={`my-1 tag ${section.viewAll ? '' : 'd-none'}`} >
                        <span className="section-item">{item.name}</span>
                    </li>
                )
            }
        });

        return (
            <div key={`${section.name}-${section.enumValue}`} className="mb-4">
                {renderSectionHeader(section.name)}
                <ul className="section-items m-0 pt-1 px-0" >
                    {choices}
                </ul>
                {section.items.length > _viewAllMax &&
                    <button className="btn btn-link mr-2 ml-auto justify-content-end align-items-center d-flex" data-enumvalue={section.enumValue} onClick={toggleViewAll} >
                        {section.viewAll ? '- See less' : '+ See all'}
                    </button>
                }
            </div>
        )
    }

    const renderSections = () => {
        if (props.item == null) return;

        var sections = [];
        sections.push({
            enumValue: 1, id: 1, name: 'Industry Verticals', expanded: true, items: props.item.industryVerticals, viewAll: _viewAll.industryVerticals
        });
        sections.push({
            enumValue: 2, id: 2, name: 'Processes', expanded: true, items: props.item.categories, viewAll: _viewAll.categories
        });

        //map meta tags into same structure as other items
        if (props.item.metaTags != null && props.item.metaTags.length > 0) {
            const metatags = props.item.metaTags.map((t, counter) => {
                return { id: counter, name: t };
            });
            sections.push({
                enumValue: 3, id: 3, name: 'Tags', expanded: true, items: metatags, viewAll: _viewAll.metaTags
            });
        }

        const cards = sections.map((item) => {
            return renderSection(item);
        });

        return (
            <div className="info-section mb-4 px-1 rounded">
                {cards}
            </div>
        );
    }



    return (
        <div className={`info-panel ${props.className == null ? '' : props.className}`} >
            {renderSections()}
        </div>
    )

}

export default MarketplaceEntitySidebar