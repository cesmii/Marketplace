import React, { useState, useEffect, useRef } from 'react'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService'
import AdminImageList from './shared/AdminImageList';

const CLASS_NAME = "AdminStockImageList";

function AdminStockImageList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _scrollToRef = useRef(null);
    const [_refreshImageData, setRefreshImageData] = useState(true);
    const [_dataRows, setDataRows] = useState({all: [], itemCount: 0});
   
    const caption = 'Admin';

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - get static lookup data
    //-------------------------------------------------------------------
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchImageData() {

            var url = `image/all`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.post(url, { id: null }).then(result => {
                if (result.status === 200) {
                    setDataRows({ all: result.data, itemCount: result.data.length });
                } else {
                    setDataRows({ all: [], itemCount: 0 });
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
    }, [_refreshImageData]);

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
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${caption} | Admin | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            <div className="row py-2 pb-0">
                <div className="col-sm-9">
                    <h1 className="mb-0">Admin | Stock Images</h1>
                </div>
            </div>

            <div className="row" >
                <div ref={_scrollToRef} className="col-sm-12 mb-4" >
                    <AdminImageList items={_dataRows.all} onImageUpload={onImageUpload} onDeleteItem={onDeleteImage} marketplaceItemId={null} />
                </div>
            </div>
        </>
    )
}

export default AdminStockImageList;