import React, { useEffect, useState } from 'react'

import { generateLogMessageString } from '../../../utils/UtilityService'
import AdminRelatedItemRow from './AdminRelatedItemRow';

const CLASS_NAME = "AdminRelatedItemList";

function AdminRelatedItemList(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_items, setItems] = useState([]);
    const [_itemsLookup, setItemsLookup] = useState([]);  //marketplace items 

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
    // filter related items lookup to remove already selected items
    //-------------------------------------------------------------------
    useEffect(() => {
        if (props.itemsLookup == null) {
            setItemsLookup([]);
        }
        else {
            setItemsLookup(props.itemsLookup.filter(f => {
                return (props.items == null || props.items.findIndex(x => x.relatedId === f.id) < 0);
            })
            );
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||props.items||Cleanup', CLASS_NAME));
        };
    }, [props.items, props.itemsLookup]);

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
                <div className="text-center" >There are no related items.</div>
            </div>
        );
    }

    const renderItemsGridHeader = () => {
        if ((props.items == null || props.items.length === 0)) return;
        return (
            <AdminRelatedItemRow key="header" item={null} isHeader={true} cssClass="admin-item-row" />
        )
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
                <AdminRelatedItemRow key={key} item={item} cssClass={`admin-item-row`} itemsLookup={_itemsLookup}
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
            {renderItemsGridHeader()}
            {renderItemsGrid()}
            {(_itemsLookup != null && _itemsLookup.length > 0) &&
                <div className={`row my-1 p-0 py-1`}>
                    <div className="col-sm-12 border-top bg-light py-1" >
                        <button className="btn btn-icon-outline circle primary d-inline " onClick={onAdd} ><i className="material-icons">add</i>
                        </button>
                        {(props.captionAdd != null) &&
                            <span className="pl-2" >{props.captionAdd}</span>
                        }
                    </div>
                </div>
            }
        </>
    )
}

export default AdminRelatedItemList;