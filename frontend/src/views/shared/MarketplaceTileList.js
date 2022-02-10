import React from 'react'
import { Card } from 'react-bootstrap';

import stockTilePhoto from '../../components/img/icon-molecule-landscape.svg'
import iconMolecule from '../../components/img/icon-molecule-square.svg'
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
    const renderCardImageBanner = (item, isfirst, isLast) => {
        var imgSrc = item.imageLandscape == null ? stockTilePhoto : getImageUrl(item.imageLandscape);

        //some of the css is trying to achieve same height for tiles in a row based on variable content
        return (
            <Card className="h-100 border-0 mb-0 marketplace-tile">
                <Card.Body className="h-100 p-0 tile-body">
                    <img className="card-img-top" src={imgSrc} alt={`${item.name}-${getImageAlt(item.imageLandscape)}`} />
                    <div className="body-content p-4 pb-0" >
                        <span className="card-title font-weight-bold mb-3 d-block bitter">{item.displayName}</span>
                        <div className="card-text mb-0" dangerouslySetInnerHTML={{ __html: item.abstract }} ></div>
                    </div>
            </Card.Body>
            </Card>
        );
    }

    const renderCardThumbnail = (item, isfirst, isLast) => {
        var imgSrc = item.imageSquare == null ? iconMolecule : getImageUrl(item.imageSquare);
        return (
            <Card className="h-100 border-0 mb-0 marketplace-tile">
                <Card.Body className="h-100 p-0 tile-body">
                    <div className="row body-content p-4 pb-0" >
                        {imgSrc != null &&
                            <div className="col-3 col-sm-4" >
                                <img className={`card-img-thumb p-1 p-lg-2`} src={imgSrc} alt={`${item.name}-${getImageAlt(item.imageSquare)}`} />
                            </div>
                        }
                        <div className={`${imgSrc != null ? "col-9 col-sm-8" : "col-12 p-0"}`} >
                            <span className="card-title font-weight-bold mb-3 d-block bitter">{item.displayName}</span>
                            <div className="card-text mb-0" dangerouslySetInnerHTML={{ __html: item.abstract }} ></div>
                        </div>
                    </div>
                </Card.Body>
            </Card>
        );
    }

    const renderTiles = () => {
        if (props.items == null || props.items.length === 0) {
            return (
                <div className="flex-grid no-data">
                    {renderNoDataTile()}
                </div>
            )
        }

        var colCountCss = props.colCount == null ? 'col-sm-4' : `col-sm-${(12 / props.colCount).toString()}`;

        const mainBody = props.items.map((itm, counter) => {
            var isFirst = counter === 0;
            var isLast = counter === props.items.length;
            var mainBody = null;
            if (props.layout === "banner") {
                mainBody = renderCardImageBanner(itm, isFirst, isLast);
            }
            else {
                mainBody = renderCardThumbnail(itm, null, isFirst, isLast);
            }
            return (
                <div key={itm.id} className={`${colCountCss} pb-4`}>
                    <a href={`/library/${itm.name}`} className="tile-link" >
                        {mainBody}
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