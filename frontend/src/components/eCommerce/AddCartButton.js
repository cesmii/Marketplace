import React, { useState } from 'react'
import { Button } from 'react-bootstrap';

import { generateLogMessageString } from '../../utils/UtilityService';
import CartAddModal from './CartAddModal';

const CLASS_NAME = "AddCartButton";

function AddCartButton(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //used in popup profile add/edit ui. Default to new version
    const [_showModal, setShowModal] = useState(false);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const addStart = () => {
        console.log(generateLogMessageString('addStart', CLASS_NAME));
        setShowModal(true);
    }

    const onAdd = () => {
        //update cart with item and quantity
        setShowModal(false);
    }

    const onCancel = () => {
        console.log(generateLogMessageString(`onAddCancel`, CLASS_NAME));
        setShowModal(false);
    };


    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (props.item === null || props.item === {}) return null;
    if (!props.item.allowPurchase) return null;
    const css = props.className == null ? '' : props.className;

    return (
        <>
            <Button variant="primary" className={`auto-width text-nowrap ${css}`} onClick={addStart}>Add to Cart</Button>
            {_showModal &&
                <CartAddModal item={props.item} showModal={_showModal} onAdd={onAdd} onCancel={onCancel} showAbstract={false} />
            }
        </>
    );
}

export default AddCartButton;