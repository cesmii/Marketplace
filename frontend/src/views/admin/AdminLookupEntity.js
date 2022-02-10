import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'

import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { useAuthState } from "../../components/authentication/AuthContext";

import { SVGIcon } from "../../components/SVGIcon";
import color from "../../components/Constants";
import ConfirmationModal from '../../components/ConfirmationModal';

const CLASS_NAME = "AdminLookupEntity";

function AdminLookupEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const { id, parentId } = useParams();
    //var pageMode = //state is not always present. If user types a url or we use an href link, state is null. history.location.state.viewMode;
    //see logic below for how we calculate.
    const [mode, setMode] = useState(initPageMode());
    const [item, setItem] = useState({});
    const [_lookupTypes, setLookupTypes] = useState([]);
    const [_refreshLookupTypes, setRefreshLookupTypes] = useState(true);

    const [isLoading, setIsLoading] = useState(true);
    const [isReadOnly, setIsReadOnly] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const authTicket = useAuthState();
    const [_isValid, setIsValid] = useState({ name: true, displayOrder: true, lookupType: true });
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    var caption = 'Lookup Item';

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            //mode not set right if we were on this page, save an copy and navigate into edit same marketplaceItem. Rely on
            // parentId, id. Then determine mode. for copy, we use parentId, for edit/view, we use id.
            var result = null;
            try {
                var data = { id: (parentId != null ? parentId : id) };
                var url = `admin/lookup/${parentId == null ? 'getbyid' : 'copy'}`
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this lookup item.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This lookup item was not found.';
                    history.push('/404');
                }
                //403 error - user may be allowed to log in but not permitted to perform the API call they are attempting
                else if (err != null && err.response != null && err.response.status === 403) {
                    console.log(generateLogMessageString('useEffect||fetchData||Permissions error - 403', CLASS_NAME, 'error'));
                    msg += ' You are not permitted to edit lookup items.';
                    history.goBack();
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            var thisMode = (parentId != null) ? 'copy' : 'edit';

            //set item state value
            setItem(result.data);
            setIsLoading(false);
            setLoadingProps({ isLoading: false, message: null });
            setMode(thisMode);

            // set form to readonly if we're in viewmode or is deleted (isActive = false)
            setIsReadOnly(thisMode.toLowerCase() === "view" || !result.data.isActive);

        }

        //get a blank lookup item object from server
        async function fetchDataAdd() {
            console.log(generateLogMessageString('useEffect||fetchDataAdd||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                var url = `admin/lookup/init`
                result = await axiosInstance.post(url);
            }
            catch (err) {
                var msg = 'An error occurred retrieving the blank lookup item.';
                console.log(generateLogMessageString('useEffect||fetchDataAdd||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' A problem occurred with the add lookup item screen.';
                    history.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            //set item state value
            setItem(result.data);
            setIsLoading(false);
            setLoadingProps({ isLoading: false, message: null });
            //setMode(thisMode);
            setIsReadOnly(false);
        }

        //fetch our data 
        // for view/edit modes
        if ((id != null && id.toString() !== 'new') || parentId != null) {
            fetchData();
        }
        else {
            fetchDataAdd();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id, parentId, authTicket.user]);


    //-------------------------------------------------------------------
    // Region: Lookup Types Data
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchLookupTypes() {
            console.log(generateLogMessageString('useEffect||fetchLookupTypes||async', CLASS_NAME));

            var result = null;
            try {
                var data = { id: (parentId != null ? parentId : id) };
                var url = `admin/lookup/types/all`
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving lookup types.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' Lookup types endpoint not found.';
                    history.push('/404');
                }
                //403 error - user may be allowed to log in but not permitted to perform the API call they are attempting
                else if (err != null && err.response != null && err.response.status === 403) {
                    console.log(generateLogMessageString('useEffect||fetchLookupTypes||Permissions error - 403', CLASS_NAME, 'error'));
                    msg += ' You are not permitted to fetch lookup types.';
                    history.goBack();
                }
                setLoadingProps({
                    message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            //set item state value
            setLookupTypes(result.data);
        }

        //fetch lookup types data 
        // for view/edit modes
        if (_refreshLookupTypes) {
            setRefreshLookupTypes(false);
            fetchLookupTypes();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [_refreshLookupTypes]);

    //-------------------------------------------------------------------
    // Region: 
    //-------------------------------------------------------------------
    function initPageMode() {
        //if path contains copy and parent id is set, mode is copy
        //else - we won't know the author ownership till we fetch data, default view
        if (parentId != null && history.location.pathname.indexOf('/copy/') > -1) return 'copy';

        //if path contains new, then go into a new mode
        if (id === 'new') {
            return 'new';
        }

        //if path contains id, then default to view mode and determine in fetch whether user is owner or not.
        return 'view';
    }

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_name = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValid({ ..._isValid, name: isValid });
    };

    const validateForm_displayOrder = (e) => {
        var isValid = (e.target.value == null || e.target.value === '' || !isNaN(parseFloat(e.target.value)));
        setIsValid({ ..._isValid, displayOrder: isValid });
    };

    const validateForm_lookupType = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, lookupType: isValid });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.name = item.name != null && item.name.trim().length > 0;
        _isValid.displayOrder = item.displayName == null || item.displayName === '' || !isNaN(parseFloat(item.displayOrder));
        _isValid.lookupType = item.lookupType != null && item.lookupType.enumValue.toString() !== "-1";

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.name && _isValid.displayOrder && _isValid.lookupType);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onDeleteItem = () => {
        console.log(generateLogMessageString('onDeleteItem', CLASS_NAME));
        setDeleteModal({ show: true, item: item });
    };

    const onDeleteConfirm = () => {
        console.log(generateLogMessageString('onDeleteConfirm', CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        var data = { id: item.id };
        var url = `admin/lookup/delete`;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: `Item was deleted`, isTimed: true
                            }
                        ],
                        refreshLookupData: true,
                        refreshSearchCriteria: true
                    });
                    history.push('/admin/lookup/list');
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Delete Item Error', message: result.data.message });
                    setLoadingProps({ isLoading: false, message: null });
                    setDeleteModal({ show: false, item: null });
                }

            })
            .catch(error => {
                //hide a spinner, show a message
                setError({ show: true, caption: 'Delete Item Error', message: `An error occurred deleting this item.` });
                setLoadingProps({ isLoading: false, message: null });

                console.log(generateLogMessageString('deleteItem||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });
    };

    const onCancel = () => {
        //raised from header nav
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        history.goBack();
    };

    const onSave = () => {
        //raised from header nav
        console.log(generateLogMessageString('onSave', CLASS_NAME));

        //do validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform insert call
        console.log(generateLogMessageString(`handleOnSave||${mode}`, CLASS_NAME));
        var url = mode.toLowerCase() === "copy" || mode.toLowerCase() === "new" ?
            `admin/lookup/add` : `admin/lookup/update`;
        axiosInstance.post(url, item)
            .then(resp => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "success", body: `Lookup item was saved.`, isTimed: true }
                    ],
                    refreshLookupData: true,
                    refreshSearchCriteria: true
                });

                //now redirect to lookup item list
                history.push(`/admin/lookup/list`);
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred ${mode.toLowerCase() === "copy" ? "copying" : "saving"} this lookup item.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });
    };

    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "name":
                item[e.target.id] = e.target.value;
                break;
            case "displayOrder":
                const re = /^[0-9\b]+$/;
                // if value is not blank, then test the regex
                if (e.target.value === '' ) {
                    item[e.target.id] = null;
                }
                else if (re.test(e.target.value)) {
                    item[e.target.id] = e.target.value;
                }
                else {
                    return;
                }
                break;
            case "lookupType":
                if (e.target.value.toString() === "-1") item.lookupType = null;
                else {
                    item.lookupType = { enumValue: parseInt(e.target.value), name: e.target.options[e.target.selectedIndex].text };
                }
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(item)));
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderMoreDropDown = () => {
        if (item == null || (mode.toLowerCase() === "copy" || mode.toLowerCase() === "new")) return;

        //React-bootstrap bug if you launch modal, then the dropdowns don't work. Add onclick code to the drop down as a workaround - https://github.com/react-bootstrap/react-bootstrap/issues/5561
        return (
            <Dropdown className="action-menu icon-dropdown ml-2" onClick={(e) => e.stopPropagation()} >
                <Dropdown.Toggle drop="left">
                    <SVGIcon name="more-vert" size="24" fill={color.shark} />
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    <Dropdown.Item href={`/admin/lookup/new`}>Add Lookup Item</Dropdown.Item>
                    <Dropdown.Item href={`/admin/lookup/copy/${item.id}`}>Copy '{item.name}'</Dropdown.Item>
                    <Dropdown.Item onClick={onDeleteItem} >Delete '{item.name}'</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
        );
    }

    const renderButtons = () => {
        if (mode.toLowerCase() !== "view") {
            return (
                <>
                    <Button variant="text-solo" className="ml-1" onClick={onCancel} >Cancel</Button>
                    <Button variant="secondary" type="button" className="ml-2" onClick={onSave} >Save</Button>
                </>
            );
        }
    }

    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderDeleteConfirmation = () => {

        if (!_deleteModal.show) return;

        var message = `You are about to delete '${_deleteModal.item.name}'. This action cannot be undone. Are you sure?`;
        var caption = `Delete Item`;

        return (
            <>
                <ConfirmationModal showModal={_deleteModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Delete", callback: onDeleteConfirm, buttonVariant: "danger" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onDeleteCancel`, CLASS_NAME));
                            setDeleteModal({ show: false, item: null });
                        },
                        buttonVariant: null
                    }} />
            </>
        );
    };

    //render error message as a modal to force user to say ok.
    const renderErrorMessage = () => {

        if (!_error.show) return;

        return (
            <>
                <ConfirmationModal showModal={_error.show} caption={_error.caption} message={_error.message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={null}
                    cancel={{
                        caption: "OK",
                        callback: () => {
                            //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
                            setError({ show: false, caption: null, message: null });
                        },
                        buttonVariant: 'danger'
                    }} />
            </>
        );
    };

    const renderLookupTypeOptions = () => {
        if (_lookupTypes == null || _lookupTypes.length === 0) return null;
        return (
            _lookupTypes.map((item) => {
                return (<option key={item.enumValue} value={item.enumValue} >{item.name}</option>)
            })
        );
    }


    const renderForm = () => {
        //console.log(item);
        return (
                <>
                <div className="row">
                    <div className="col-md-4">
                        <Form.Group>
                            <Form.Label>Lookup Type</Form.Label>
                            {!_isValid.lookupType &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="lookupType" as="select" className={(!_isValid.lookupType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" 
                                value={item.lookupType == null ? "-1" : item.lookupType.enumValue} onBlur={validateForm_lookupType} onChange={onChange} readOnly={isReadOnly} >
                                <option key="-1|Select One" value="-1" >--Select One--</option>
                                {renderLookupTypeOptions()}
                            </Form.Control>
                        </Form.Group>
                    </div>
                    <div className="col-md-4">
                        <Form.Group>
                            <Form.Label>Name</Form.Label>
                            {!_isValid.name &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="name" className={(!_isValid.name ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter unique name`}
                                value={item.name == null ? '' : item.name} onBlur={validateForm_name} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-4">
                        <Form.Group>
                            <Form.Label>Display Order</Form.Label>
                            {!_isValid.displayOrder &&
                                <span className="invalid-field-message inline">
                                    Invalid. Must be a positive integer.
                                </span>
                            }
                            <Form.Control id="displayOrder" className={(!_isValid.displayOrder ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter numeric value`}
                                value={item.displayOrder == null ? "" : item.displayOrder} onBlur={validateForm_displayOrder} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
            </>
        )
    }

    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-4">
                <div className="col-sm-12 d-flex align-items-center" >
                    {renderHeaderBlock()}
                </div>
            </div>
        );
    };

    const renderHeaderBlock = () => {

        return (
            <>
                <h1 className="m-0 mr-2">
                    Admin | {caption}
                </h1>
                <div className="ml-auto d-flex align-items-center" >
                    {renderButtons()}
                    {renderMoreDropDown()}
                </div>
            </>
        )
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (loadingProps.isLoading || isLoading) return null;

    //return final ui
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " Admin | " + caption}</title>
            </Helmet>
            <Form noValidate>
            {renderHeaderRow()}
            <div className="row" >
                <div className="col-sm-12 mb-4" >
                    {renderForm()}
                </div>
            </div>
            </Form>
            {renderDeleteConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default AdminLookupEntity;
