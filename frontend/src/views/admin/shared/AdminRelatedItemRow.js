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
        relatedType: true
    });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //called when an item is selected in the panel
    const onChangeRelatedType = (e) => {
        console.log(generateLogMessageString(`onChangeRelatedType||${e.target.options[e.target.selectedIndex].text}`, CLASS_NAME));

        validateForm_relatedType(e);

        //update state for other components to see
        if (props.onChangeItem != null) {
            props.onChangeItem(props.item.relatedId,
                {
                    relatedId: props.item.relatedId,
                    displayName: props.item.displayName,
                    relatedType: { id: e.target.value, name: e.target.options[e.target.selectedIndex].text },
                    externalSource: props.item.externalSource
                });
        }
    }

    const onChangeRelatedId = (e) => {
        console.log(generateLogMessageString(`onChangeRelatedId||${e.target.options[e.target.selectedIndex].text}`, CLASS_NAME));

        validateForm_relatedId(e);

        //update state for other components to see
        if (props.onChangeItem != null) {
            //if we are dealing with external source drop down, then we handle this slightly differently
            const sourceId = e.target[e.target.selectedIndex].getAttribute("data-sourceid");
            const code = e.target[e.target.selectedIndex].getAttribute("data-code");
            const externalSource = (sourceId && code) ? { sourceId: sourceId, code: code, id: e.target.value } : null;

            props.onChangeItem(props.item.relatedId,
                {
                    relatedId: e.target.value,
                    displayName: e.target.options[e.target.selectedIndex].text,
                    relatedType: props.item.relatedType,
                    externalSource: externalSource
                });
        }
    }

    //called when an item is selected in the panel
    const onDelete = (e) => {
        console.log(generateLogMessageString(`onDelete||${props.item.displayName}`, CLASS_NAME));

        //update state for other components to see
        if (props.onDelete != null) {
            props.onDelete(props.item.relatedId);
        }
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const validateForm_relatedId = (e) => {
        const isValid = e.target.value.toString() !== "-1";
        setIsValid({
            relatedId: isValid,
            relatedType: props.item.relatedType != null && props.item.relatedType.id.toString() !== "-1"
        });
    };

    const validateForm_relatedType = (e) => {
        const isValid = e.target.value.toString() !== "-1";
        const isValidRelatedId = props.item.relatedId != null && (isNaN(props.item.relatedId) || parseFloat(props.item.relatedId) > 0);
        setIsValid({
            relatedId: isValidRelatedId,
            relatedType: isValid
        });
    };

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
            <Form.Group className="mb-0">
                <Form.Control id="relatedTypeId" as="select" value={props.item.relatedType == null ? "-1" : props.item.relatedType.id}
                    className={`minimal pr-5 ${!_isValid.relatedType ? 'invalid-field' : ''}`}
                    onChange={onChangeRelatedType} onBlur={validateForm_relatedType} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>

            </Form.Group>
        )
    };

    //Note - this component is shared by marketplace related items grid and external items grid so use caution 
    //in changing one without disrupting the other
    const renderRelatedId = () => {
        //until lookup data arrives, show label
        if (props.itemsLookup == null) return (`${props.item.displayName}`);

        //if relatedId has a real value, then don't permit the user to change the value
        //they can always delete and re-add to change. 
        //A real value will be a Mongo id - combo of num and char OR a long positive int
        if (isNaN(props.item.relatedId) || parseFloat(props.item.relatedId) > 0) {
            return (
                <>
                    {props.item.displayName}
                    {(props.item.namespace != null && props.item.namespace !== '') &&
                        <>
                            <br />
                            <span style={{ wordBreak: "break-word" }} >{props.item.namespace} (v. {props.item.version})</span>
                        </>
                    }
                </>
            );
        }

        //show drop down list
        const options = props.itemsLookup.map((itm) => {
            //marketplace item, externalSource is null, external items will have externalSource populated 
            //and externalSource will include the id
            const displayName = `${itm.displayName}` +
                `${(itm.namespace != null && itm.namespace !== '') ? ' (' + itm.namespace + ' v.' + itm.version + ')' : ''}`;
            return (<option key={itm.id} value={itm.id} data-sourceid={itm.externalSource?.sourceId} data-code={itm.externalSource?.code} >{displayName}</option>)
        });

        return (
            <Form.Group className="mb-0">
                <Form.Control id="relatedId" as="select" value={props.item.relatedId == null ? "-1" : props.item.relatedId}
                    className={`minimal pr-5 ${!_isValid.relatedId ? 'invalid-field' : ''}`}
                    onChange={onChangeRelatedId} onBlur={validateForm_relatedId} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    let cssClass = props.cssClass + (props.isHeader ? " bottom header" : " center border-top");
    if (!_isValid.relatedId || !_isValid.relatedType) {
        cssClass += ' alert alert-danger';
    }

    if (props.isHeader) {
        return (
            <div className={`row my-1 p-0 py-1 d-flex align-items-center ${cssClass}`}>
                <div className="col-sm-6 fw-bold" >
                    Name
                </div>
                <div className="col-sm-4 fw-bold" >
                    Related Type
                </div>
                <div className="col-sm-2 text-end fw-bold" >
                </div>
            </div>
        );
    }

    //item row
    if (props.item === null || props.item === {}) return null;

    return (
        <div className={`row my-1 p-0 py-1 d-flex align-items-center ${cssClass}`}>
            <div className="col-sm-6" >
                {renderRelatedId()}
            </div>
            <div className="col-sm-4" >
                {renderRelatedType()}
            </div>
            <div className="col-sm-2 text-end" >
                <button className="btn btn-icon-outline circle ms-auto" title="Delete Item" onClick={onDelete} ><i className="material-icons">close</i></button>
            </div>
        </div>
    );
}

export default AdminRelatedItemRow;