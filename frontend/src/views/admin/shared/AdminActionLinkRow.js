import React, { useState } from 'react'
import { Form } from 'react-bootstrap';

//import { AppSettings } from '../../../utils/appsettings';
import { generateLogMessageString } from '../../../utils/UtilityService';

const CLASS_NAME = "AdminActionLinkRow";

function AdminActionLinkRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _targets = [{ id: '_self', caption: 'Self' }, { id: '_blank', caption: 'Blank' }, { id: '_new', caption: 'New' }];
    const [_isValid, setIsValid] = useState({
        url: true,
        caption: true
    });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onChange||e:${e.target}`, CLASS_NAME));

        switch (e.target.id) {
            case "url":
                validateForm_url(e);
                break;
            case "caption":
                validateForm_caption(e);
                break;
            default:
                break;
        }

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "url":
            case "iconName":
            case "caption":
            case "target":
                props.item[e.target.id] = e.target.value;
                break;
            default:
                return;
        }
        //update the state
        if (props.onChangeItem) props.onChangeItem(JSON.parse(JSON.stringify(props.item)));
    }


    //called when an item is selected in the panel
    const onDelete = () => {
        console.log(generateLogMessageString(`onDelete||${props.item.caption}`, CLASS_NAME));

        //update state for other components to see
        if (props.onDelete != null) {
            props.onDelete(props.item.id);
        }
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const validateForm_url = (e) => {
        const isValid = e.target.value.toString() !== "";
        setIsValid({ ..._isValid, url: isValid });
    };

    const validateForm_caption = (e) => {
        const isValid = e.target.value.toString() !== "";
        setIsValid({ ..._isValid, caption: isValid });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderTarget = () => {

        //show drop down list for edit, copy mode
        const options = _targets.map((item) => {
            return (<option key={item.id} value={item.id} >{item.caption}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Target</Form.Label>
                <Form.Control id="target" as="select" value={props.item.target == null ? "_self" : props.item.target}
                    className={`minimal pr-5`}
                    onChange={onChange} >
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
    if (!_isValid.url || !_isValid.caption) {
        cssClass += ' alert alert-danger';
    }

    if (props.isHeader) {
        return (
            <div className={`row my-1 p-0 py-1 d-flex align-items-center ${cssClass}`}>
                <div className="col-sm-5 font-weight-bold" >
                    Url / Caption
                </div>
                <div className="col-sm-4 font-weight-bold" >
                    Icon Name
                </div>
                <div className="col-sm-3 text-right font-weight-bold" >
                    Target
                </div>
            </div>
        );
    }

    //item row
    if (props.item === null || props.item === {}) return null;

    return (
        <div className={`row my-1 p-0 py-1 d-flex align-items-center ${cssClass}`}>
            <div className="col-sm-10" >
                <Form.Group>
                    <Form.Label>Url*</Form.Label>
                    {!_isValid.url &&
                        <span className="invalid-field-message inline">
                            Required
                        </span>
                    }
                    <Form.Control id="url" className={(!_isValid.url ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter url (ie https://marketplace.cesmii.net)`}
                        value={props.item.url} onBlur={validateForm_url} onChange={onChange} />
                </Form.Group>
            </div>
            <div className="col-sm-2 text-right pt-1" >
                <button className="btn btn-icon-outline circle ml-auto" title="Delete Item" onClick={onDelete} ><i className="material-icons">close</i></button>
            </div>
            <div className="col-sm-5" >
                <Form.Group>
                    <Form.Label>Caption*</Form.Label>
                    {!_isValid.caption &&
                        <span className="invalid-field-message inline">
                            Required
                        </span>
                    }
                    <Form.Control id="caption" className={(!_isValid.caption ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter friendly caption`}
                        value={props.item.caption} onBlur={validateForm_caption} onChange={onChange} />
                </Form.Group>
            </div>
            <div className="col-sm-4" >
                <Form.Group>
                    <Form.Label>Icon Name</Form.Label>
                    <Form.Control id="iconName" className='minimal pr-5' placeholder={`Any Material Icon is allowed`}
                        value={props.item.iconName} onChange={onChange} />
                </Form.Group>
            </div>
            <div className="col-sm-3" >
                {renderTarget()}
            </div>
        </div>
    );
}

export default AdminActionLinkRow;