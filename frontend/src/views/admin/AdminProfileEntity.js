import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'
import Card from 'react-bootstrap/Card'
import Tab from 'react-bootstrap/Tab'
import Nav from 'react-bootstrap/Nav'

import axiosInstance from "../../services/AxiosService";

import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString, prepDateVal } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import { SVGIcon } from "../../components/SVGIcon";
import color from "../../components/Constants";
import MultiSelect from '../../components/MultiSelect';
import ConfirmationModal from '../../components/ConfirmationModal';
//import { WysiwygEditor } from '../../components/WysiwygEditor';
//import AdminImageList from './shared/AdminImageList';
import AdminRelatedItemList from './shared/AdminRelatedItemList';
import { clearSearchCriteria } from '../../services/MarketplaceService';

import '../../components/styles/TabContainer.scss';

const CLASS_NAME = "AdminProfileEntity";

function AdminProfileEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const { id } = useParams();
    //var pageMode = //state is not always present. If user types a url or we use an href link, state is null. history.location.state.viewMode;
    //see logic below for how we calculate.
    const [mode, setMode] = useState(initPageMode());
    const [item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const [isReadOnly, setIsReadOnly] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_isValid, setIsValid] = useState({
        profileId: true, relatedItems: true, relatedProfiles: true, relatedItemsMinCount: true
    });
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const [_refreshItem, setRefreshItem] = useState(null);  //trigger a retrieval of data on select of profile id - new mode.

    const [_itemsLookup, setItemsLookup] = useState([]);  //profile items 
    const [_loadLookupData, setLoadLookupData] = useState(null);

    //-------------------------------------------------------------------
    // Region: fetch calls
    //-------------------------------------------------------------------
    async function fetchData(val) {
        console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
        //initialize spinner during loading
        setLoadingProps({ isLoading: true, message: null });

        var result = null;
        try {
            var data = { id: val };
            var url = `admin/profile/getbyid`
            result = await axiosInstance.post(url, data);
        }
        catch (err) {
            var msg = 'An error occurred retrieving this profile item.';
            console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
            //console.log(err.response.status);
            if (err != null && err.response != null && err.response.status === 404) {
                msg += ' This profile item was not found.';
                history.push('/404');
            }
            //403 error - user may be allowed to log in but not permitted to perform the API call they are attempting
            else if (err != null && err.response != null && err.response.status === 403) {
                console.log(generateLogMessageString('useEffect||fetchData||Permissions error - 403', CLASS_NAME, 'error'));
                msg += ' You are not permitted to edit profile items.';
                history.goBack();
            }
            setLoadingProps({
                isLoading: false, message: null, inlineMessages: [
                    { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
            });
        }

        if (result == null) return;

        var thisMode = 'edit';

        //convert collection to comma separated list
        //special handling of meta tags which shows as a concatenated list in an input box
        result.data.metaTagsConcatenated = result.data == null || result.data.metaTags == null ? "" : result.data.metaTags.join(', ');
        //set item state value
        setItem(result.data);
        setIsLoading(false);
        setLoadingProps({ isLoading: false, message: null });
        setMode(thisMode);

        //profiles will have limited editing capability. They are maintained in the Cloud Lib. 
        setIsReadOnly(true);

    }

    //get a blank profile item object from server
    async function fetchDataAdd() {
        console.log(generateLogMessageString('useEffect||fetchDataAdd||async', CLASS_NAME));
        //initialize spinner during loading
        setLoadingProps({ isLoading: true, message: null });

        var result = null;
        try {
            var url = `admin/profile/init`
            result = await axiosInstance.post(url);
        }
        catch (err) {
            var msg = 'An error occurred retrieving the blank profile item.';
            console.log(generateLogMessageString('useEffect||fetchDataAdd||error', CLASS_NAME, 'error'));
            //console.log(err.response.status);
            if (err != null && err.response != null && err.response.status === 404) {
                msg += ' A problem occurred with the add profile item screen.';
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

        //profiles will have limited editing capability. They are maintained in the Cloud Lib.
        setIsReadOnly(true);
    }

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        //fetch our data 
        // for view/edit modes
        if ((id != null && id.toString() !== 'new')) {
            fetchData(id);
        }
        else {
            fetchDataAdd();
        }

        //this will execute on unmount
        return () => {
        };
    }, [id]);

    useEffect(() => {
        //fetch our data 
        //for new mode when user selects a profile from ddl
        if (_refreshItem === true && item.id != null) {
            setRefreshItem(false);
            fetchData(item.id);
        }

        //this will execute on unmount
        return () => {
        };
    }, [_refreshItem]);

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - get static lookup data
    //-------------------------------------------------------------------
    /*
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchImageData() {

            var url = `image/all`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.post(url, { id: id }).then(result => {
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
    */

    //-------------------------------------------------------------------
    // Trigger get lookup data from server (if necessary)
    //-------------------------------------------------------------------
    /*
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
    */

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
    const validateForm_profileId = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, profileId: isValid });
    };


    /*
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
    */

    const validateForm_relatedItems = () => {
        return item.relatedItems == null ||
            item.relatedItems.filter(x => x.relatedId === "-1" || x.relatedType?.id === "-1").length === 0;
    };

    const validateForm_relatedProfiles = () => {
        return item.relatedProfiles == null ||
            item.relatedProfiles.filter(x => x.relatedId === "-1" || x.relatedType?.id === "-1").length === 0;
    };

    //must have at least one item to save (for now)
    const validateForm_relatedItemsMinCount = () => {
        const countRelatedItems = item.relatedItems == null ? 0 : item.relatedItems.length;
        const countRelatedProfiles = item.relatedProfiles == null ? 0 : item.relatedProfiles.length;
        return countRelatedItems + countRelatedProfiles > 0;
    };
    
    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        /*
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
        */
        _isValid.profileId = item.id != null && item.id.toString() !== "-1";
        _isValid.relatedItems = validateForm_relatedItems();
        _isValid.relatedProfiles = validateForm_relatedProfiles();
        _isValid.relatedItemsMinCount = validateForm_relatedItemsMinCount();

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.profileId && _isValid.relatedItems && _isValid.relatedProfiles && _isValid.relatedItemsMinCount );
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onRemoveRelationships = () => {
        console.log(generateLogMessageString('onRemoveRelationships', CLASS_NAME));
        setDeleteModal({ show: true, item: item });
    };

    const onRemoveRelationshipsConfirm = () => {
        console.log(generateLogMessageString('onRemoveRelationshipsConfirm', CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform call - this will only remove relationships in the marketplace db. 
        //this does not impact the Cloud Lib in any way.
        var data = { id: item.id };
        var url = `admin/profile/delete`;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: `Related items and related profiles were removed`, isTimed: true
                            }
                        ],
                        refreshLookupData: true
                    });
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Remove Related Error', message: `An error occurred removing relationships: ${result.data.message}` });
                    setLoadingProps({ isLoading: false, message: null });
                }
                history.push('/admin/profile/list');
            })
            .catch(error => {
                //hide a spinner, show a message
                setError({ show: true, caption: 'Remove Related Error', message: `An error occurred removing relationships.` });
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
        var url = `admin/profile/upsert`;
        axiosInstance.post(url, item)
            .then(resp => {
                if (resp.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "success", body: `Profile item was saved`, isTimed: true }
                        ]
                    });

                    //now redirect to profile item on front end
                    history.push(`/admin/profile/${resp.data.data}`);
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
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred ${mode.toLowerCase() === "copy" ? "copying" : "saving"} this profile item.`, isTimed: false }
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

    const onChangeProfileId = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "id":
                if (e.target.value.toString() === "-1") {
                    item[e.target.id] = null;
                    fetchDataAdd();
                }
                else {
                    item[e.target.id] = e.target.value;
                    item.displayName = e.target.options[e.target.selectedIndex].text;
                    fetchData(e.target.value);
                }
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(item)));
        validateForm_profileId(e);
    }
    /*
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
    */

    /*
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
    */

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
    /*
    const onImageUpload = (imgs) => {
        //trigger api call to get latest. 
        setRefreshImageData(true);
    }

    const onDeleteImage = (id) => {
        //trigger api call to get latest. 
        setRefreshImageData(true);
    }
    */

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
        setIsValid({ ..._isValid, relatedItemsMinCount: validateForm_relatedItemsMinCount })
    }

    const onChangeRelatedProfile = (currentId, arg) => {
        console.log(generateLogMessageString('onChangeRelatedProfile', CLASS_NAME));
        var match = item.relatedProfiles.find(x => x.relatedId === currentId);
        match.relatedId = arg.relatedId;
        match.displayName = arg.displayName;
        match.relatedType = arg.relatedType;
        setItem(JSON.parse(JSON.stringify(item)));
        setIsValid({ ..._isValid, relatedItemsMinCount: validateForm_relatedItemsMinCount })
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

    const onDeleteRelatedItem = (id) => {
        console.log(generateLogMessageString('onDeleteRelatedItem', CLASS_NAME));
        //make a copy of the array 
        item.relatedItems = item.relatedItems.filter(x => x.relatedId !== id);
        //item.relatedItems = item.relatedItems.map(x => { return (x.relatedId !== id ? x : null); }).filter(x => x != null);
        setItem(JSON.parse(JSON.stringify(item)));
    }

    const onDeleteRelatedProfile = (id) => {
        console.log(generateLogMessageString('onDeleteRelatedProfile', CLASS_NAME));
        item.relatedProfiles = item.relatedProfiles.filter(x => x.relatedId !== id);
        setItem(JSON.parse(JSON.stringify(item)));
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderItemType = () => {
        return (
            <Form.Group>
                <Form.Label>Type</Form.Label>
                <Form.Control id="type" type="" value={item.type != null ? item.type.name : ""} readOnly={isReadOnly} />
            </Form.Group>
        )
    };

    const renderPublisher = () => {
        return (
            <Form.Group>
                <Form.Label>Publisher</Form.Label>
                <Form.Control id="publisher" value={item.publisher != null ? item.publisher.displayName : ""} readOnly={isReadOnly} />
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
                    <Dropdown.Item onClick={onRemoveRelationships} >Remove All Related Items & Profiles</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
        );
    }

    const renderProfileSelect = () => {

        if (id !== 'new') return;

        //until lookup data arrives, show label
        if (_itemsLookup == null || _itemsLookup?.lookupProfiles == null ) return;

        //show drop down list
        const options = _itemsLookup?.lookupProfiles.map((itm) => {
            const version = itm.version == null ? '' : ' v.' + itm.version; 
            const displayName = `${itm.displayName}` +
                `${(itm.namespace != null && itm.namespace !== '') ? ' (' + itm.namespace + version + ')' : ''}`;
            return (<option key={itm.id} value={itm.id} >{displayName}</option>)
        });

        return (
            <Form.Group className="mb-0">
                <Form.Control id="id" as="select" value={item.id == null ? "-1" : item.id}
                    className={`minimal pr-5 ${!_isValid.profileId ? 'invalid-field' : ''}`}
                    onChange={onChangeProfileId} onBlur={validateForm_profileId} >
                    <option key="-1|Select One" value="-1" >--Select One--</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers - Immage selection
    //-------------------------------------------------------------------
    /*
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
    */

    const renderButtons = () => {
        if (mode.toLowerCase() !== "view") {
            return (
                <>
                    <Button variant="text-solo" className="ml-1" href="/admin/profile/list" >Cancel</Button>
                    <Button variant="secondary" type="button" className="ml-2" onClick={onSave} >Save</Button>
                    {id !== "new" &&
                        renderMoreDropDown()
                    }
                </>
            );
        }
    }

    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderDeleteConfirmation = () => {

        if (!_deleteModal.show) return;

        var message = `You are about to remove all related items and related profiles from '${_deleteModal.item.displayName}'. This action cannot be undone. Are you sure?`;
        var caption = `Remove Related`;

        return (
            <>
                <ConfirmationModal showModal={_deleteModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Proceed", callback: onRemoveRelationshipsConfirm, buttonVariant: "danger" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onRemoveRelationshipsCancel`, CLASS_NAME));
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

    const renderValidationSummary = () => {

        let summary = [];
        if (!_isValid.profileId) summary.push('Profile is required.');
        if (!validateForm_relatedItems()) summary.push('Related Items - Select item and set related type.');
        if (!validateForm_relatedProfiles()) summary.push('Related Profiles - Select item and set related type.');
        if (!_isValid.relatedItemsMinCount) summary.push('Related Items - At least one related item or one related profile is required.');
        if (summary.length == 0) return null;

        let content = summary.map(function (x, i) {
            return i > 0 ? (<span key={i} ><br />{x}</span>) : (<span key={i} >{x}</span>);
        });

        return (
            <div className="alert alert-danger w-100">
                {content}
            </div>
        );
    };

    const renderRelatedItems = () => {
        return (
            <>
                {!_isValid.relatedItemsMinCount &&
                    <p class="mb-1 text-danger">At least one related item or one related profile is required.</p>
                }
                <div className="row mt-2">
                    <div className="col-12">
                        <AdminRelatedItemList caption="Related Marketplace Items" captionAdd="Add Related Marketplace Item"
                            items={item.relatedItems} itemsLookup={_itemsLookup?.lookupItems?.filter(x => x.id !== item.id)}
                            type={AppSettings.itemTypeCode.smApp} onChangeItem={onChangeRelatedItem}
                            onAdd={onAddRelatedItem} onDelete={onDeleteRelatedItem} />
                    </div>
                </div>
                <div className="row">
                    <div className="col-12">
                        <hr className="my-3" />
                        <AdminRelatedItemList caption="Related SM Profiles" captionAdd="Add Related SM Profile"
                            items={item.relatedProfiles} itemsLookup={_itemsLookup?.lookupProfiles}
                            type={AppSettings.itemTypeCode.smProfile} onChangeItem={onChangeRelatedProfile}
                            onAdd={onAddRelatedProfile} onDelete={onDeleteRelatedProfile} />
                    </div>
                </div>
            </>
        );
    };

    const renderCommonInfo = () => {
        return (
            <>
                {id === 'new' &&
                    <div className="row">
                        <div className="col-md-9">
                            <Form.Group>
                                <Form.Label>Select Profile*</Form.Label>
                                {!_isValid.profileId &&
                                    <span className="invalid-field-message inline">
                                        Required
                                    </span>
                                }
                                {renderProfileSelect()}
                            </Form.Group>
                        </div>
                    </div>
                }
                <div className="row">
                    <div className="col-md-9">
                        <Form.Group>
                            <Form.Label>Namespace</Form.Label>
                            <Form.Control id="namespace" className="minimal pr-5" value={item.namespace == null ? '' : item.namespace} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-3">
                        {(item.id != null && item.id !== "-1") && 
                            <Form.Group>
                                <Form.Label>Cloud Lib Id</Form.Label>
                                <Form.Control id="id" className="minimal pr-5" value={item.id} readOnly={isReadOnly} />
                            </Form.Group>
                        }
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-9">
                        <Form.Group>
                            <Form.Label>Display Name</Form.Label>
                            <Form.Control id="displayName" className="minimal pr-5" value={item.displayName == null ? '' : item.displayName} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-3">
                        {renderItemType()}
                    </div>
                </div>
            </>
        );
    };

    const renderGeneralTab = () => {
        //console.log(item);
        return (
            <>
                <div className="row mt-2">
                    <div className="col-sm-6 col-lg-4">
                        <Form.Group>
                            <Form.Label>Version</Form.Label>
                            <Form.Control id="version" value={item.version == null ? '' : item.version} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-sm-6 col-lg-4">
                        <Form.Group>
                            <Form.Label>Publish Date</Form.Label>
                            <Form.Control id="publishDate" mindate="2010-01-01" type={`${item.id == null ? '' : 'date'}`} value={item.id == null ? '' : prepDateVal(item.publishDate)} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Abstract</Form.Label>
                            <div className="border p-2 px-3 rounded form-control h-auto" readOnly={true} dangerouslySetInnerHTML={{ __html: item.abstract == null ? '<br/>' : item.abstract }}></div>
                        </Form.Group>
                    </div>
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Description</Form.Label>
                            <div className="border p-2 px-3 rounded form-control h-auto" readOnly={true} dangerouslySetInnerHTML={{ __html: item.description == null ? '<br/>' : item.description }}></div>
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
            </>
        );
    }

    /*
    const renderImagesInfo = () => {
        return (
            <>
                <div className="row mt-2">
                    <div className="col-12">
                        <h3 className="mb-4">Image Selection</h3>
                    </div>
                    <div className="col-md-6">
                        {renderImageSelection("imagePortrait", "Portrait Image", "Recommended aspect ratio 3:4. Used by: Home page ('Featured Solution' banner image, 'Popular' items tiles), Library page (result tiles)")}
                    </div>
                    <div className="col-md-6">
                        {renderImageSelection("imageLandscape", "Landscape Image", "Recommended: 320px w by 180px h (16:9) Used by: Home page ('New' items tiles), profile item page (banner image, 'Related' items tiles)")}
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-12 border-top pt-2">
                        <AdminImageList caption="Uploaded Images" items={_imageRows.filter(x => x.marketplaceItemId === item.id)} onImageUpload={onImageUpload} onDeleteItem={onDeleteImage} marketplaceItemId={item.id} />
                    </div>
                </div>
            </>
        );
    };
    */

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
                            Keywords</span></div>
                        <Form.Group>
                            <Form.Control id="metaTagsConcatenated" as="textarea" style={{ height: '100px' }} value={item.metaTagsConcatenated} readOnly={isReadOnly} />
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
                    Profile item
                </h1>
                <div className="ml-auto d-flex align-items-center" >
                    {renderButtons()}
                </div>
            </>
        )
    }

    const tabListener = (eventKey) => {
    }

    const renderTabbedForm = () => {
        return (
            <Tab.Container id="admin-marketplace-entity" defaultActiveKey="relatedItems" onSelect={tabListener} >
                <Nav variant="pills" className="row mt-1 px-2 pr-md-3">
                    <Nav.Item className="col-sm-4 rounded p-0 pl-2" >
                        <Nav.Link eventKey="general" className="text-center text-md-left p-1 px-2 h-100" >
                            <span className="headline-3">General</span>
                        </Nav.Link>
                    </Nav.Item>
                    <Nav.Item className="col-sm-4 rounded p-0 pr-2">
                        <Nav.Link eventKey="relatedItems" className="text-center text-md-left p-1 px-2 h-100" >
                            <span className="headline-3">Related Items</span>
                            {/*<span className="d-none d-md-inline"><br />Optional and advanced settings</span>*/}
                        </Nav.Link>
                    </Nav.Item>
                </Nav>

                <Tab.Content>
                    <Tab.Pane eventKey="general">
                        <Card className="">
                            <Card.Body className="pt-3">
                                { renderGeneralTab()}
                            </Card.Body>
                        </Card>
                    </Tab.Pane>
                    <Tab.Pane eventKey="relatedItems">
                        <Card className="">
                            <Card.Body className="pt-3">
                                {renderRelatedItems()}
                            </Card.Body>
                        </Card>
                    </Tab.Pane>
                </Tab.Content>
            </Tab.Container>
        );
    };

    const renderSubTitle = () => {
        if (mode === "new" || mode === "copy") return;
        return (
            <a className="px-2 btn btn-text-solo align-items-center auto-width ml-auto justify-content-end d-flex" href={`/profile/${item.name}`} ><i className="material-icons">visibility</i>View</a>
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
                <title>{`${item?.displayName != null ? item?.displayName + ' | ' : '' } Admin | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            <Form noValidate>
            {renderHeaderRow()}
            <div className="row" >
                <div className="col-sm-3" >
                    {renderMultiSelectAreas()}
                </div>
                <div className="col-sm-9 mb-4 tab-container" >
                    {renderValidationSummary()}
                    {renderCommonInfo()}
                    {renderTabbedForm()}
                </div>
            </div>
            </Form>
            {renderDeleteConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default AdminProfileEntity;
