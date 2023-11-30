import React from 'react'

//const CLASS_NAME = "AdminJobDefinitionRow";

function AdminJobDefinitionRow(props) { //props are item, showActions

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
                    <th className="ps-2" >
                        
                    </th>
                    <th className="" >
                        Name
                    </th>
                    <th className="pe-2 text-end" >
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
                <td className="py-2 ps-2 align-middle" >
                    <a className="btn btn-icon-outline circle me-2" href={`/admin/jobdefinition/${props.item.id}`} ><i className="material-icons">edit</i></a>
                </td>
                <td className="py-2 align-middle" >
                    {props.item.name}
                </td>
                <td className="py-2 pe-2 text-end align-middle" >
                    <button className="btn btn-icon-outline circle ms-auto" title="Delete Item" onClick={onDeleteItem} ><i className="material-icons">close</i></button>
                </td>
            </tr>
        </>
    );
}

export default AdminJobDefinitionRow;