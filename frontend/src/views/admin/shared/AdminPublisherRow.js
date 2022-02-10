import React from 'react'

//const CLASS_NAME = "AdminPublisherRow";

function AdminPublisherRow(props) { //props are item, showActions

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
                <td className="py-2 pl-2 align-middle" >
                    <a className="btn btn-icon-outline circle mr-2" href={`/admin/publisher/${props.item.id}`} ><i className="material-icons">edit</i></a>
                </td>
                <td className="py-2 align-middle" >
                    {props.item.displayName}
                    <br />
                    <a href={`/publisher/${props.item.name}`} >View in Library</a>
                </td>
                <td className="py-2 pr-2 text-right align-middle" >
                    <button className="btn btn-icon-outline circle ml-auto" title="Delete Item" onClick={onDeleteItem} ><i className="material-icons">close</i></button>
                </td>
            </tr>
        </>
    );
}

export default AdminPublisherRow;