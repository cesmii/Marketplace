import React from 'react'
import { Card } from 'react-bootstrap';

import stockTilePhoto from '../../components/img/icon-molecule-landscape.svg'
import iconMolecule from '../../components/img/icon-molecule-portrait.svg'
import { AppSettings } from '../../utils/appsettings';
import { getImageAlt, getImageUrl } from '../../utils/UtilityService';
import '../styles/MarketplaceTileList.scss';

//const CLASS_NAME = "MarketplaceTileList";

function MarketplaceTileList(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render different layout styles
    //-------------------------------------------------------------------
    const renderDisplayName = (item) =>
    {
        if (item.type?.code === AppSettings.itemTypeCode.smProfile) {
            return (
                <>
                    {item.displayName}
                    {(item.namespace != null && item.namespace !== '') &&
                        <>
                            <br />
                            <span className="font-weight-normal" style={{ wordBreak: "break-word" }} >{item.namespace} (v. {item.version})</span>
                        </>
                    }
                </>
            );
        }

        return (item.displayName);
    };

    //-------------------------------------------------------------------
    // Region: Render different layout styles
    //-------------------------------------------------------------------
    const renderCardImageBanner = (item, isfirst, isLast) => {
        const imgSrc = item.imageLandscape == null ? stockTilePhoto : getImageUrl(item.imageLandscape);

        //some of the css is trying to achieve same height for tiles in a row based on variable content
        return (
            <Card className="h-100 border-0 mb-0 marketplace-tile">
                <Card.Body className="h-100 p-0 tile-body">
                    <img className="card-img-top" src={imgSrc} alt={`${item.name}-${getImageAlt(item.imageLandscape)}`} />
                    <div className="body-content p-4 pb-0" >
                        <span className="card-title font-weight-bold mb-3 d-block bitter">{renderDisplayName(item)}</span>
                        <div className="card-text mb-0" dangerouslySetInnerHTML={{ __html: item.abstract }} ></div>
                    </div>
            </Card.Body>
            </Card>
        );
    }

    const renderCardImageBannerAbbreviated = (item, isfirst, isLast) => {
        const imgSrc = item.imageLandscape == null ? stockTilePhoto : getImageUrl(item.imageLandscape);

        //some of the css is trying to achieve same height for tiles in a row based on variable content
        return (
            <Card className="h-100 border-0 mb-0 marketplace-tile">
                <Card.Body className="h-100 p-0 tile-body">
                    <img className="card-img-top" src={imgSrc} alt={`${item.name}-${getImageAlt(item.imageLandscape)}`} />
                    <div className="body-content p-2 px-4" >
                        <span className="card-title font-weight-bold d-block bitter text-center">{renderDisplayName(item)}</span>
                    </div>
                </Card.Body>
            </Card>
        );
    }


    //portrait image down left side of tile
    const renderCardThumbnail = (item, isfirst, isLast) => {
        return (
            <Card className="h-100 border-0 mb-0 marketplace-tile marketplace-list-item">
                <Card.Body className="h-100 p-0 tile-body d-flex">
                    <div className="col-md-6 col-lg-5 p-0 d-none d-lg-block" >
                        {renderImageBg(item)}
                    </div>
                    <div className="col-md-12 col-lg-7 p-4" >
                        <span className="card-title font-weight-bold mb-3 d-block bitter">{renderDisplayName(item)}</span>
                        <div className="card-text mb-0" dangerouslySetInnerHTML={{ __html: item.abstract }} ></div>
                    </div>
                </Card.Body>
            </Card>
        );
    }

    const renderImageBg = (item) => {
        const imgSrc = item.imagePortrait == null ? iconMolecule : getImageUrl(item.imagePortrait);
        const bgImageStyle = 
            {
                backgroundImage: `url(${imgSrc})`
            };

        return (
            <div className="image-bg" >
                <div className="overlay-icon cover" style={bgImageStyle} >&nbsp;</div>
            </div>
        );
    };

    const renderTiles = () => {
        if (props.items == null || props.items.length === 0) {
            return (
                <div className="flex-grid no-data">
                    {renderNoDataTile()}
                </div>
            )
        }

        const colCountCss = props.colCount == null ? 'col-sm-4' : `col-sm-${(12 / props.colCount).toString()}`;

        const mainBody = props.items.map((itm, counter) => {
            const isFirst = counter === 0;
            const isLast = counter === props.items.length;
            var tile = null;
            if (props.layout === "banner") {
                tile = renderCardImageBanner(itm, isFirst, isLast);
            }
            else if (props.layout === "banner-abbreviated") {
                tile = renderCardImageBannerAbbreviated(itm, isFirst, isLast);
            }
            else {
                tile = renderCardThumbnail(itm, isFirst, isLast);
            }
            const url = itm.type?.code === AppSettings.itemTypeCode.smProfile ? `/profile/${itm.id}` :
                    `/library/${itm.name}`;

            return (
                <div key={itm.id} className={`${colCountCss} pb-4`}>
                    <a href={url} className="tile-link" >
                        {tile}
                    </a>
                </div>
            )
        });

        return (mainBody);

    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderNoDataTile = () => {
        return (
            <div className="alert alert-info-custom mt-2 mb-2">
                <div className="text-center" >There are no matching items.</div>
            </div>
        );
    }


    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (props.items == null || props.items.length === 0) return null;

    return (
        <div className="row" >
            {renderTiles()}
        </div>
    )
}

export default MarketplaceTileList;