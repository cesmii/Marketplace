import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'

import JSONInput from 'react-json-editor-ajrm';
import locale from 'react-json-editor-ajrm/locale/en';

import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString, tryJsonParse } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import { SVGIcon } from "../../components/SVGIcon";
import color from "../../components/Constants";
import ConfirmationModal from '../../components/ConfirmationModal';

const CLASS_NAME = "AdminExternalSourceEntity";

function AdminExternalSourceEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const { id, parentId } = useParams();
    //var pageMode = //state is not always present. If user types a url or we use an href link, state is null. history.location.state.viewMode;
    //see logic below for how we calculate.
    const [mode, setMode] = useState(initPageMode());
    const [_item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const [isReadOnly, setIsReadOnly] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_isValid, setIsValid] = useState({
        name: true, code: true, itemType: true, publisher: true, typeName: true, typeNameFormat: true, adminTypeNameFormat: true,
        dataFormat: true, marketplaceItem: true
    });
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    var caption = 'External Source';

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
                var url = `admin/externalSource/${parentId == null ? 'getbyid' : 'copy'}`
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this item.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This item was not found.';
                    history.push('/404');
                }
                //403 error - user may be allowed to log in but not permitted to perform the API call they are attempting
                else if (err != null && err.response != null && err.response.status === 403) {
                    console.log(generateLogMessageString('useEffect||fetchData||Permissions error - 403', CLASS_NAME, 'error'));
                    msg += ' You are not permitted to edit items.';
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

        //get a blank external source item object from server
        async function fetchDataAdd() {
            console.log(generateLogMessageString('useEffect||fetchDataAdd||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                var url = `admin/externalSource/init`
                result = await axiosInstance.post(url);
            }
            catch (err) {
                var msg = 'An error occurred retrieving the blank external source item.';
                console.log(generateLogMessageString('useEffect||fetchDataAdd||error', CLASS_NAME, 'error'));
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' A problem occurred with the add external source item screen.';
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
    // Region: hooks
    // useEffect - get static lookup data
    //-------------------------------------------------------------------

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

    const validateForm_code = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValid({ ..._isValid, code: isValid });
    };

    const validateForm_itemType = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, itemType: isValid });
    };

    const validateForm_publisher = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, publisher: isValid });
    };

    const validate_TypeName = (val) => {
        if (val == null || val.length === 0) return true;
        //no spaces, starts with char, no numbers, allows periods, underscores
        var format = /^[a-zA-Z\._]+$/;
        return format.test(val);
    }

    const validateForm_typeName = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        var isValidFormat = validate_TypeName(e.target.value);
        setIsValid({ ..._isValid, typeName: isValid, typeNameFormat: isValidFormat });
    };

    const validateForm_adminTypeName = (e) => {
        var isValidFormat = validate_TypeName(e.target.value);
        setIsValid({ ..._isValid, adminTypeNameFormat: isValidFormat });
    };

    const validate_data_json = (val) => {
        var isValid = false;
        if (val == null && val.trim().length === 0) {
            isValid = true;
        }
        else {
            isValid = tryJsonParse(val).success;
        }
        return isValid;
    };

    //validate all images
    const validateForm_image = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({
            ..._isValid, images: {
                "imagePortrait": e.target.id === "imagePortrait" ? isValid : _isValid.images.imagePortrait,
                "imageBanner": true, //e.target.id === "imageBanner" ? isValid : _isValid.images.imageBanner,
                "imageLandscape": e.target.id === "imageLandscape" ? isValid : _isValid.images.imageLandscape
            }
        });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.name = _item.name != null && _item.name.trim().length > 0;
        _isValid.code = _item.code != null && _item.code.trim().length > 0;
        _isValid.typeName = _item.name != null && _item.typeName.trim().length > 0;
        _isValid.publisher = _item.publisher != null && _item.publisher.id.toString() !== "-1";
        _isValid.typeNameFormat = validate_TypeName(_item.typeName);
        _isValid.adminTypeNameFormat = validate_TypeName(_item.adminTypeName);
        //use the value checked when we onBlur from the json editor. This will be the most up to date indicator: _isValid.dataFormat =

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.name && _isValid.typeName && _isValid.publisher && _isValid.typeNameFormat &&
            _isValid.dataFormat && _isValid.marketplaceItem);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onDeleteItem = () => {
        console.log(generateLogMessageString('onDeleteItem', CLASS_NAME));
        setDeleteModal({ show: true, item: _item });
    };

    const onDeleteConfirm = () => {
        console.log(generateLogMessageString('onDeleteConfirm', CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        var data = { id: _item.id };
        var url = `admin/externalSource/delete`;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: `Item was deleted`, isTimed: true
                            }
                        ]
                    });
                    history.push('/admin/externalsource');
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
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform insert call
        console.log(generateLogMessageString(`handleOnSave||${mode}`, CLASS_NAME));
        var url = mode.toLowerCase() === "copy" || mode.toLowerCase() === "new" ?
            `admin/externalSource/add` : `admin/externalSource/update`;
        axiosInstance.post(url, _item)
            .then(resp => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "success", body: `External source item was saved.`, isTimed: true }
                    ],
                    refreshSearchCriteria: true
                });

                history.push(`/admin/externalSource/${resp.data.data}`);
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred ${mode.toLowerCase() === "copy" ? "copying" : "saving"} this external source item.`, isTimed: false }
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

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "name":
            case "code":
            case "baseUrl":
            case "adminTypeName":
            case "typeName":
                _item[e.target.id] = e.target.value;
                break;
            case "enabled":
                _item[e.target.id] = e.target.checked;
                break;
            case "itemType":
            case "publisher":
                if (e.target.value.toString() === "-1") _item[e.target.id] = null;
                else {
                    _item[e.target.id] = { id: e.target.value, name: e.target.options[e.target.selectedIndex].text };
                }
                break;
            case "data":
                if (e.target.value === '') {
                    _item[e.target.value] = null;
                    setIsValid({ ..._isValid, dataFormat: true });
                }
                else {
                    var test = validate_data_json(e.target.value);
                    setIsValid({ ..._isValid, dataFormat: test });
                    if (test) {
                        _item[e.target.id] = e.target.value;
                    }
                    else {
                        console.log(generateLogMessageString('onChange||data||Could not parse JSON value:' + test.e, CLASS_NAME, 'error'));
                    }
                }
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

    //on change handler to update state
    const onChangeImageSelect = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "defaultImageBanner":
            case "defaultImageLandscape":
            case "defaultImagePortrait":
                if (e.target.value.toString() === "-1") _item[e.target.id] = null;
                else {
                    _item[e.target.id] = { id: e.target.value, fileName: e.target.options[e.target.selectedIndex].text };
                }
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

    //on change handler to update state
    const onBlurData = (e) => {
        console.log(generateLogMessageString('onBlurData||data', CLASS_NAME));
        //console.log(e);
        if (e.error) {
            setIsValid({ ..._isValid, dataFormat: false });
        }
        else {
            setIsValid({ ..._isValid, dataFormat: true });
            setItem({ ..._item, data: JSON.stringify(e.jsObject) });
        }
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderItemType = () => {
        //show readonly input for view mode
        if (isReadOnly) {
            return (
                <Form.Group>
                    <Form.Label>Item Type</Form.Label>
                    <Form.Control id="itemType" type="" value={_item.itemType != null ? _item.itemType.name : ""} readOnly={isReadOnly} />
                </Form.Group>
            )
        }
        if (loadingProps.lookupDataStatic == null) return;

        //show drop down list for edit, copy mode
        var items = loadingProps.lookupDataStatic.filter((g) => {
            return g.lookupType.enumValue === AppSettings.LookupTypeEnum.SmItemType //
        });
        const options = items.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Item Type</Form.Label>
                {!_isValid.itemType &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="itemType" as="select" className={(!_isValid.itemType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={_item.itemType == null ? "-1" : _item.itemType.id}
                    onBlur={validateForm_itemType} onChange={onChange} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    const renderPublisher = () => {
        //show readonly input for view mode
        if (isReadOnly) {
            return (
                <Form.Group>
                    <Form.Label>Publisher</Form.Label>
                    <Form.Control id="publisher" type="" value={_item.publisher != null ? _item.publisher.displayName : ""} readOnly={isReadOnly} />
                </Form.Group>
            )
        }
        if (loadingProps.lookupDataStatic == null) return;

        //show drop down list for edit, copy mode
        var items = loadingProps.lookupDataStatic.filter((g) => {
            return g.lookupType.enumValue === AppSettings.LookupTypeEnum.Publisher //
        });
        const options = items.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Publisher</Form.Label>
                {!_isValid.publisher &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="publisher" as="select" className={(!_isValid.publisher ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={_item.publisher == null ? "-1" : _item.publisher.id}
                    onBlur={validateForm_publisher} onChange={onChange} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    const renderMoreDropDown = () => {
        if (_item == null || (mode.toLowerCase() === "copy" || mode.toLowerCase() === "new")) return;

        //React-bootstrap bug if you launch modal, then the dropdowns don't work. Add onclick code to the drop down as a workaround - https://github.com/react-bootstrap/react-bootstrap/issues/5561
        return (
            <Dropdown className="action-menu icon-dropdown ml-2" onClick={(e) => e.stopPropagation()} >
                <Dropdown.Toggle drop="left">
                    <SVGIcon name="more-vert" size="24" fill={color.shark} />
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    <Dropdown.Item href={`/admin/externalSource/new`}>Add External Source</Dropdown.Item>
                    <Dropdown.Item href={`/admin/externalSource/copy/${_item.id}`}>Copy '{_item.name}'</Dropdown.Item>
                    <Dropdown.Item onClick={onDeleteItem} >Delete '{_item.name}'</Dropdown.Item>
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
        var captionModal = `Delete Item`;

        return (
            <>
                <ConfirmationModal showModal={_deleteModal.show} caption={captionModal} message={message}
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
                            setError({ show: false, caption: null, message: null });
                        },
                        buttonVariant: 'danger'
                    }} />
            </>
        );
    };

    const renderForm = () => {
        return (
            <>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label>Name</Form.Label>
                            {!_isValid.name &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="name" className={(!_isValid.name ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter unique name`}
                                value={_item.name == null ? '' : _item.name} onBlur={validateForm_name} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label>Code</Form.Label>
                            {!_isValid.code &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="code" className={(!_isValid.code ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter unique code`}
                                value={_item.code == null ? '' : _item.code} onBlur={validateForm_code} onChange={onChange} readOnly={isReadOnly || (id != null && id.toString() !== 'new')} />
                        </Form.Group>
                    </div>
                    <div className="col-md-6">
                        <span className="small text-muted" >This is a friendly name associated with this type of data. It is not displayed in the system.</span>
                    </div>
                    <div className="col-md-6">
                        <span className="small text-muted" >This code plays a key role in the system and should be unique across external sources. This will factor into navigation and be a key for finding this source.</span>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-md-6">
                        {renderPublisher()}
                    </div>
                    <div className="col-md-6">
                        {renderItemType()}
                    </div>
                    <div className="col-md-6">
                    </div>
                    <div className="col-md-6">
                        <span className="small text-muted" >The items from this API are associated with this item type.</span>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Base Url</Form.Label>
                            <Form.Control id="baseUrl" className='minimal pr-5' type="" placeholder={`Enter base url of the API.`}
                                value={_item.baseUrl == null ? '' : _item.baseUrl} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-12 pb-2">
                        <a className="mr-2 font-weight-bold small" href={_item.baseUrl} target="_blank">Test Link</a>
                        <span className="small text-muted" >This will be used as the base url when making API calls to this source. </span>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Type Name</Form.Label>
                            {!_isValid.typeName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            {!_isValid.typeNameFormat &&
                                <span className="invalid-field-message inline">
                                    No spaces, numbers or special characters (except periods ".")
                                </span>
                            }
                            <Form.Control id="typeName" className={(!_isValid.typeName || !_isValid.typeNameFormat ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter valid type name`}
                                value={_item.typeName == null ? '' : _item.typeName} onBlur={validateForm_typeName} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-12 pb-2">
                        <span className="small text-muted" >This is the fully qualified type name used by the marketplace search to call the external source API. The code will attempt to instantiate a class of this type. So, this requires companion code to be written. (Ex: CESMII.Marketplace.DAL.ExternalSources.CloudLibDAL)</span>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Admin Type Name</Form.Label>
                            {!_isValid.adminTypeNameFormat &&
                                <span className="invalid-field-message inline">
                                    No spaces, numbers or special characters (except periods ".")
                                </span>
                            }
                            <Form.Control id="adminTypeName" className={(!_isValid.adminTypeNameFormat ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter unique name`}
                                value={_item.adminTypeName == null ? '' : _item.adminTypeName} onBlur={validateForm_adminTypeName} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-12 pb-2">
                        <span className="small text-muted" >Optional. This is the fully qualified type name used by the marketplace related items feature. The code will attempt to pull items from this external source so they can be associated with other items in the marketplace. (Ex: CESMII.Marketplace.DAL.ExternalSources.AdminCloudLibDAL)</span>
                    </div>
                </div>
                <div className="row">
                    <div className="col-sm-6 col-lg-4">
                        <div className="d-flex h-100">
                            <Form.Group>
                                <Form.Check className="align-self-end" type="checkbox" id="enabled" label="Enabled" checked={_item.enabled}
                                    onChange={onChange} readOnly={isReadOnly} />
                            </Form.Group>
                        </div>
                    </div>
                    <div className="col-md-12 pb-2">
                        <span className="small text-muted" >Checking this value will cause this item to be included in searches within the marketplace.</span>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Data (JSON structure)</Form.Label>
                            {!_isValid.dataFormat &&
                                <span className="invalid-field-message inline">
                                    Invalid JSON structure
                                </span>
                            }
                            <JSONInput
                                id='data'
                                placeholder={_item.data == null ? {} : JSON.parse(_item.data)}
                                locale={locale}
                                colors={{
                                    // overrides theme colors with whatever color value you want
                                    default: color.textPrimary,
                                    keys: color.cardinal,
                                    colon: color.cardinal,
                                    background: "#ffffff",
                                    background_warning: color.transparent,
                                    error: color.textSecondary
                                }}
                                height='200px'
                                width="100%"
                                waitAfterKeyPress={2000}
                                onBlur={onBlurData}
                                viewOnly={isReadOnly}
                            />
                            <span className="small text-muted" >Optional. This is JSON data that will be used by the external source DAL to help it get data properly from the external source system.</span>
                        </Form.Group>
                    </div>
                </div>
            </>
        )
    }

    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-4">
                <div className="col-sm-9 d-flex align-items-center mx-auto" >
                    {renderHeaderBlock()}
                </div>
            </div>
        );
    };

    const renderHeaderBlock = () => {

        return (
            <>
                <h1 className="m-0 mr-2">
                    Admin - {caption}
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
                <title>{`${caption} | Admin | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            <Form noValidate>
                {renderHeaderRow()}
                <div className="row" >
                    <div className="col-sm-9 mb-4 mx-auto" >
                        {renderForm()}
                    </div>
                </div>
            </Form>
            {renderDeleteConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default AdminExternalSourceEntity;
