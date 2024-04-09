import React, { useState } from 'react'
import { Button, Container, Row, Col } from 'react-bootstrap';

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { generateLogMessageString } from '../../utils/UtilityService';
import CartAddModal from './CartAddModal';

const CLASS_NAME = "AddCartButton";

function AddCartButton(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //used in popup profile add/edit ui. Default to new version
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_modalShow, setShow] = useState(false);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const addStart = (e) => {
        console.log(generateLogMessageString('addStart', CLASS_NAME));
        setShow(true);
    }

    const onAdd = () => {
        //update cart with item and quantity
        setShow(false);
    }

    const onCancel = () => {
        console.log(generateLogMessageString(`onAddCancel`, CLASS_NAME));
        setShow(false);
    };


    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderCartModal = () => {

        if (!_modalShow) return;

        return (
            <CartAddModal item={props.item} showModal={_modalShow} onAdd={onAdd} onCancel={onCancel} />
        );
    };


    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (props.item === null || props.item === {}) return null;
    if (!props.item.allowPurchase) return null;

    return (
        <>
            <Container>
                <Row>
                    <Col>Price: {props.item.price}$</Col>
                    <Col><Button variant="primary" className="auto-width text-nowrap" onClick={addStart}>Add to Cart</Button></Col>
                </Row>
            </Container>            
            {renderCartModal()}
        </>
    );
}

export default AddCartButton;