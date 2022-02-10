import React, { useState, useEffect, Fragment } from 'react'

import { generateLogMessageString } from '../../utils/UtilityService'
import { renderLinkedName } from './MarketplaceRenderHelpers';

import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import '../styles/MarketplaceBreadcrumbs.scss';

const CLASS_NAME = "MarketplaceBreadcrumbs";

function MarketplaceBreadcrumbs(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_items, setItems] = useState([]);

    //-------------------------------------------------------------------
    // Region: Get the ancestors for this item and all of its siblings in a sorted array
    //-------------------------------------------------------------------
    useEffect(() => {

        async function bindData() {
            //console.log(generateLogMessageString('useEffect||bindData||async', CLASS_NAME));
            setItems(props.item.ancestory);
        }
        bindData();


        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.item]);

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------

    if (props.item == null) return;

    if (_items == null || _items === []) return;

    //return final ui
    //assume array ordered properly by inheritance
    const result = _items.map((p, i) => {
        var delimiter = i < _items.length - 1 ? (<span className="mr-2" >/</span>) : "";
        return (
            <Fragment key={`crumb_${p.id}`} >
                {i < _items.length - 1 ? renderLinkedName(p, 'mr-2') : p.name }
                { delimiter }
            </Fragment>
        );
    });

    return (
        <div key="breadcrumbs" className="row breadcrumbs m-0">
            <div className="col-sm-12 m-0 p-0">
                <span className="mr-2" ><SVGIcon name="schema" size="18" fill={color.shark} alt="breadcrumbs" /></span>
                {result}
            </div>
        </div>
    );
}

function ProfileBreadcrumbs(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_items, setItems] = useState([]);

    //-------------------------------------------------------------------
    // Region: Get the ancestors for this item and all of its siblings in a sorted array
    //-------------------------------------------------------------------
    useEffect(() => {

        async function bindData() {
            //console.log(generateLogMessageString('useEffect||bindData||async', CLASS_NAME));
            setItems(props.item.ancestory);
        }
        bindData();


        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.item]);

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------

    if (props.item == null) return;

    if (_items == null || _items === []) return;

    //return final ui
    //assume array ordered properly by inheritance
    const result = _items.map((p, i) => {
        var delimiter = i < _items.length - 1 ? (<span className="mr-2" >/</span>) : "";
        return (
            <Fragment key={`crumb_${p.id}`} >
                {i < _items.length - 1 ? renderLinkedName(p, 'mr-2') : p.name}
                { delimiter}
            </Fragment>
        );
    });

    return (
        <div key="breadcrumbs" className="row breadcrumbs m-0">
            <div className="col-sm-12 m-0 p-0">
                <span className="mr-2" ><SVGIcon name="schema" size="18" fill={color.shark} alt="breadcrumbs" /></span>
                {result}
            </div>
        </div>
    );
}

export { MarketplaceBreadcrumbs, ProfileBreadcrumbs };
