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
                <div className="text-center" >There are no items.</div>
            </div>
        );
    }

    const renderItemsGridHeader = () => {
        if ((props.items == null || props.items.length === 0)) return;
        return (
            <thead>
                <AdminRelatedItemRow key="header" item={null} isHeader={true} cssClass="admin-item-row" />
            </thead>
        )
    }

    //render the main grid
    const renderItemsGrid = () => {
        if (props.items == null || props.items.length === 0) {
            return (
                <tbody>
                    <tr>
                        <td className="no-data">
                            {renderNoDataRow()}
                        </td>
                    </tr>
                </tbody>
            )
        }

        const mainBody = props.items.map((item) => {
            const key = `${item.relatedId}-${item.id}`;
            return (
                <AdminRelatedItemRow key={key} item={item} cssClass={`admin-item-row`} itemsLookup={_itemsLookup}
                    type={props.type} onChangeItem={props.onChangeItem} onDelete={props.onDelete} />
            );
        });

        return (
            <tbody>
                {mainBody}
            </tbody>
        )
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {(props.caption != null) &&
                <h2>{props.caption}</h2>
            }
            <table className="flex-grid w-100 grid-select" >
                {renderItemsGridHeader()}
                {renderItemsGrid()}
                {(_itemsLookup != null && _itemsLookup.length > 0) &&
                    <tfoot>
                        <tr>
                            <td>
                                <button className="btn btn-icon-outline circle primary" onClick={onAdd} ><i className="material-icons">add</i></button>
                            </td>
                        </tr>
                    </tfoot>
                }
            </table>
        </>
    )
}

export default AdminRelatedItemList;