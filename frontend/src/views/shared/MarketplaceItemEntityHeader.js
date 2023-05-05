import React, { useState } from 'react'
import { Button } from 'react-bootstrap';

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import DownloadNodesetModal from '../../components/DownloadNodesetModal';
import { AppSettings } from '../../utils/appsettings';
import { formatDate, generateLogMessageString, getImageUrl } from '../../utils/UtilityService';
import { MarketplaceItemJobLauncher } from './MarketplaceItemJobLauncher';

const CLASS_NAME = "MarketplaceItemEntityHeader";

function MarketplaceItemEntityHeader(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //used in popup profile add/edit ui. Default to new version
    const { loadingProps } = useLoadingContext();
    const [_downloadModalShow, setDownloadModal] = useState(false);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onDownloadStart = (e) => {
        console.log(generateLogMessageString(`onAdd`, CLASS_NAME));
        e.preventDefault();

        //if user already downloaded any nodeset in past, bypass email collection form
        if (loadingProps.downloadNodesetUid) {
            const itm = JSON.parse(JSON.stringify(AppSettings.requestInfoNew));
            itm.smProfileId = props.item.id;
            itm.smProfile = props.item;
            itm.requestTypeCode = "smprofile-download";
            itm.email = "REPEAT";
            itm.uid = loadingProps.downloadNodesetUid;
            if (props.onDownload) props.onDownload(itm);
        }
        else {
            setDownloadModal(true);
        }
    };

    const onDownload = (itm) => {
        if (props.onDownload) props.onDownload(itm);
        setDownloadModal(false);
    }

    const onDownloadCancel = () => {
        console.log(generateLogMessageString(`onDownloadCancel`, CLASS_NAME));
        setDownloadModal(false);
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderImageBg = () => {

        const bgImageStyle = props.item.imageLandscape == null ? {} :
            {
                backgroundImage: `url(${getImageUrl(props.item.imageLandscape)})`
        };

        return (
            <div className="image-bg" >
                <div className="overlay-icon contain" style={bgImageStyle} >&nbsp;</div>
            </div>
        );
    };

    const renderMarketplaceHeader = () => {
        return (
            <>
                <div className={`row mx-0 p-0 ${props.cssClass} mb-4`}>
                    <div className="col-sm-6 col-md-5 p-0 d-none d-sm-block" >
                        {renderImageBg()}
                    </div>
                    <div className="col-sm-6 col-md-7 p-4" >
                        {/*<h2>{props.item.name}</h2>*/}
                        {props.item.abstract != null &&
                            <div className="mb-2" dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                        }
                        <p className="mb-0" ><b className="mr-2" >Published:</b>{formatDate(props.item.publishDate)}</p>
                        {/*<div className="d-none d-lg-inline" >{renderIndustryVerticalItem(props.item)}</div>*/}
                        {/*<div className="d-none d-lg-inline" >{renderCategoryItem(props.item)}</div>*/}
                        {/*<div className="d-none d-lg-inline" >{renderMetaTagItem(props.item)}</div>*/}
                        {renderJobDefinitions()}
                    </div>
                </div>
            </>
        );
    };

    const renderProfileHeader = () => {
        return (
            <>
                <div className={`row mx-0 p-0 ${props.cssClass} mb-4`}>
                    <div className="col-sm-6 col-md-5 p-0 d-none d-sm-block" >
                        {renderImageBg()}
                    </div>
                    <div className="col-sm-6 col-md-7 p-4" >
                        {props.item.abstract != null &&
                            <div className="mb-2" dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                        }
                        {(props.item.namespace != null && props.item.namespace !== '') &&
                            <p className="mb-2" ><b className="mr-2" >Namespace:</b>
                                <span style={{ wordBreak: "break-word" }} >{props.item.namespace}</span>
                            </p>
                        }
                        <p className="mb-2" ><b className="mr-2" >Published:</b>{formatDate(props.item.publishDate)}</p>
                        <p className="mb-2" ><b className="mr-2" >Version:</b>{props.item.version}</p>
                        {props.onDownload &&
                            <p className="my-4" ><Button variant="secondary" type="button" className="px-4" onClick={onDownloadStart} >Download Nodeset XML</Button></p>
                        }
                        {props.showProfileDesignerLink &&
                            <p className="mb-2" ><Button variant="secondary" type="link" target="_blank" className="px-4" href={`${AppSettings.ProfileDesignerUrl}cloudlibrary/viewer/${props.item.id}`} >
                                View in SM Profile Designer</Button></p>
                        }
                    </div>
                </div>
            </>
        );
    };

    //renderDownloadModal as a modal to force user to say ok.
    const renderDownloadModal = () => {

        if (!_downloadModalShow) return;

        return (
            <DownloadNodesetModal item={props.item} showModal={_downloadModalShow} onDownload={onDownload} onCancel={onDownloadCancel} showSavedMessage={true} />
        );
    };


    const renderJobDefinitions = () => {

        if (!props.isAuthenticated || props.item.jobDefinitions == null || props.item.jobDefinitions.length === 0) return;

        return props.item.jobDefinitions.map((x) => {
            return (
                <MarketplaceItemJobLauncher key={x.id} className={`mr-2 mt-2`} isAuthenticated={props.isAuthenticated} jobDefinitionId={x.id} marketplaceItemId={props.item.id} jobName={x.name} payload={x.payload} />
            );
        });
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (props.item === null || props.item === {}) return null;
    if (props.item.name == null) return null;

    if (props.item.type?.code === AppSettings.itemTypeCode.smProfile) {
        return (
            <>
                {renderProfileHeader()}
                {renderDownloadModal()}
            </>
        )
    }
    else {
        return (
            renderMarketplaceHeader()
        )
    }

}

export default MarketplaceItemEntityHeader;