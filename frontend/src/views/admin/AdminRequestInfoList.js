import React, { useState, useEffect, useRef } from 'react'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService'
import GridPager from '../../components/GridPager'
import { useAuthState } from "../../components/authentication/AuthContext";
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import HeaderSearch from '../../components/HeaderSearch';
import { getRequestInfoPreferences, setRequestInfoPreferencesPageSize } from '../../services/RequestInfoService';
import AdminRequestInfoRow from './shared/AdminRequestInfoRow';

const CLASS_NAME = "AdminRequestInfoList";
const entityInfo = {
    name: "Marketplace Item",
    namePlural: "Marketplace Items",
    entityUrl: "/marketplace/:id",
    listUrl: "/marketplace/all"
}

function AdminRequestInfoList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const authTicket = useAuthState();
    const _scrollToRef = useRef(null);
    const [_dataRows, setDataRows] = useState({
        all: [], itemCount: 0, listView: true
    });
    const _requestInfoPreferences = getRequestInfoPreferences();
    const [_pager, setPager] = useState({ currentPage: 1, pageSize: _requestInfoPreferences.pageSize, searchVal: null });
    const { loadingProps, setLoadingProps } = useLoadingContext();
   
    const caption = 'Admin';

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const handleOnSearchChange = (val) => {
        //raised from header nav
        //console.log(generateLogMessageString('handleOnSearchChange||Search value: ' + val, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setPager({ ..._pager, currentPage: 1, searchVal: val });
    };

    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setPager({ ..._pager, currentPage: currentPage, pageSize: pageSize });

        //scroll screen to top of grid on page change
        ////scroll a bit higher than the top edge so we get some of the header in the view
        window.scrollTo({ top: (_scrollToRef.current.offsetTop - 120), behavior: 'smooth' });
        //scrollToRef.current.scrollIntoView();

        //preserve choice in local storage
        setRequestInfoPreferencesPageSize(pageSize);
    };

    //const onChangePage = (currentPage, pageSize) => {
    //    console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));

    //    //this will trigger a fetch from the API to pull the data for the filtered criteria
    //    setCurrentPage(currentPage);

    //    loadingProps.searchCriteria.skip = (currentPage - 1) * pageSize; //0-based
    //    loadingProps.searchCriteria.take = pageSize;
    //    setLoadingProps({ searchCriteria: JSON.parse(JSON.stringify(loadingProps.searchCriteria)) });

    //    //scroll screen to top of grid on page change
    //    ////scroll a bit higher than the top edge so we get some of the header in the view
    //    window.scrollTo({ top: (_scrollToRef.current.offsetTop - 120), behavior: 'smooth' });
    //    //scrollToRef.current.scrollIntoView();

    //    //preserve choice in local storage
    //    setMarketplacePageSize(pageSize);
    //};
     

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            var url = `requestinfo/search`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            var data = { Query: _pager.searchVal, Skip: (_pager.currentPage - 1) * _pager.pageSize, Take: _pager.pageSize };
            await axiosInstance.post(url, data).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setDataRows({
                        ..._dataRows,
                        all: result.data.data, itemCount: result.data.count
                    });

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

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these items.', isTimed: true }]
                    });
                }
            });
        }

        fetchData();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
        //type passed so that any change to this triggers useEffect to be called again
        //_setMarketplacePageSizePreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [_pager]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderNoDataRow = () => {
        return (
            <div className="alert alert-info-custom mt-2 mb-2">
                <div className="text-center" >There are no matching {entityInfo.name.toLowerCase()} records.</div>
            </div>
        );
    }

    //render pagination ui
    const renderPagination = () => {
        if (_dataRows == null || _dataRows.all.length === 0) return;
        return <GridPager currentPage={_pager.currentPage} pageSize={_pager.pageSize} itemCount={_dataRows.itemCount} onChangePage={onChangePage} />
    }

    const renderItemsGridHeader = () => {
        return (
            <thead>
                <AdminRequestInfoRow key="header" item={null} isHeader={true} cssClass="admin-item-row" />
            </thead>
        )
    }

    //render the main grid
    const renderItemsGrid = () => {
        if (!loadingProps.isLoading && (_dataRows.all == null || _dataRows.all.length === 0)) {
            return (
                <tbody>
                    <tr>
                        <td className="no-data">
                            {renderNoDataRow()}
                        </td>
                    </tr>
                </tbody>
            )
        }
        if ((_dataRows.all == null || _dataRows.all.length === 0)) return;

        const mainBody = _dataRows.all.map((item) => {
            return (
                <AdminRequestInfoRow key={item.id} item={item} cssClass="admin-item-row" />
            );
        });

        return (
            <tbody>
                {mainBody}
            </tbody>
        )
    }

    //

    //
    //const renderGridActions = () => {
    //    return (
    //        <>
    //            Sort by: tbd - drop down
    //            <Button variant="icon-solo" onClick={onListViewToggle} className={_dataRows.listView ? "ml-2" : "ml-2 inactive"} ><i className="material-icons">format_list_bulleted</i></Button>
    //            <Button variant="icon-solo" onClick={onTileViewToggle} className={!_dataRows.listView ? "ml-2" : "ml-2 inactive"}  ><i className="material-icons">grid_view</i></Button>
    //        </>
    //    );
    //}

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " Admin | " + caption}</title>
            </Helmet>
            <div className="row py-2 pb-4">
                <div className="col-sm-9">
                    <h1>Admin | Request Info (Contact Us) Items</h1>
                </div>
                <div className="col-sm-3 d-flex align-items-center" >
                    <HeaderSearch filterVal={_pager.searchVal == null ? null : _pager.searchVal} onSearch={handleOnSearchChange} searchMode="standard" currentUserId={authTicket.user == null ? null : authTicket.user.id} />
                </div>
            </div>

            <div className="row pb-2" >
                <div className="col-sm-12 d-flex align-items-center" >
                    {(_dataRows.itemCount != null && _dataRows.itemCount > 0) &&
                        <span className="pl-2 ml-auto font-weight-bold">{_dataRows.itemCount}{_dataRows.itemCount === 1 ? ' item' : ' items'}</span>
                    }
                </div>
            </div>

            <div className="row" >
                <div ref={_scrollToRef} className="col-sm-12 mb-4" >
                    <table className="flex-grid w-100" >
                    {renderItemsGridHeader()}
                    {renderItemsGrid()}
                    </table>
                    {renderPagination()}
                </div>
            </div>
        </>
    )
}

export default AdminRequestInfoList;