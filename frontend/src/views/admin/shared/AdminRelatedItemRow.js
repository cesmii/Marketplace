import React, { useState } from 'react'
import { Form } from 'react-bootstrap';

import { useLoadingContext } from '../../../components/contexts/LoadingContext';
import { AppSettings } from '../../../utils/appsettings';
import { generateLogMessageString } from '../../../utils/UtilityService';

const CLASS_NAME = "AdminRelatedItemRow";

function AdminRelatedItemRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps } = useLoadingContext();
    const [_isValid, setIsValid] = useState({
        relatedId: true,
        relatedTypeId: true
    });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //called when an item is selected in the panel
    const onChangeRelatedType = (e) => {
        console.log(generateLogMessageString(`onChangeRelatedType||${e.target.options[e.target.selectedIndex].text}`, CLASS_NAME));

        setIsValid({ ..._isValid, relatedTypeId: e.target.value.toString() !== "-1" });

        //update state for other components to see
        if (props.onChangeItem != null) {
            props.onChangeItem(props.item.relatedId,
                { relatedId: props.item.relatedId, displayName: props.item.displayName, relatedType: { id: e.target.value, name: e.target.options[e.target.selectedIndex].text }});
        }
    }

    const onChangeRelatedId = (e) => {
        console.log(generateLogMessageString(`onChangeRelatedId||${e.target.options[e.target.selectedIndex].text}`, CLASS_NAME));

        setIsValid({ ..._isValid, relatedId: e.target.value.toString() !== "-1" });

        //update state for other components to see
        if (props.onChangeItem != null) {
            props.onChangeItem(props.item.relatedId, 
                { relatedId: e.target.value, displayName: e.target.options[e.target.selectedIndex].text, relatedType: props.item.relatedType });
        }
    }

    //called when an item is selected in the panel
    const onDelete = (e) => {
        console.log(generateLogMessageString(`onDelete||${props.item.displayName}`, CLASS_NAME));

        //update state for other components to see
        if (props.onDelete != null) {
            props.onDelete(e, props.item.relatedId);
        }
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const validateForm_relatedId = (e) => {
        const isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, relatedId: isValid });
    };

    const validateForm_relatedTypeId = (e) => {
        const isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, relatedTypeId: isValid });
    };

    //validate all
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));
        _isValid.relatedId = props.item.relatedId.toString() !== "-1";
        _isValid.relatedTypeId = props.item.relatedTypeId != null && props.item.relatedTypeId.toString() !== "-1";
        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.relatedId && _isValid.relatedTypeId);
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderRelatedType = () => {
        if (loadingProps.lookupDataStatic == null) return;

        //show drop down list for edit, copy mode
        var items = loadingProps.lookupDataStatic.filter((g) => {
            return g.lookupType.enumValue === AppSettings.LookupTypeEnum.RelatedType //
        });
        const options = items.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Control id="relatedTypeId" as="select" value={props.item.relatedType == null ? "-1" : props.item.relatedType.id}
                    className={`minimal pr-5 ${!_isValid.relatedTypeId ? 'invalid-field' : ''}`}
                    onChange={onChangeRelatedType} onBlur={validateForm_relatedTypeId} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
                {!_isValid.relatedTypeId &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }

            </Form.Group>
        )
    };

    const renderRelatedId = () => {
        if (props.itemsLookup == null) return (`${props.item.displayName}`);

        //show drop down list
        const options = props.itemsLookup.map((item) => {
            return (<option key={item.id} value={item.id} >{item.displayName}</option>)
        });

        return (
            <Form.Group>
                <Form.Control id="relatedId" as="select" value={props.item.relatedId == null ? "-1" : props.item.relatedId}
                    className={`minimal pr-5 ${!_isValid.relatedId ? 'invalid-field' : ''}`}
                    onChange={onChangeRelatedId} onBlur={validateForm_relatedId} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
                {!_isValid.relatedId &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
            </Form.Group>
        )
    };

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
                        Name
                    </th>
                    <th className="pr-2" >
                        Related Type
                    </th>
                    <th className="pr-2 text-right" >
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
                <td className="py-2 align-text-top" >
                    {renderRelatedId()}
                </td>
                <td className="py-2 pr-2" >
                    {renderRelatedType()}
                </td>
                <td className="py-2 pr-2 text-right" >
                    <button className="btn btn-icon-outline circle ml-auto" title="Delete Item" onClick={onDelete} ><i className="material-icons">close</i></button>
                </td>
            </tr>
        </>
    );
}

export default AdminRelatedItemRow;