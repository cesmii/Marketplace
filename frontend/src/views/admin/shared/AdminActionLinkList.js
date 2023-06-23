import React, { useEffect, useState } from 'react'

import { generateLogMessageString } from '../../../utils/UtilityService'
import AdminActionLinkRow from './AdminActionLinkRow';

const CLASS_NAME = "AdminActionLinkList";

function AdminActionLinkList(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_items, setItems] = useState([]);

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        setItems(props.items);

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||props.items||Cleanup', CLASS_NAME));
        };
    }, [props.items]);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    const onAdd = (e) => {
        //raised from add button click
        console.log(generateLogMessageString(`onAdd`, CLASS_NAME));
        e.preventDefault();

        //trigger an add of a blank row
        if (props.onAdd != null) {props.onAdd();}
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderNoDataRow = () => {
        return (
            <div className="alert alert-info-custom mt-2 mb-2">
                <div className="text-center" >There are no action links.</div>
            </div>
        );
    }

    //render the main grid
    const renderItemsGrid = () => {
        if (props.items == null || props.items.length === 0) {
            return (
                <div className={`row my-1 p-0 py-1`}>
                    <div className="col-sm-12 py-1" >
                        {renderNoDataRow()}
                    </div>
                </div>
            )
        }

        return props.items.map((item) => {
            const key = `${item.relatedId}-${item.id}`;
            return (
                <AdminActionLinkRow key={key} item={item} cssClass={`admin-item-row`} 
                    type={props.type} onChangeItem={props.onChangeItem} onDelete={props.onDelete} />
            );
        });
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {(props.caption != null) &&
                <h3>{props.caption}</h3>
            }
            {(props.infoText != null) &&
                <span className="small text-muted">
                    {props.infoText}
                </span>
            }

            {renderItemsGrid()}
            <div className={`row my-1 p-0 py-1`}>
                <div className="col-sm-12 border-top bg-light py-1" >
                    <button className="btn btn-icon-outline circle primary d-inline " onClick={onAdd} ><i className="material-icons">add</i>
                    </button>
                    {(props.captionAdd != null) &&
                        <span className="pl-2" >{props.captionAdd}</span>
                    }
                </div>
            </div>
        </>
    )
}

export default AdminActionLinkList;