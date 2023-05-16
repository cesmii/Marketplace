import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'

import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString, prepDateVal, validate_NoSpecialCharacters } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import { SVGIcon } from "../../components/SVGIcon";
import color from "../../components/Constants";
import MultiSelect from '../../components/MultiSelect';
import ConfirmationModal from '../../components/ConfirmationModal';
import { WysiwygEditor } from '../../components/WysiwygEditor';
import AdminImageList from './shared/AdminImageList';
import AdminRelatedItemList from './shared/AdminRelatedItemList';
import { clearSearchCriteria } from '../../services/MarketplaceService';

const CLASS_NAME = "AdminMarketplaceEntity";

function AdminMarketplaceEntity() {

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
    const [_isValid, setIsValid] = useState({
        name: true, nameFormat: true, displayName: true, abstract: true, description: true,
        status: true, type: true, publisher: true, publishDate: true,
        images: { imagePortrait: true, imageSquare: true, imageLandscape: true}
    });
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const [_refreshImageData, setRefreshImageData] = useState(true);
    const [_imageRows, setImageRows] = useState([]);
    var caption = 'Marketplace Item';

    const [_itemsLookup, setItemsLookup] = useState([]);  //marketplace items 
    const [_loadLookupData, setLoadLookupData] = useState(null);

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
                var url = `admin/marketplace/${parentId == null ? 'getbyid' : 'copy'}`
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this marketplace item.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This marketplace item was not found.';
                    history.push('/404');
                }
                //403 error - user may be allowed to log in but not permitted to perform the API call they are attempting
                else if (err != null && err.response != null && err.response.status === 403) {
                    console.log(generateLogMessageString('useEffect||fetchData||Permissions error - 403', CLASS_NAME, 'error'));
                    msg += ' You are not permitted to edit marketplace items.';
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

        //get a blank marketplace item object from server
        async function fetchDataAdd() {
            console.log(generateLogMessageString('useEffect||fetchDataAdd||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                var url = `admin/marketplace/init`
                result = await axiosInstance.post(url);
            }
            catch (err) {
                var msg = 'An error occurred retrieving the blank marketplace item.';
                console.log(generateLogMessageString('useEffect||fetchDataAdd||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' A problem occurred with the add marketplace item screen.';
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
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchImageData() {

            var url = `image/all`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.post(url, {id: id}).then(result => {
                if (result.status === 200) {
                    setImageRows(result.data);
                } else {
                    setImageRows(null);
                }
                setRefreshImageData(false);
            }).catch(e => {
                if (e.response && e.response.status === 401) {
                }
                else {
                    console.log(generateLogMessageString('useEffect||fetchData||' + JSON.stringify(e), CLASS_NAME, 'error'));
                    console.log(e);
                    setRefreshImageData(false);
                }
            });
        };

        if (_refreshImageData) {
            //console.log(generateLogMessageString('useEffect||refreshLookupData||Trigger fetch', CLASS_NAME));
            fetchImageData();
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||refreshLookupData||Cleanup', CLASS_NAME));
        };
    }, [id, _refreshImageData]);

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
    // Trigger get related items lookups - all mktplace items, all profiles.
    //-------------------------------------------------------------------
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchData(url) {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            //get copy of search criteria structure from session storage
            var criteria = JSON.parse(JSON.stringify(loadingProps.searchCriteria));
            criteria = clearSearchCriteria(criteria);
            criteria = { ...criteria, Query: null, Skip: 0, Take: 999 };
            await axiosInstance.post(url, criteria).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setItemsLookup(result.data);
                    setLoadLookupData(false);

                    //hide a spinner
                    setLoadingProps({ isLoading: false, message: null });
                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these items.', isTimed: true }]
                    });
                }
                //hide a spinner
                setLoadingProps({ isLoading: false, message: null });
                setLoadLookupData(false);

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the related items.', isTimed: true }]
                    });
                }
                setLoadLookupData(false);
            });
        }

        //go get the data.
        if (_loadLookupData == null || _loadLookupData === true) {
            fetchData(`marketplace/admin/lookup/related`);
        }

        //this will execute on unmount
        return () => {
            //
        };
    }, [_loadLookupData]);

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

    const validateForm_abstract = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, abstract: isValid });
    };

    const validateForm_description = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, description: isValid });
    };

    const validateForm_status = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, status: isValid });
    };

    const validateForm_type = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, type: isValid });
    };

    const validateForm_publisher = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, publisher: isValid });
    };

    //validate all images
    const validateForm_image = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({
            ..._isValid, images: {
                "imagePortrait": e.target.id === "imagePortrait" ? isValid : _isValid.images.imagePortrait,
                "imageSquare": true, //e.target.id === "imageSquare" ? isValid : _isValid.images.imageSquare,
                "imageLandscape": e.target.id === "imageLandscape" ? isValid : _isValid.images.imageLandscape
            }
        });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.name = item.name != null && item.name.trim().length > 0;
        _isValid.nameFormat = validate_NoSpecialCharacters(item.name);
        _isValid.displayName = item.displayName != null && item.displayName.trim().length > 0;
        _isValid.description = true; //item.description != null && item.description.trim().length > 0;
        _isValid.status = item.status != null && item.status.id.toString() !== "-1";
        _isValid.type = item.type != null && item.type.id.toString() !== "-1";
        _isValid.publisher = item.publisher != null && item.publisher.id.toString() !== "-1";
        _isValid.publishDate = item.publishDate != null && item.publishDate.trim().length > 0;
        _isValid.images.imagePortrait = item.imagePortrait != null && item.imagePortrait.id.toString() !== "-1";
        _isValid.images.imageSquare = true; //item.imageSquare != null && item.imageSquare.id.toString() !== "-1";
        _isValid.images.imageLandscape = item.imageLandscape != null && item.imageLandscape.id.toString() !== "-1";

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.name && _isValid.nameFormat && _isValid.displayName && _isValid.abstract && _isValid.description &&
            _isValid.status && _isValid.publisher && _isValid.publishDate &&
            _isValid.images.imagePortrait && _isValid.images.imageSquare && _isValid.images.imageLandscape &&
            _isValid.type);
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
        var url = `admin/marketplace/delete`;
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
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Delete Item Error', message: `An error occurred deleting this item: ${result.data.message}` });
                    setLoadingProps({ isLoading: false, message: null });
                }
                history.push('/library');
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

        //convert my metatags string back into array representation for saving
        //split the string into array and then build out array of tags
        item.metaTags = item.metaTagsConcatenated == null || item.metaTagsConcatenated.trim().length === 0 ?
            null : item.metaTagsConcatenated.split(",").map(x => x.trim(' '));

        //perform insert call
        console.log(generateLogMessageString(`handleOnSave||${mode}`, CLASS_NAME));
        var url = mode.toLowerCase() === "copy" || mode.toLowerCase() === "new" ?
            `admin/marketplace/add` : `admin/marketplace/update`;
        axiosInstance.post(url, item)
            .then(resp => {
                if (resp.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "success", body: `Marketplace item was saved`, isTimed: true }
                        ]
                    });

                    //now redirect to marketplace item on front end
                    history.push(`/admin/library/${resp.data.data}`);
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Save Error', message: resp.data.message });
                    setLoadingProps({ isLoading: false, message: null });
                }

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred ${mode.toLowerCase() === "copy" ? "copying" : "saving"} this marketplace item.`, isTimed: false }
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

    //on change publish date handler to update state
    const onChangePublishDate = (e) => {

        //if user types directly into year field, it prematurely does an onChange event fire.
        //This prevents that:
        if (e.target.value !== '') {
            var dt = new Date(e.target.value);
            if (dt.getFullYear() < 2000) return;
        }

        //update the state
        item[e.target.id] = e.target.value === '' ? null : e.target.value;
        setItem(JSON.parse(JSON.stringify(item)));
    }

    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "displayName":
            case "description":
            case "abstract":
            case "version":
            case "metaTagsConcatenated":
            case "ccName1":
            case "ccName2":
            case "ccEmail1":
            case "ccEmail2":
                item[e.target.id] = e.target.value;
                break;
            case "name":
                item[e.target.id] = e.target.value.toLowerCase();
                break;
            case "isFeatured":
            case "isVerified":
                item[e.target.id] = e.target.checked;
                break;
            case "status":
            case "type":
            case "publisher":
                if (e.target.value.toString() === "-1") item[e.target.id] = null;
                else {
                    item[e.target.id] = { id: e.target.value, name: e.target.options[e.target.selectedIndex].text };
                }
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(item)));
    }

    //on change handler to update state
    const onChangeImageSelect = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "imagePortrait":
            case "imageSquare":
            case "imageLandscape":
                if (e.target.value.toString() === "-1") item[e.target.id] = null;
                else {
                    item[e.target.id] = { id: e.target.value, fileName: e.target.options[e.target.selectedIndex].text };
                }
                break;
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
    // Region: Event handler - Images
    //-------------------------------------------------------------------
    const onImageUpload = (imgs) => {
        //trigger api call to get latest. 
        setRefreshImageData(true);
    }

    const onDeleteImage = (id) => {
        //trigger api call to get latest. 
        setRefreshImageData(true);
    }

    //-------------------------------------------------------------------
    // Region: Event handler - related items, related profiles
    //-------------------------------------------------------------------
    const onChangeRelatedItem = (currentId, arg) => {
        console.log(generateLogMessageString('onChangeRelatedItem', CLASS_NAME));
        var match = item.relatedItems.find(x => x.relatedId === currentId);
        match.relatedId = arg.relatedId;
        match.displayName = arg.displayName;
        match.relatedType = arg.relatedType;
        setItem(JSON.parse(JSON.stringify(item)));
    }

    const onChangeRelatedProfile = (currentId, arg) => {
        console.log(generateLogMessageString('onChangeRelatedProfile', CLASS_NAME));
        var match = item.relatedProfiles.find(x => x.relatedId === currentId);
        match.relatedId = arg.relatedId;
        match.displayName = arg.displayName;
        match.relatedType = arg.relatedType;
        setItem(JSON.parse(JSON.stringify(item)));
    }

    const onAddRelatedItem = () => {
        console.log(generateLogMessageString('onAddRelatedItem', CLASS_NAME));
        //we need to be aware of newly added rows and those will be signified by a negative -id. 
        //Once saved server side, these will be issued ids from db.
        //Depending on how we are adding (single row or multiple rows), the id generation will be different. Both need 
        //a starting point negative id
        var id = (-1) * (item.relatedItems == null ? 1 : item.relatedItems.length + 1);

        item.relatedItems.push({ relatedId: id, relatedType: { id: "-1" } });
        setItem(JSON.parse(JSON.stringify(item)));
    }

    const onAddRelatedProfile = () => {
        console.log(generateLogMessageString('onAddRelatedProfile', CLASS_NAME));
        //we need to be aware of newly added rows and those will be signified by a negative -id. 
        //Once saved server side, these will be issued ids from db.
        //Depending on how we are adding (single row or multiple rows), the id generation will be different. Both need 
        //a starting point negative id
        var id = (-1) * (item.relatedProfiles == null ? 1 : item.relatedProfiles.length + 1);
        item.relatedProfiles.push({ relatedId: id, relatedType: { id: "-1" } });
        setItem(JSON.parse(JSON.stringify(item)));
    }

    const onDeleteRelatedItem = (e, id) => {
        console.log(generateLogMessageString('onDeleteRelatedItem', CLASS_NAME));
        item.relatedItems = item.relatedItems.filter(x => x.relatedId !== id);
        setItem(JSON.parse(JSON.stringify(item)));
    }

    const onDeleteRelatedProfile = (e, id) => {
        console.log(generateLogMessageString('onDeleteRelatedProfile', CLASS_NAME));
        item.relatedProfiles = item.relatedProfiles.filter(x => x.relatedId !== id);
        setItem(JSON.parse(JSON.stringify(item)));
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderMarketplaceStatus = () => {
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
            return g.lookupType.enumValue === AppSettings.LookupTypeEnum.MarketplaceStatus //
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

    const renderItemType = () => {
        //show readonly input for view mode
        if (isReadOnly) {
            return (
                <Form.Group>
                    <Form.Label>Type</Form.Label>
                    <Form.Control id="type" type="" value={item.type != null ? item.type.name : ""} readOnly={isReadOnly} />
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
                <Form.Label>Type</Form.Label>
                {!_isValid.type &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="type" as="select" className={(!_isValid.type ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={item.type == null ? "-1" : item.type.id}
                    onBlur={validateForm_type} onChange={onChange} >
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
                    <Form.Control id="publisher" type="" value={item.publisher != null ? item.publisher.displayName : ""} readOnly={isReadOnly} />
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
                <Form.Control id="publisher" as="select" className={(!_isValid.publisher ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={item.publisher == null ? "-1" : item.publisher.id}
                    onBlur={validateForm_publisher} onChange={onChange} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    const renderMoreDropDown = () => {
        if (item == null || (mode.toLowerCase() === "copy" || mode.toLowerCase() === "new")) return;

        //React-bootstrap bug if you launch modal, then the dropdowns don't work. Add onclick code to the drop down as a workaround - https://github.com/react-bootstrap/react-bootstrap/issues/5561
        return (
            <Dropdown className="action-menu icon-dropdown ml-2" onClick={(e) => e.stopPropagation()} >
                <Dropdown.Toggle drop="left">
                    <SVGIcon name="more-vert" size="24" fill={color.shark} />
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    <Dropdown.Item href={`/admin/library/new`}>Add Marketplace Item</Dropdown.Item>
                    <Dropdown.Item href={`/admin/library/copy/${item.id}`}>Copy '{item.name}'</Dropdown.Item>
                    <Dropdown.Item onClick={onDeleteItem} >Delete '{item.name}'</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers - Immage selection
    //-------------------------------------------------------------------
    const renderImageSelection = (fieldName, caption, infoText) => {
        //show readonly input for view mode
        if (isReadOnly) {
            return (
                <Form.Group>
                    <Form.Label>{caption}</Form.Label>
                    <Form.Control id={fieldName} type="" value={item[fieldName] != null ? item[fieldName].fileName : ""} readOnly={isReadOnly} />
                </Form.Group>
            )
        }
        if (_imageRows == null) return;

        //show drop down list for edit, copy mode
        const options = _imageRows.map((item) => {
            return (<option key={item.id} value={item.id} >{item.fileName}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>{caption}</Form.Label>
                {!_isValid.images[fieldName] &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id={fieldName} as="select" className={(!_isValid.images[fieldName] ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={item[fieldName] == null ? "-1" : item[fieldName].id}
                    onBlur={validateForm_image} onChange={onChangeImageSelect} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
                {infoText != null &&
                    <span className="small text-muted">
                        {infoText}
                    </span>
                }
            </Form.Group>
        )
    };


    const renderButtons = () => {
        if (mode.toLowerCase() !== "view") {
            return (
                <>
                    <Button variant="text-solo" className="ml-1" href="/admin/library/list" >Cancel</Button>
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
                            <Form.Control id="name" className={(!_isValid.name || !_isValid.nameFormat ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type="" placeholder={`Enter unique name with no spaces or special characters. `}
                                value={item.name} onBlur={validateForm_name} onChange={onChange} readOnly={isReadOnly} />
                            <span className="small text-muted" >This will be used in the formation of the url for this item. This must be unique and contain no spaces nor special characters.</span>
                        </Form.Group>
                    </div>
                    <div className="col-md-4">
                        {renderMarketplaceStatus()}
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-12 pt-2">
                        <AdminRelatedItemList caption="Related Marketplace Items" items={item.relatedItems}
                            itemsLookup={_itemsLookup?.lookupItems}
                            type={AppSettings.itemTypeCode.smApp} onChangeItem={onChangeRelatedItem}
                            onAdd={onAddRelatedItem} onDelete={onDeleteRelatedItem} />
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-12 pt-2">
                        <AdminRelatedItemList caption="Related SM Profiles" items={item.relatedProfiles}
                            itemsLookup={_itemsLookup?.lookupProfiles}
                            type={AppSettings.itemTypeCode.smProfile} onChangeItem={onChangeRelatedProfile}
                            onAdd={onAddRelatedProfile} onDelete={onDeleteRelatedProfile} />
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-8">
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
                    <div className="col-md-4">
                        {renderItemType()}
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-sm-6 col-lg-4">
                        <Form.Group>
                            <Form.Label>Version</Form.Label>
                            <Form.Control id="version" type="" placeholder=""
                                value={item.version == null ? '' : item.version} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-sm-6 col-lg-4">
                        <Form.Group>
                            <Form.Label>Publish Date</Form.Label>
                            {!_isValid.publishDate &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="publishDate" mindate="2010-01-01" type="date" className={(!_isValid.publishDate ? 'invalid-field' : '')}
                                value={prepDateVal(item.publishDate)} onChange={onChangePublishDate} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-sm-6 col-lg-4">
                        <div className="d-flex h-100">
                            <Form.Group>
                                <Form.Check className="align-self-end" type="checkbox" id="isVerified" label="Verified" checked={item.isVerified}
                                    onChange={onChange} readOnly={isReadOnly} />
                            </Form.Group>
                            <Form.Group className="ml-4">
                                <Form.Check className="align-self-end" type="checkbox" id="isFeatured" label="Featured" checked={item.isFeatured}
                                    onChange={onChange} readOnly={isReadOnly} />
                            </Form.Group>
                        </div>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-sm-6 col-lg-4">
                        <Form.Group>
                            <Form.Label>Recipient Name 1 (for CC)</Form.Label>
                            <Form.Control id="ccName1" type="" placeholder="" value={item.ccName1 == null ? '' : item.ccName1} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-sm-6 col-lg-4">
                        <Form.Group>
                            <Form.Label>Email Address 1</Form.Label>
                            <Form.Control id="ccEmail1" type="" placeholder="" value={item.ccEmail1 == null ? '' : item.ccEmail1} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-sm-6 col-lg-4">
                        <Form.Group>
                            <Form.Label>Recipient Name 2 (for CC)</Form.Label>
                            <Form.Control id="ccName2" type="" placeholder="" value={item.ccName2 == null ? '' : item.ccName2} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-sm-6 col-lg-4">
                        <Form.Group>
                            <Form.Label>Email Address 2</Form.Label>
                            <Form.Control id="ccEmail2" type="" placeholder="" value={item.ccEmail2 == null ? '' : item.ccEmail2} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Abstract</Form.Label>
                            {!_isValid.abstract &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <WysiwygEditor id="abstract" value={item.abstract} onChange={onChange} onValidate={validateForm_abstract} className={(!_isValid.abstract ? 'short invalid-field' : 'short')} />
                        </Form.Group>
                    </div>
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
                </div>
                <div className="row mt-2">
                    <div className="col-md-4">
                        {renderPublisher()}
                    </div>
                    <div className="col-md-4">
                    </div>
                    <div className="col-md-4">
                    </div>
                </div>
                <div className="row mt-2 pt-2 border-top">
                    <div className="col-12">
                        <h3 className="mb-4">Image Selection</h3>
                    </div>
                    <div className="col-md-6">
                        {renderImageSelection("imagePortrait", "Portrait Image", "Recommended aspect ratio 3:4. Used by: Home page ('Featured Solution' banner image, 'Popular' items tiles), Library page (result tiles)")}
                    </div>
                    {/*<div className="col-md-6">*/}
                    {/*    {renderImageSelection("imageSquare", "Square Image (Deprecated)", "Recommended aspect ratio 1:1. Deprecated.")}*/}
                    {/*</div>*/}
                    <div className="col-md-6">
                        {renderImageSelection("imageLandscape", "Landscape Image", "Recommended: 320px w by 180px h (16:9) Used by: Home page ('New' items tiles), Marketplace item page (banner image, 'Related' items tiles)")}
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-12 border-top pt-2">
                        <AdminImageList caption="Uploaded Images" items={_imageRows.filter(x => x.marketplaceItemId === item.id)} onImageUpload={onImageUpload} onDeleteItem={onDeleteImage} marketplaceItemId={item.id} />
                    </div>
                </div>
            </>
        )
    }

    const renderMultiSelectAreas = () => {
        if (item == null) return;
        return (
            <>
                <MultiSelect items={item.industryVerticals} caption="Industry Verticals" onItemSelect={onItemSelectIndustryVertical} className="light" />
                <MultiSelect items={item.categories} caption="Processes" onItemSelect={onItemSelectCategory} className="light" />

                <div className="info-panel light">
                    <div className="info-section mb-4 px-1 rounded">
                        <div className="headline-3 mb-2">
                            <span className="pr-2 w-100 d-block rounded">
                            Meta tags (optional)</span></div>
                        <Form.Group>
                            <Form.Control id="metaTagsConcatenated" as="textarea" style={{ height: '300px' }} placeholder="Enter tags seperated by a comma" value={item.metaTagsConcatenated} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
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
            <a className="px-2 btn btn-text-solo align-items-center auto-width ml-auto justify-content-end d-flex" href={`/library/${item.name}`} ><i className="material-icons">visibility</i>View</a>
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

export default AdminMarketplaceEntity;
