import React, { useState, useEffect, useRef } from 'react'
import axiosInstance from "../../../services/AxiosService";

import { useLoadingContext } from '../../../components/contexts/LoadingContext';
import AdminImageRow from './AdminImageRow';
import { ImageUploader } from '../../../components/ImageUploader';
import { generateLogMessageString } from '../../../utils/UtilityService';
import ConfirmationModal from '../../../components/ConfirmationModal';
import color from '../../../components/Constants';

const CLASS_NAME = "AdminImageList";

function AdminImageList(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _scrollToRef = useRef(null);
    const { setLoadingProps } = useLoadingContext();
    const [_dataRows, setDataRows] = useState({all: [], itemCount: 0});
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onImageUpload = (ids) => {
        console.log(generateLogMessageString(`onImageUpload||# Images: ${ids.length}`, CLASS_NAME));
        if (props.onImageUpload) props.onImageUpload(ids);
    }

    const onImageReplace = (ids) => {
        console.log(generateLogMessageString(`onImageReplace||# Images: ${ids.length}`, CLASS_NAME));
        if (props.onImageUpload) props.onImageUpload(ids);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onDeleteItem = (img) => {
        console.log(generateLogMessageString('onDeleteItem', CLASS_NAME));
        setDeleteModal({ show: true, item: img });
    };

    const onDeleteConfirm = () => {
        console.log(generateLogMessageString('onDeleteConfirm', CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        var data = { id: _deleteModal.item.id };
        var url = `image/delete`;
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
                    });
                    if (props.onDeleteItem) props.onDeleteItem(_deleteModal.item);
                    setDeleteModal({ show: false, item: null });
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Delete Item Error', message: `An error occurred deleting this item: ${result.data.message}` });
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
                setDeleteModal({ show: false, item: null });
            });
    };

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {

        setDataRows({
            ..._dataRows,
            all: props.items, itemCount: props.items?.length
        });

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.items]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderNoDataRow = () => {
        return (
            <div className="alert alert-info-custom mt-2 mb-2">
                <div className="text-center" >There are no images for this item.</div>
            </div>
        );
    }

    const renderItemsGridHeader = () => {
        if (_dataRows.all == null || _dataRows.all.length === 0) return null;

        return (
            <thead>
                <AdminImageRow key="header" item={null} isHeader={true} cssClass="admin-item-row" />
            </thead>
        )
    }

    //render the main grid
    const renderItemsGrid = () => {
        if (_dataRows.all == null || _dataRows.all.length === 0) {
            return (
                renderNoDataRow()
            )
        }

        const mainBody = _dataRows.all.map((item) => {
            return (
                <AdminImageRow key={item.id} item={item} cssClass="admin-item-row" onDeleteItem={onDeleteItem} onImageReplace={onImageReplace}
                    canDelete={props.marketplaceItemId == null || props.marketplaceItemId === item.marketplaceItemId} />
            );
        });

        return (
            <tbody>
                {mainBody}
            </tbody>
        )
    }

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

    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderDeleteConfirmation = () => {

        if (!_deleteModal.show) return;

        var message = `You are about to delete '${_deleteModal.item.fileName}'. This action cannot be undone. Are you sure?`;
        var caption = `Delete Image`;

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

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <div className="row mt-2 py-2" >
                <div className="col-sm-12 d-flex align-items-center" >
                    {props.caption != null &&
                        <h2 className="mb-0">{props.caption}</h2>
                    }
                    <ImageUploader cssClass="btn-secondary" uploadToServer={true} onImageUpload={onImageUpload} marketplaceItemId={props.marketplaceItemId} />
                </div>
            </div>

            <div className="row" >
                <div ref={_scrollToRef} className="col-sm-12 mb-4" >
                    <table className="flex-grid w-100" >
                    {renderItemsGridHeader()}
                    {renderItemsGrid()}
                    </table>
                </div>
            </div>
            {renderDeleteConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default AdminImageList;