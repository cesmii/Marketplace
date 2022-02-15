import React from 'react'

import { ImageUploader } from '../../../components/ImageUploader';
import { generateLogMessageString, getImageAlt, getImageUrl } from '../../../utils/UtilityService';

const CLASS_NAME = "AdminImageRow";

function AdminImageRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onDeleteItem = (e) => {
        if (props.onDeleteItem) props.onDeleteItem(props.item);
        e.preventDefault();
    }

    const onImageReplace = (ids) => {
        console.log(generateLogMessageString(`onImageReplace||# Images: ${ids.length}`, CLASS_NAME));
        if (props.onImageReplace) props.onImageReplace(ids);
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
                    <th className="" >
                    </th>
                    <th className="" >
                        File Name (Url)
                    </th>
                    <th className="" >
                    </th>
                    <th className="text-right" >
                        Delete
                    </th>
                </tr>
            </>
        );
    }

    //item row
    if (props.item === null || props.item === {}) return null;

    const url = getImageUrl(props.item);

    return (
        <>
            <tr className={`mx-0 my-1 p-0 py-1 ${cssClass}`}>
                <td className="py-2 d-none d-sm-table-cell align-middle text-center" >
                    <img className="shadow m-2" style={{ maxWidth: '160px', height: 'auto' }} src={props.item.src} alt={getImageAlt(props.item)} />
                </td>
                <td className="py-2 align-middle" >
                    {props.item.fileName}
                    <br/>
                    <a href={url} target="_blank" rel="noreferrer" >{url}</a>
                </td>
                <td className="py-2 text-right align-middle" >
                    <ImageUploader imageId={props.item.id} caption="Replace" cssClass="btn-primary auto-width mb-0" uploadToServer={true} onImageUpload={onImageReplace} marketplaceItemId={props.item.marketplaceItemId} />
                </td>
                <td className="py-2 text-right align-middle" >
                    {(props.canDelete) &&
                        <button className="btn btn-icon-outline circle ml-auto" title="Delete Image" onClick={onDeleteItem} ><i className="material-icons">close</i></button>
                    }
                </td>
            </tr>
        </>
    );
}

export default AdminImageRow;