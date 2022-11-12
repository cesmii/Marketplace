import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'

import { AppSettings } from '../../utils/appsettings';
import { formatDate, generateLogMessageString } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import { SVGIcon } from "../../components/SVGIcon";
import color from "../../components/Constants";
import ConfirmationModal from '../../components/ConfirmationModal';

const CLASS_NAME = "AdminRequestInfoEntity";

function AdminRequestInfoEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const { id } = useParams();
    //var pageMode = //state is not always present. If user types a url or we use an href link, state is null. history.location.state.viewMode;
    //see logic below for how we calculate.
    const [mode, setMode] = useState("edit");
    const [item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const [isReadOnly, setIsReadOnly] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_isValid, setIsValid] = useState({ status: true });
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    var caption = 'Request Info';

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
                var data = { id: id };
                var url = `requestinfo/getbyid`
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
                    msg += ' You are not permitted to edit these items.';
                    history.goBack();
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
            setMode("edit");

            // set form to readonly if we're in viewmode or is deleted (isActive = false)
            setIsReadOnly(!result.data.isActive);

        }

        //fetch our data 
        // for view/edit modes
        if ((id != null)) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id]);


    //-------------------------------------------------------------------
    // Trigger get lookup data from server (if necessary)
    //-------------------------------------------------------------------
    useEffect(() => {
        //fetch referrer data 
        if (loadingProps.lookupDataStatic == null) {
            setLoadingProps({ refreshLookupData: true });
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [loadingProps.lookupDataStatic]);


    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_status = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, status: isValid });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.status = item.status != null && item.status.id.toString() !== "-1";
        setIsValid(JSON.parse(JSON.stringify(_isValid)));

        return (_isValid.status);
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
        var url = `requestinfo/delete`;
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
                    history.push('/admin/requestinfo/list');
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
        history.push('/admin/requestinfo/list');
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
        var url = `requestinfo/update`;
        axiosInstance.post(url, item)
            .then(resp => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "success", body: `Item was updated.`, isTimed: true }
                    ],
                    refreshLookupData: true,
                    refreshSearchCriteria: true
                });

                //now redirect 
                history.push('/admin/requestinfo/list');
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred saving this item.`, isTimed: false }
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
            case "notes":
                item[e.target.id] = e.target.value;
                break;
            case "status":
                if (e.target.value.toString() === "-1") item.status = null;
                else {
                    item.status = { id: e.target.value, name: e.target.options[e.target.selectedIndex].text };
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
    const renderStatus = () => {
        //show readonly input for view mode
        if (isReadOnly) {
            return (
                <Form.Group>
                    <Form.Label>Status</Form.Label>
                    <Form.Control id="status" type="" value={item.status != null ? item.status.name : ""} readOnly={isReadOnly} />
                </Form.Group>
            )
        }
        if (loadingProps.lookupDataStatic == null) return;

        //show drop down list for edit, copy mode
        var items = loadingProps.lookupDataStatic.filter((g) => {
            return g.lookupType.enumValue === AppSettings.LookupTypeEnum.TaskStatus 
        });
        const options = items.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Status</Form.Label>
                {!_isValid.status &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="status" as="select" className={(!_isValid.status ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={item.status == null ? "-1" : item.status.id}
                    onBlur={validateForm_status} onChange={onChange} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    const renderMoreDropDown = () => {
        if (item != null && !item.isActive) return;

        //React-bootstrap bug if you launch modal, then the dropdowns don't work. Add onclick code to the drop down as a workaround - https://github.com/react-bootstrap/react-bootstrap/issues/5561
        return (
            <Dropdown className="action-menu icon-dropdown ml-2" onClick={(e) => e.stopPropagation()} >
                <Dropdown.Toggle drop="left">
                    <SVGIcon name="more-vert" size="24" fill={color.shark} />
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    <Dropdown.Item onClick={onDeleteItem} >Delete Item</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
        );
    }

    const renderButtons = () => {
        if ((item != null && !item.isActive) || mode.toLowerCase() === "view" ) return;

        return (
            <>
                <Button variant="text-solo" className="ml-1" onClick={onCancel} >Cancel</Button>
                <Button variant="secondary" type="button" className="ml-2" onClick={onSave} >Save</Button>
            </>
        );
    }

    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderDeleteConfirmation = () => {

        if (!_deleteModal.show) return;

        var message = `You are about to delete a request info item. This action cannot be undone. Are you sure?`;
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

    const renderMarketplaceItem = () => {
        if (item.marketplaceItem == null) return; 
        return (
            <div className="row  mb-3 alert-info-custom rounded p-2">
                <div className="col-md-12 border-bottom">
                    <h2 className="headline-3" >Related To</h2>
                </div>
                <div className="col-md-12">
                    <Form.Label>Name:</Form.Label>
                    <span className="ml-2"><a href={`/admin/library/${item.marketplaceItem.id}`} >{item.marketplaceItem.displayName}</a></span>
                </div>
                <div className="col-md-12">
                    <Form.Label>Abstract:</Form.Label>
                    <span className="ml-2">{item.marketplaceItem.abstract}</span>
                </div>
            </div>
        );
    }

    const renderPublisher = () => {
        if (item.publisher == null) return;
        return (
            <div className="row mb-3 alert-info-custom rounded p-2">
                <div className="col-md-12 border-bottom">
                    <h2 className="headline-3" >Related To</h2>
                </div>
                <div className="col-md-12">
                    <Form.Label>Name:</Form.Label>
                    <span className="ml-2"><a href={`/admin/publisher/${item.publisher.id}`} >{item.publisher.displayName}</a></span>
                </div>
            </div>
        );
    }

    const renderSmProfile = () => {
        if (item.smProfile == null) return;
        return (
            <div className="row mb-3 alert-info-custom rounded p-2">
                <div className="col-md-12 border-bottom">
                    <h2 className="headline-3" >Related To</h2>
                </div>
                <div className="col-md-12">
                    <Form.Label>SM Profile:</Form.Label>
                    <span className="ml-2">{item.smProfile.displayName}</span>
                </div>
                <div className="col-md-12">
                    <Form.Label>Namespace:</Form.Label>
                    <span className="ml-2">{item.smProfile.namespace}</span>
                </div>
            </div>
        );
    }

    const renderForm = () => {
        if (item == null) return;
        //console.log(item);
        return (
            <>
                {!item.isActive &&
                    <div className="row">
                    <div className="col-md-12 alert alert-warning">
                            <b>Note:</b>This item was previously deleted.  
                        </div>
                    </div>
                }
                <div className="row mb-3">
                    <div className="col-md-12">
                        <Form.Label>Request Type: </Form.Label>
                        <span className="ml-2">{item.requestType != null ? item.requestType.name : ''}</span>
                    </div>
                    <div className="col-md-12">
                        <Form.Label>Sumbitted:</Form.Label>
                        <span className="ml-2">{formatDate(item.created)}</span>
                    </div>
                </div>
                { renderMarketplaceItem()}
                { renderPublisher()}
                { renderSmProfile()}
                <div className="row mb-3">
                    <div className="col-md-12 border-bottom">
                        <h2 className="headline-3" >Contact Info</h2>
                    </div>
                    <div className="col-md-12">
                        <Form.Label>First Name:</Form.Label><span className="ml-2">{item.firstName}</span>
                    </div>
                    <div className="col-md-12">
                        <Form.Label>Last Name:</Form.Label><span className="ml-2">{item.lastName}</span>
                    </div>
                    <div className="col-md-12">
                        <Form.Label>Email:</Form.Label><span className="ml-2">{item.email}</span>
                    </div>
                    <div className="col-md-12">
                        <Form.Label>Phone:</Form.Label><span className="ml-2">{item.phone}</span>
                    </div>
                    {(item.membershipStatus != null) &&
                        <div className="col-md-12">
                        <Form.Label>Membership Status:</Form.Label><span className="ml-2">{item.membershipStatus.name}</span>
                        </div>
                    }
                    {((item.companyName != null && item.companyUrl != null && item.industries != null) &&
                        (item.companyName.toString() + item.companyUrl.toString() + item.industries.toString()).length > 0) &&
                        <div className="col-md-12 border-bottom">
                            <h2 className="headline-3" >Company Info</h2>
                        </div>
                    }
                    {(item.companyName != null && item.companyName !== '') &&
                        <div className="col-md-12">
                            <Form.Label>Name:</Form.Label><span className="ml-2">{item.companyName}</span>
                        </div>
                    }
                    {(item.industries != null && item.industries !== '') &&
                        <div className="col-md-12">
                            <Form.Label>Industry(s):</Form.Label><span className="ml-2">{item.industries}</span>
                        </div>
                    }
                    {(item.companyUrl != null && item.companyUrl !== '' ) &&
                        <div className="col-md-12">
                            <Form.Label>Website:</Form.Label><span className="ml-2">{item.companyUrl}</span>
                        </div>
                    }
                </div>
                <div className="row mb-3">
                    <div className="col-md-12 border-top pt-2">
                        <Form.Label>Description:</Form.Label><span className="d-block">{item.description}</span>
                    </div>
                </div>

                <div className="row mt-2">
                    <div className="col-md-12 border-bottom">
                        <h2 className="headline-3" >Admin Use Only (internal)</h2>
                    </div>
                    <div className="col-md-4">
                        {renderStatus()}
                    </div>
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Notes</Form.Label>
                            <Form.Control id="notes" as="textarea" style={{ height: '100px' }}
                                placeholder={`Enter any notes relevant to this inquiry`}
                                value={item.notes != null ? item.notes : ''} onChange={onChange} readOnly={isReadOnly} />
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
                <title>{`${caption} | Admin | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            <Form noValidate>
            {renderHeaderRow()}
            <div className="row" >
                <div className="col-sm-11 mb-4 offset-1" >
                    {renderForm()}
                </div>
            </div>
            </Form>
            {renderDeleteConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default AdminRequestInfoEntity;
