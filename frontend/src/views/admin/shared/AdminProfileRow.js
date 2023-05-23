import React from 'react'
import { formatDate } from '../../../utils/UtilityService';

//const CLASS_NAME = "AdminProfileRow";

function AdminProfileRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

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
                    <th className="py-2 d-none d-sm-table-cell align-text-top" >
                        Namespace / Version
                    </th>
                    <th className="pr-2 text-right" >
                        Publication Date
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
                    <a className="btn btn-icon-outline circle mr-2" href={`/admin/profile/${props.item.id}`} ><i className="material-icons">edit</i></a>
                </td>
                <td className="py-2 align-text-top" >
                    {props.item.displayName}
                    <br/>
                    <a href={`/profile/${props.item.name}`} >View in Library</a>
                </td>
                <td className="py-2 d-none d-sm-table-cell align-text-top" >
                    {props.item.namespace}
                    {props.item.version != null &&
                        <>
                        <br />
                        Version: {props.item.version}
                        </>
                    }
                </td>
                <td className="py-2 pr-2 text-right" >
                    {formatDate(props.item.publishDate)}
                </td>
            </tr>
        </>
    );
}

export default AdminProfileRow;