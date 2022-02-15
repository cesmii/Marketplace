import React from 'react'

import '../../components/styles/InfoPanel.scss';

//const CLASS_NAME = "MarketplaceEntitySidebar";
function MarketplaceEntitySidebar(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpders
    //-------------------------------------------------------------------
    const renderSectionHeader = (caption) => {
        return (
            <div className="headline-3" >
                <span key="caption" className="caption font-weight-bold">
                    {caption}
                </span>
            </div>
        )
    };

    const renderSection = (section) => {
        if (section.items == null || section.items.length === 0) return;

        const choices = section.items.map((item) => {
            return (
                <li key={`${section.enumValue}-${item.id}`} className='my-1 tag' >
                    <span className="section-item">{item.name}</span>
                </li>
            )
        });
        return (
            <div key={`${section.name}-${section.enumValue}`} className="mb-4">
                {renderSectionHeader(section.name)}
                <ul className="section-items m-0 pt-1 px-0" >
                    {choices}
                </ul>
            </div>
        )
    }

    const renderSections = () => {
        if (props.item == null) return;

        var sections = [];
        sections.push({ enumValue: 1, id: 1, name: 'Industry Verticals', expanded: true, items: props.item.industryVerticals });
        sections.push({ enumValue: 2, id: 2, name: 'Processes', expanded: true, items: props.item.categories  });

        //map meta tags into same structure as other items
        if (props.item.metaTags != null && props.item.metaTags.length > 0) {
            const metatags = props.item.metaTags.map((t, counter) => {
                return { id: counter, name: t };
            });
            sections.push({ enumValue: 3, id: 3, name: 'Tags', expanded: true, items: metatags });
            //tags.push({ enumValue: 4, id: 4, name: 'Publisher', expanded: true, items: props.item.industryVerticals });
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
        <div className="info-panel" >
            {renderSections()}
        </div>
    )

}

export default MarketplaceEntitySidebar