import React from 'react'

//const CLASS_NAME = "AdminMarketplaceRow";

function AdminMarketplaceRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onDeleteItem = (e) => {
        if (props.onDeleteItem) props.onDeleteItem(props.item);
        e.preventDefault();
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderAnalytics = (e) => {
        if (props.item.analytics == null) return ( "[none]");
        return (
            <ul className="m-0 p-0">
                <li className="m-0 p-0">Page Visits count: {props.item.analytics.pageVisitCount}</li>
                <li className="m-0 p-0">Like count: {props.item.analytics.likeCount}</li>
                <li className="m-0 p-0">More Info count: {props.item.analytics.moreInfoCount}</li>
                <li className="m-0 p-0">Share count: {props.item.analytics.shareCount}</li>
            </ul>
            );
    }

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    var cssClass = props.cssClass + (props.isHeader ? " bottom header" : " center border-top");

    if (props.isHeader) {
        return (
            <>
                <tr className={`mx-0 my-1 p-0 py-1 ${cssClass}`}>
                    <th className="pl-2" >
                        
                    </th>
                    <th className="" >
                        Display Name
                    </th>
                    <th className="py-2 d-none d-sm-table-cell align-text-top" >
                        Analytics
                    </th>
                    <th className="pr-2 text-right" >
                        Delete
                    </th>
                </tr>
            </>
        );
    }

    //item row
    if (props.item === null || props.item === {}) return null;

    return (
        <>
            <tr className={`mx-0 my-1 p-0 py-1 ${cssClass}`}>
                <td className="py-2 pl-2" >
                    <a className="btn btn-icon-outline circle mr-2" href={`/admin/library/${props.item.id}`} ><i className="material-icons">edit</i></a>
                </td>
                <td className="py-2 align-text-top" >
                    {props.item.displayName}
                    <br/>
                    <a href={`/library/${props.item.name}`} >View in Library</a>
                </td>
                <td className="py-2 d-none d-sm-table-cell align-text-top" >
                    {renderAnalytics()}
                </td>
                <td className="py-2 pr-2 text-right" >
                    <button className="btn btn-icon-outline circle ml-auto" title="Delete Item" onClick={onDeleteItem} ><i className="material-icons">close</i></button>
                </td>
            </tr>
        </>
    );
}

export default AdminMarketplaceRow;