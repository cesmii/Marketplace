import React from 'react'
import { formatDate } from '../../../utils/UtilityService';

//const CLASS_NAME = "AdminRequestInfoRow";

function AdminRequestInfoRow(props) { //props are item, showActions

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
                    <th className="" >
                        
                    </th>
                    <th className="" >
                        Personal Info
                    </th>
                    <th className="d-none d-sm-table-cell" >
                        Company Info
                    </th>
                    <th className="d-none d-sm-table-cell" >
                        Request Type
                    </th>
                    <th className="d-none d-sm-table-cell" >
                        Status
                    </th>
                    <th className="text-right" >
                        Created On
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
                <td className="py-2" >
                    <a className="btn btn-icon-outline circle mr-2" href={`/admin/requestinfo/${props.item.id}`} ><i className="material-icons">edit</i></a>
                </td>
                <td className="py-2 align-text-top" >
                        {(props.item.firstName.toString() !== '' || props.item.lastName.toString() !== '') &&
                            <p className="mb-1 d-block" >{props.item.firstName} {props.item.lastName}</p>
                        }
                        <p className="mb-0 d-block" >{props.item.email}</p>
                        {(props.item.membershipStatus != null) &&
                            <p className="mb-0 mt-1 d-block" >{props.item.membershipStatus.name}</p>
                        }
                </td>
                <td className="py-2 d-none d-sm-table-cell align-text-top" >
                    {(props.item.companyName != null && props.item.companyName.toString() !== '') &&
                        <p className="mb-1" >{props.item.companyName}</p>
                    }
                    {(props.item.industries != null && props.item.industries.toString() !== '') &&
                        <p className="mb-1" >Industries: {props.item.industries}</p>
                    }
                    {(props.item.companyUrl != null && props.item.companyUrl.toString() !== '') &&
                        <p className="mb-0" >{props.item.companyUrl}</p>
                    }
                </td>
                <td className="py-2 d-none d-sm-table-cell align-text-top" >
                    {(props.item.requestType != null) &&
                        <p className="m-0" >{props.item.requestType.name}</p>
                    }
                </td>
                <td className="py-2 d-none d-sm-table-cell text-center align-text-top" >
                    {(props.item.status != null) &&
                        <p className="m-0" >{props.item.status.name}</p>
                    }
                </td>
                <td className="py-2 text-right align-text-top" >
                    {formatDate(props.item.created)}
                </td>
            </tr>
        </>
    );
}

export default AdminRequestInfoRow;