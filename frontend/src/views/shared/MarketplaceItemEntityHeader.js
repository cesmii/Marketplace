import React, { useState } from 'react'
import { Button } from 'react-bootstrap';
import color from '../../components/Constants';

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import DownloadNodesetModal from '../../components/DownloadNodesetModal';
import AddCartButton from '../../components/eCommerce/AddCartButton';
import { RenderImageBg } from '../../services/MarketplaceService';
import { AppSettings } from '../../utils/appsettings';
import { formatItemPublishDate, generateLogMessageString, renderMenuColorIcon, renderMenuColorMaterialIcon } from '../../utils/UtilityService';
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
            itm.externalId = props.item.id;
            itm.externalItem = props.item;
            itm.externalSource = props.item.externalSource;
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

    const onAddCart = (itm, quantity) => {
        console.log(generateLogMessageString(`onAddCart`, CLASS_NAME));
        if (props.onAddCart) props.onAddCart(itm, quantity);
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderMarketplaceHeader = () => {
        return (
            <>
                <div className={`row mx-0 p-0 ${props.cssClass} mb-4 border`}>
                    <div className="col-sm-5 p-0" >
                        <RenderImageBg item={props.item} defaultImage={props.item.imageLandscape} responsiveImage={props.item.imageBanner} clickable={false} />
                    </div>
                    <div className="col-sm-7 p-4" >
                        {/*<h2>{props.item.name}</h2>*/}
                        {props.item.abstract != null &&
                            <div className="mb-2" dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                        }
                        <p className="mb-0" ><b className="mr-2" >Published:</b>{formatItemPublishDate(props.item)}</p>
                        {renderPrices()}
                        <AddCartButton item={props.item} onAdd={onAddCart} className="mt-3 mr-2" />
                        {renderJobDefinitions()}
                        {/*<div className="d-none d-lg-inline" >{renderIndustryVerticalItem(props.item)}</div>*/}
                        {/*<div className="d-none d-lg-inline" >{renderCategoryItem(props.item)}</div>*/}
                        {/*<div className="d-none d-lg-inline" >{renderMetaTagItem(props.item)}</div>*/}
                        {renderActionLinks()}
                        {(props.item.relatedItemsGrouped != null && props.item.relatedItemsGrouped.length > 0) &&
                            <p className="mt-3 mb-0" >
                                <Button variant="link" type="button" className="d-flex align-self-center px-0" onClick={props.onViewSpecifications} >
                                    {renderMenuColorMaterialIcon('visibility', color.cornflower, 'mr-1')}View Specifications</Button>
                            </p>
                        }
                    </div>
                </div>
            </>
        );
    };

    const renderProfileHeader = () => {
        return (
            <>
                <div className={`row mx-0 p-0 ${props.cssClass} mb-4 border`}>
                    <div className="col-sm-5 p-0" >
                        <RenderImageBg item={props.item} defaultImage={props.item.imageLandscape} responsiveImage={props.item.imageBanner} clickable={false} />
                    </div>
                    <div className="col-sm-7 p-4" >
                        {props.item.abstract != null &&
                            <div className="mb-2" dangerouslySetInnerHTML={{ __html: props.item.abstract }} ></div>
                        }
                        {(props.item.namespace != null && props.item.namespace !== '') &&
                            <p className="mb-2" ><b className="mr-2" >Namespace:</b>
                                <span style={{ wordBreak: "break-word" }} >{props.item.namespace}</span>
                            </p>
                        }
                        <p className="mb-2" ><b className="mr-2" >Published:</b>{formatItemPublishDate(props.item)}</p>
                        <p className="mb-2" ><b className="mr-2" >Version:</b>{props.item.version}</p>
                        {props.onDownload &&
                            <p className="mt-3 mb-0" >
                                <Button variant="link" type="button" className="d-flex align-self-center px-0" onClick={onDownloadStart} >
                                {renderMenuColorMaterialIcon('download', color.cornflower, 'mr-1')}Download Nodeset XML</Button>
                            </p>
                        }
                        {props.showProfileDesignerLink &&
                            <p className="mt-3 mb-0" >
                            <Button variant="link" type="button" target="_blank" className="d-flex align-self-center px-0" href={`${AppSettings.ProfileDesignerUrl}cloudlibrary/viewer/${props.item.id}`} >
                                {renderMenuColorIcon('profile', null, color.cornflower, 'mr-1')}View in SM Profile Designer</Button>
                            </p>
                        }
                        {(props.item.relatedItemsGrouped != null && props.item.relatedItemsGrouped.length > 0) &&
                            <p className="mt-3 mb-0" >
                                <Button variant="link" type="button" className="d-flex align-self-center px-0" onClick={props.onViewSpecifications} >
                                {renderMenuColorMaterialIcon('visibility', color.cornflower, 'mr-1')}View Specifications</Button>
                            </p>
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

    const renderActionLinks = () => {

        if (props.item.actionLinks == null || props.item.actionLinks.length === 0) return;

        return props.item.actionLinks.map((x, i) => {
            return (
                <p key={`actionLink-${i}`} className="mt-3 mb-0" >
                    <a className="d-flex align-self-center px-0" href={x.url} target={x.target == null || x.target === '' ? 'self' : x.target} >
                        {renderMenuColorMaterialIcon(x.iconName == null || x.iconName === '' ? 'settings' : x.iconName
                            , color.cornflower, 'mr-1')}{x.caption}</a>
                </p>
            );
        });
    };

    const renderJobDefinitions = () => {

        if (props.item.jobDefinitions == null || props.item.jobDefinitions.length === 0) return;

        return props.item.jobDefinitions.map((x) => {
            if (x.actionType !== AppSettings.JobActionType.ECommerceOnComplete) {
                return (
                    <MarketplaceItemJobLauncher key={x.id} className="mt-3" isAuthenticated={props.isAuthenticated} jobDefinition={x} marketplaceItemId={props.item.id} marketplaceItemName={props.item.name} />
                );
            }
        });
    };

    const renderPrices = () => {

        if (props.item.eCommerce == null || !props.item.eCommerce.allowPurchase ||
            props.item.eCommerce.prices == null || props.item.eCommerce.prices.length == 0) return null;

        return props.item.eCommerce.prices.map((x,i) => {
            return (
                <p key={`price-${i}`} className="mt-2 mb-0" ><b className="mr-2" >{x.caption}:</b>${x.amount}</p>
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