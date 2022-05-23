import { useState } from 'react';
import axiosInstance from '../services/AxiosService';

import { useLoadingContext } from './contexts/LoadingContext';
import { generateLogMessageString } from '../utils/UtilityService';

const CLASS_NAME = "ImageUploader";

function ImageUploader(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { setLoadingProps } = useLoadingContext();
    const [_fileSelection, setFileSelection] = useState('');

    //-------------------------------------------------------------------
    // Region: Event handlers
    //-------------------------------------------------------------------
    const onUploadClick = (e) => {
        console.log(generateLogMessageString(`onImportClick`, CLASS_NAME));

        //if (loadingProps.isImporting) return;

        //console.log(generateLogMessageString(`onProfileLibraryFileChange`, CLASS_NAME));

        let files = e.target.files;
        let readers = [];
        if (!files.length) return;

        for (let i = 0; i < files.length; i++) {
            readers.push(readImageData(files[i]));
        }

        Promise.all(readers).then((values) => {
            //either upload locally and save with a larger save operation. 
            //or call API immediately and save as you upload.
            if (props.uploadToServer) {
                uploadImagesToApi(values);
            }
            else {
                if (props.onImageUpload) props.onImageUpload(values);
            }
        });
    };

    //-------------------------------------------------------------------
    // Save image as base64 string
    const uploadImagesToApi = async (items) => {

        var url = props.imageId == null ? `image/add` : `image/update`;
        //url = `profile/import/slow`; //testing purposes
        console.log(generateLogMessageString(`uploadImage(s)||${url}`, CLASS_NAME));

        var msgFiles = "";
        items.forEach(function (f) {
            //msgFiles += msgFiles === "" ? `<br/>File(s) being imported: ${f.fileName}` : `<br/>${f.fileName}`;
            msgFiles += msgFiles === "" ? `Image(s) being uploaded: ${f.fileName}` : `<br/>${f.fileName}`;
        });

        //show a processing message at top. One to stay for duration, one to show for timed period.
        //var msgImportProcessingId = new Date().getTime();
        setLoadingProps({
            isLoading: true, message: `Uploading images...This may take a few minutes.`
        });

        await axiosInstance.post(url, props.imageId == null ? items : items[0]).then(result => {
            if (result.status === 200) {
                //check for success message OR check if some validation failed
                //remove processing message, show a result message
                //inline for isSuccess, pop-up for error
                if (result.data.isSuccess) {
                    //inform parent component of the new images. 
                    if (props.onImageUpload) props.onImageUpload(result.data.data);
                    setLoadingProps({ isLoading: false });
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: result.data?.message, isTimed: false }]
                    });
                }
            } else {
                //hide a spinner, show a message
                setLoadingProps({isLoading: false });
                //setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s)` });
            }
        }).catch(e => {
            if (e.response && e.response.status === 401) {
                setLoadingProps({ isLoading: false, message: null, isImporting: false });
            }
            else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //,inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: e.response.data ? e.response.data : `An error occurred saving the imported profile.`, isTimed: false, isImporting: false }]
                });
                //setError({ show: true, caption: 'Import Error', message: e.response && e.response.data ? e.response.data : `A system error has occurred during the profile import. Please contact your system administrator.` });
                console.log(generateLogMessageString('handleOnSave||saveFile||' + JSON.stringify(e), CLASS_NAME, 'error'));
                console.log(e);
            }
        });
    }

    const readImageData = (file) => {
        return new Promise(function (resolve, reject) {
            let fr = new FileReader();

            fr.onload = function () {
                //transfer data into fields we maintain in our model
                //image id is null for new and exists for replace scenario
                resolve({ id: props.imageId, fileName: file.name, type: file.type, src: fr.result, marketplaceItemId: props.marketplaceItemId });
            };

            fr.onerror = function () {
                reject(fr);
            };

            fr.readAsDataURL(file);
        });
    }

    //this will always force the file selector to trigger event after selection.
    //it wasn't firing if selection was same between 2 instances
    const resetFileSelection = () => {
        console.log(generateLogMessageString(`resetFileSelection`, CLASS_NAME));
        setFileSelection('');
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    var buttonCss = `btn auto-width ${props.cssClass} ${props.disabled ? "disabled" : ""}`;
    var caption = props.caption == null ? "Upload Image" : props.caption;

    return (
        <div className="ml-auto">
            <label className={buttonCss} >
                {caption}
                {props.imageId == null ?
                    <input type="file" multiple value={_fileSelection} onClick={resetFileSelection} disabled={props.disabled ? "disabled" : ""} onChange={onUploadClick} style={{ display: "none" }}
                        accept="image/*" />
                    :
                    <input type="file" value={_fileSelection} onClick={resetFileSelection} disabled={props.disabled ? "disabled" : ""} onChange={onUploadClick} style={{ display: "none" }}
                        accept="image/*" />
                }
            </label>
        </div>
    )
}


export { ImageUploader };
