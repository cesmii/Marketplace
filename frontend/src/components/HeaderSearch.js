import React, { useState, useEffect } from 'react'
//import PropTypes from 'prop-types';
import Form from 'react-bootstrap/Form'
import FormControl from 'react-bootstrap/FormControl'
import InputGroup from 'react-bootstrap/InputGroup'
import Button from 'react-bootstrap/Button'

//import Fab from './Fab'

import { generateLogMessageString } from '../utils/UtilityService'

const CLASS_NAME = "HeaderSearch";

function HeaderSearch(props) { //(caption, iconName, showSearch, searchValue, onSearch)
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_filterVal, setFilterVal] = useState(props.filterVal); //props.searchValue

    //-------------------------------------------------------------------
    // Region: useEffect
    //-------------------------------------------------------------------
    useEffect(() => {

        if (_filterVal !== props.filterVal) {
            setFilterVal(props.filterVal);
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.filterVal]);

    ////-------------------------------------------------------------------
    //// Region: Event Handling of child component events
    ////-------------------------------------------------------------------
    //update search state so that form submit has value
    const onSearchChange = (e) => {
        //when using predictive search mode, we don't execute the search on the parent grid. we display it in a drop down
        if (props.searchMode === 'predictive') return;

        setFilterVal(e.target.value);
    }

    //update search state so that form submit has value
    const onSearchBlur = (e) => {
        // call change page function in parent component
        if (props.onSearchBlur) props.onSearchBlur(e.target.value);
    }

    //trigger search after x chars entered or search button click
    const onSearchClick = (e) => {
        console.log(generateLogMessageString(`onSearchClick||Search value: ${_filterVal}`, CLASS_NAME));
        e.preventDefault();
        // call change page function in parent component
        if (props.onSearch) props.onSearch(_filterVal);
    }

    ////-------------------------------------------------------------------
    //// Region: Render helpers
    ////-------------------------------------------------------------------
    const renderSearchUI = () => {
        //Some components won't show the search ui.
        if (props.showSearch != null && !props.showSearch) return;
        return (
            <>
                {(props.itemCount != null && props.itemCount > 0) &&
                    <span className="text-right text-nowrap">{props.itemCount}{props.itemCount === 1 ? ' item' : ' items'}</span>
                }
                <Form onSubmit={onSearchClick} className="header-search-block">
                    <Form.Row className="mx-0" >
                        <InputGroup className="txt-search-ui-group">
                            <FormControl
                                type="text"
                                placeholder="Search here"
                                aria-label="Search here"
                                value={_filterVal == null ? '' : _filterVal}
                                onChange={onSearchChange}
                                onBlur={onSearchBlur}
                                className="with-append"
                            />
                            <InputGroup.Append>
                                {props.searchMode == null || props.searchMode === "standard" ? (
                                    <Button variant="search" className="p-0 px-3 border-left-0 d-flex align-items-center" onClick={onSearchClick} type="submit" title="Search" >
                                        <i className="material-icons">search</i>
                                    </Button>
                                ) : ""
                                }
                            </InputGroup.Append>
                        </InputGroup>
                    </Form.Row>
                </Form>
            </>
        );
    }


    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    return (
        <>
            {renderSearchUI()}
        </>
    )

}

export default HeaderSearch