import React, { useState, useEffect } from 'react'
import axiosInstance from "../services/AxiosService";

import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";

import ConfirmationModal from './ConfirmationModal';
import color from './Constants';

const CLASS_NAME = "OnDeleteConfirm";

function OnDeleteConfirm(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { setLoadingProps } = useLoadingContext();
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: useEffect - Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        if (props.item != null) {
            setDeleteModal({ show: true, item: props.item });
        }
    }, [props.item]);

    //-------------------------------------------------------------------
    // Region: Event Handling - delete item
    //-------------------------------------------------------------------
    const onDeleteConfirm = () => {
        console.log(generateLogMessageString('onDeleteConfirm', CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        //in some cases, we have to pass externalSource instead of id
        let data = _deleteModal.item.externalSource ? _deleteModal.item.externalSource : { id: _deleteModal.item.id };
        var url = props.urlDelete;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: props.successMessage, isTimed: true
                            }
                        ],
                    });

                    setDeleteModal({ show: false, item: null });
                    //callback
                    if (props.onDeleteComplete) props.onDeleteComplete(result.data.isSuccess, _deleteModal.item);
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: `${props.caption} - Error`, message: `${props.errorMessage}: ${result.data.message}` });
                    setLoadingProps({ isLoading: false, message: null });
                    setDeleteModal({ show: false, item: null });
                    //callback
                    if (props.onDeleteComplete) props.onDeleteComplete(false);
                }
            })
            .catch(error => {
                //hide a spinner, show a message
                setError({ show: true, caption: `${props.caption} - Error`, message: `${props.errorMessage}.` });
                setLoadingProps({ isLoading: false, message: null });

                console.log(generateLogMessageString('deleteItem||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
                setDeleteModal({ show: false, item: null });
                //callback
                if (props.onDeleteComplete) props.onDeleteComplete(false);
            });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    if (!_deleteModal.show) return null;

    var message = `${props.confirmMessage} Are you sure?`;
    var caption = props.caption;

    return (
        <ConfirmationModal showModal={_deleteModal.show} caption={caption} message={message}
            icon={{ name: "warning", color: color.trinidad }}
            confirm={{ caption: "Ok", callback: onDeleteConfirm, buttonVariant: "danger" }}
            cancel={{
                caption: "Cancel",
                callback: () => {
                    console.log(generateLogMessageString(`onDeleteCancel`, CLASS_NAME));
                    setDeleteModal({ show: false, item: null });
                    if (props.onDeleteComplete) props.onDeleteComplete(false, null);
                },
                buttonVariant: null
            }}
        />
    );

}

export default OnDeleteConfirm;