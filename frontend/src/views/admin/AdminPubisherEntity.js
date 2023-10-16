import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'

import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString, validate_NoSpecialCharacters } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import { SVGIcon } from "../../components/SVGIcon";
import color from "../../components/Constants";
import MultiSelect from '../../components/MultiSelect';
import ConfirmationModal from '../../components/ConfirmationModal';
import { WysiwygEditor } from '../../components/WysiwygEditor';

const CLASS_NAME = "AdminPubisherEntity";

function AdminPubisherEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const { id, parentId } = useParams();
    //var pageMode = //state is not always present. If user types a url or we use an href link, state is null. history.location.state.viewMode;
    //see logic below for how we calculate.
    const [mode, setMode] = useState(initPageMode());
    const [item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const [isReadOnly, setIsReadOnly] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_isValid, setIsValid] = useState({ name: true, nameFormat: true, displayName: true, description: true});
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    var caption = 'Publisher';

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
                var url = `admin/publisher/${parentId == null ? 'getbyid' : 'copy'}`
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this publisher item.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This publisher item was not found.';
                    history.push('/404');
                }
                //403 error - user may be allowed to log in but not permitted to perform the API call they are attempting
                else if (err != null && err.response != null && err.response.status === 403) {
                    console.log(generateLogMessageString('useEffect||fetchData||Permissions error - 403', CLASS_NAME, 'error'));
                    msg += ' You are not permitted to edit publisher items.';
                    history.goBack();
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            var thisMode = (parentId != null) ? 'copy' : 'edit';

            //convert collection to comma separated list
            //special handling of meta tags which shows as a concatenated list in an input box
            result.data.metaTagsConcatenated = result.data == null || result.data.metaTags == null ? "" : result.data.metaTags.join(', ');
            //set item state value
            setItem(result.data);
            setIsLoading(false);
            setLoadingProps({ isLoading: false, message: null });
            setMode(thisMode);

            // set form to readonly if we're in viewmode or is deleted (isActive = false)
            setIsReadOnly(thisMode.toLowerCase() === "view" || !result.data.isActive);

        }

        //get a blank publisher item object from server
        async function fetchDataAdd() {
            console.log(generateLogMessageString('useEffect||fetchDataAdd||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                var url = `admin/publisher/init`
                result = await axiosInstance.post(url);
            }
            catch (err) {
                var msg = 'An error occurred retrieving the blank publisher item.';
                console.log(generateLogMessageString('useEffect||fetchDataAdd||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' A problem occurred with the add publisher item screen.';
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
    }, [id, parentId]);


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
        var isValidFormat = validate_NoSpecialCharacters(e.target.value);
        setIsValid({ ..._isValid, name: isValid, nameFormat: isValidFormat });
    };

    const validateForm_displayName = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, displayName: isValid });
    };

    const validateForm_description = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, description: isValid });
    };

    //const validateForm_status = (e) => {
    //    var isValid = e.target.value.toString() !== "-1";
    //    setIsValid({ ..._isValid, status: isValid });
    //};

    //const validateForm_publisher = (e) => {
    //    var isValid = e.target.value.toString() !== "-1";
    //    setIsValid({ ..._isValid, publisher: isValid });
    //};

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.name = item.name != null && item.name.trim().length > 0;
        _isValid.nameFormat = validate_NoSpecialCharacters(item.name);
        _isValid.displayName = item.displayName != null && item.displayName.trim().length > 0;
        _isValid.description = true; //item.description != null && item.description.trim().length > 0;
        _isValid.status = item.status != null && item.status.id.toString() !== "-1";
        _isValid.publisher = item.publisher != null && item.publisher.id.toString() !== "-1";

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.name && _isValid.nameFormat && _isValid.displayName && _isValid.description );
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
        var url = `admin/publisher/delete`;
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
                        refreshLookupData: true
                    });
                    history.push('/library');
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Delete Item Error', message: result.data.message });
                    setLoadingProps({ isLoading: false, message: null });
                    setDeleteModal({ show: false, item: null});
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
            `admin/publisher/add` : `admin/publisher/update`;
        axiosInstance.post(url, item)
            .then(resp => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "success", body: `Publisher item was saved.`, isTimed: true }
                    ],
                    refreshLookupData: true,
                    refreshSearchCriteria: true
                });

                //now redirect to publisher item on front end
                history.push(`/admin/publisher/${resp.data.data}`);
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred ${mode.toLowerCase() === "copy" ? "copying" : "saving"} this publisher item.`, isTimed: false }
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
            case "displayName":
            case "description":
            case "companyUrl":
            case "metaTagsConcatenated":
                item[e.target.id] = e.target.value;
                break;
            case "name":
                item[e.target.id] = e.target.value.toLowerCase();
                break;
            case "allowFilterBy":
            case "verified":
                item[e.target.id] = e.target.checked;
                break;
            //case "publisher":
            //    if (e.target.value.toString() === "-1") item.publisher = null;
            //    else {
            //        item.publisher = { id: e.target.value, name: e.target.options[e.target.selectedIndex].text };
            //    }
            //    break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(item)));
    }

    const onItemSelectIndustryVertical = (vert) => {
        console.log(generateLogMessageString('onItemSelectIndustryVertical', CLASS_NAME));
        var match = item.industryVerticals.find(x => x.id === vert.id);
        if (match != null) {
            match.selected = vert.selected;
            setItem(JSON.parse(JSON.stringify(item)));
        }
    };

    const onItemSelectCategory = (cat) => {
        console.log(generateLogMessageString('onItemSelectCategory', CLASS_NAME));
        var match = item.categories.find(x => x.id === cat.id);
        if (match != null) {
            match.selected = cat.selected;
            setItem(JSON.parse(JSON.stringify(item)));
        }
    };

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
                    <Dropdown.Item href={`/admin/publisher/new`}>Add Publisher</Dropdown.Item>
                    <Dropdown.Item href={`/admin/publisher/copy/${item.id}`}>Copy '{item.name}'</Dropdown.Item>
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

    const renderForm = () => {
        //console.log(item);
        return (
                <>
                <div className="row">
                    <div className="col-md-8">
                        <Form.Group>
                            <Form.Label>Name</Form.Label>
                            {!_isValid.name &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            {!_isValid.nameFormat &&
                                <span className="invalid-field-message inline">
                                    No spaces or special characters
                                </span>
                            }
                            <Form.Control id="name" className={(!_isValid.name || !_isValid.nameFormat ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter unique name`}
                                value={item.name} onBlur={validateForm_name} onChange={onChange} readOnly={isReadOnly} />
                            <span className="small text-muted" >This will be used in the formation of the url for this item. This must be unique and contain no spaces nor special characters.</span>
                        </Form.Group>
                    </div>
                    <div className="col-md-4">
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Display Name</Form.Label>
                            {!_isValid.displayName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="displayName" className={(!_isValid.displayName ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter friendly name displayed on all screens`}
                                value={item.displayName} onBlur={validateForm_displayName} onChange={onChange} readOnly={isReadOnly} />
                            <span className="small text-muted" >This will be used on all screens for the display of this item. This can contain spaces, special characters, etc. </span>
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-12">
                        <div className="d-flex h-100">
                            <Form.Group>
                                <Form.Check className="align-self-end" type="checkbox" id="verified" label="Verified" checked={item.verified}
                                    onChange={onChange} readOnly={isReadOnly} />
                            </Form.Group>
                        </div>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-12">
                        <div className="d-flex h-100">
                            <Form.Group>
                                <Form.Check className="align-self-end" type="checkbox" id="allowFilterBy" label="Allow Filter By" checked={item.allowFilterBy}
                                    onChange={onChange} readOnly={isReadOnly} />
                                <span className="small text-muted" >Unchecking 'Allow Filter By' will hide this from being a search filter. This is not common.</span>
                            </Form.Group>
                        </div>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Description</Form.Label>
                            {!_isValid.description &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <WysiwygEditor id="description" value={item.description} onChange={onChange} onValidate={validateForm_description} className={(!_isValid.description ? 'invalid-field' : '')} />
                        </Form.Group>
                    </div>
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Company Url</Form.Label>
                            <Form.Control id="companyUrl" type="" placeholder="Enter company url"
                                value={item.companyUrl} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
            </>
        )
    }

    const renderMultiSelectAreas = () => {
        if (item == null) return;
        return (
            <>
                <MultiSelect items={item.industryVerticals} caption="IndustryVerticals" onItemSelect={onItemSelectIndustryVertical} />
                <MultiSelect items={item.categories} caption="Processes" onItemSelect={onItemSelectCategory} />
            </>
        );
    }

    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-4">
                <div className="col-sm-3" >
                    <div className="header-title-block d-flex align-items-center">
                        <span className="headline-1">Admin</span>
                        {renderSubTitle()}
                    </div>
                </div>
                <div className="col-sm-9 d-flex align-items-center" >
                    {renderHeaderBlock()}
                </div>
            </div>
        );
    };

    const renderHeaderBlock = () => {

        return (
            <>
                <h1 className="m-0 mr-2">
                    {caption}
                </h1>
                <div className="ml-auto d-flex align-items-center" >
                    {renderButtons()}
                    {renderMoreDropDown()}
                </div>
            </>
        )
    }


    const renderSubTitle = () => {
        if (mode === "new" || mode === "copy") return;
        return (
            <a className="px-2 btn btn-text-solo align-items-center auto-width ml-auto justify-content-end d-flex" href={`/publisher/${item.name}`} ><i className="material-icons">visibility</i>View</a>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (loadingProps.isLoading || isLoading) return null;

    //return final ui
    return (
        <>
            <Helmet>
                <title>{`${caption} | Admin | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            <Form noValidate>
            {renderHeaderRow()}
            <div className="row" >
                <div className="col-sm-3" >
                    {renderMultiSelectAreas()}
                </div>
                <div className="col-sm-9 mb-4" >
                    {renderForm()}
                </div>
            </div>
            </Form>
            {renderDeleteConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default AdminPubisherEntity;
