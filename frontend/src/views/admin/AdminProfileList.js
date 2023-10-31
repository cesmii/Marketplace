import React, { useState, useEffect, useRef } from 'react'
import { Helmet } from "react-helmet"
import { Dropdown } from 'react-bootstrap';
import axiosInstance from "../../services/AxiosService";

import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService'
import GridPager from '../../components/GridPager'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import HeaderSearch from '../../components/HeaderSearch';
import ConfirmationModal from '../../components/ConfirmationModal';
import { clearSearchCriteria, getMarketplacePreferences, setMarketplacePageSize } from '../../services/MarketplaceService';
import AdminProfileRow from './shared/AdminProfileRow';
import color from '../../components/Constants';
import OnDeleteConfirm from '../../components/OnDeleteConfirm';
import { SVGIcon } from '../../components/SVGIcon';

const CLASS_NAME = "AdminProfileList";

function AdminProfileList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _scrollToRef = useRef(null);
    const [_dataRows, setDataRows] = useState({
        all: [], itemCount: 0, listView: true
    });
    const _marketplacePreferences = getMarketplacePreferences();
    const [_pager, setPager] = useState({ currentPage: 1, pageSize: _marketplacePreferences.pageSize, searchVal: null });
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const [_itemDelete, setItemDelete] = useState(null);
    const [_itemsLookupSources, setItemsLookupSources] = useState([]);  //profile items 
    const [_loadLookupSources, setLoadLookupSources] = useState(null);

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
        setMarketplacePageSize(pageSize);
    };


    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            var url = `admin/externalsource/search`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            //get copy of search criteria structure from session storage
            var criteria = JSON.parse(JSON.stringify(loadingProps.searchCriteria));
            criteria = clearSearchCriteria(criteria);
            criteria = { ...criteria, Query: _pager.searchVal, Skip: (_pager.currentPage - 1) * _pager.pageSize, Take: _pager.pageSize };
            //append external source indicator
            await axiosInstance.post(url, criteria).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setDataRows({
                        ..._dataRows,
                        all: result.data.data
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
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
        //type passed so that any change to this triggers useEffect to be called again
    }, [_pager]);

    //-------------------------------------------------------------------
    // Trigger get related items lookups - all mktplace items, all profiles.
    //-------------------------------------------------------------------
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchData() {

            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            //get copy of search criteria structure from session storage
            var url = `admin/externalsource/lookup/sources`;
            await axiosInstance.post(url).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setItemsLookupSources(result.data);
                    setLoadLookupSources(false);

                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving external source lookup items.', isTimed: true }]
                    });
                }
                setLoadLookupSources(false);

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving external source lookup items.', isTimed: true }]
                    });
                }
                setLoadLookupSources(false);
            });
        }

        //go get the data.
        if (_loadLookupSources == null || _loadLookupSources === true) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
            //
        };
    }, [_loadLookupSources]);

    //-------------------------------------------------------------------
    // Region: Event Handling - delete item
    //-------------------------------------------------------------------
    const onDeleteItem = (itm) => {
        console.log(generateLogMessageString('onDeleteItem', CLASS_NAME));
        setItemDelete(itm);
    };

    const onDeleteComplete = (isSuccess, itm) => {
        console.log(generateLogMessageString('onDeleteComplete', CLASS_NAME));

        setItemDelete(null);

        if (!isSuccess) return;

        //remove the item from view. 
        var i = _dataRows.all.findIndex(x => x.id === itm.id);
        if (i >= 0) {
            _dataRows.all.splice(i, 1)
            setDataRows({
                ..._dataRows, all: _dataRows.all, itemCount: _dataRows.itemCount - 1
            });
        }
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderNoDataRow = () => {
        return (
            <div className="alert alert-info-custom mt-2 mb-2">
                <div className="text-center" >There are no matching items.</div>
            </div>
        );
    }

    //render pagination ui
    const renderPagination = () => {
        if (_dataRows == null || _dataRows.all?.length === 0) return;
        return <GridPager currentPage={_pager.currentPage} pageSize={_pager.pageSize} itemCount={_dataRows.itemCount} onChangePage={onChangePage}
                    pageSizeOptions={AppSettings.PageSizeOptions.Admin}/>
    }

    const renderItemsGridHeader = () => {
        if ((_dataRows.all == null || _dataRows.all.length === 0)) return;
        return (
            <thead>
                <AdminProfileRow key="header" item={null} isHeader={true} cssClass="admin-item-row" />
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
                <AdminProfileRow key={item.id} item={item} cssClass={`admin-item-row`} onDeleteItem={onDeleteItem} />
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

    const renderAddSourceDropdown = () => {
        if (_itemsLookupSources == null || _itemsLookupSources.length === 0) return;

        if (_itemsLookupSources.length === 1) {
            const src = _itemsLookupSources[0];
            return (
                <a className="btn btn-icon-outline circle primary ml-auto" href={`/admin/externalsource/${src.code}/new`} ><i className="material-icons">add</i></a>
            )
        };

        //allow for add of multiple different types of sources
        const options = _itemsLookupSources.map((src) => {
            return (
                <Dropdown.Item key={src.id} href={`/admin/externalsource/${src.code}/new`} >Add '{src.name}' Relationship</Dropdown.Item>
            )
        });

        return (
            <Dropdown className="action-menu icon-dropdown ml-auto" onClick={(e) => e.stopPropagation()} >
                <Dropdown.Toggle drop="left">
                    <SVGIcon name="more-vert" size="24" fill={color.shark} />
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    {options}
                </Dropdown.Menu>
            </Dropdown>
        );
    }


    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${caption} | Admin | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            <div className="row py-2 pb-4">
                <div className="col-sm-9">
                    <h1>Admin | External Items</h1>
                </div>
                <div className="col-sm-3 d-flex align-items-center" >
                    <HeaderSearch filterVal={_pager.searchVal == null ? null : _pager.searchVal} onSearch={handleOnSearchChange} searchMode="standard" />
                </div>
            </div>

            <div className="row pb-2" >
                <div className="col-sm-12 d-flex align-items-center" >
                    {(_dataRows.itemCount != null && _dataRows.itemCount > 0) ?
                        <>
                            <span className="px-2 ml-auto font-weight-bold">{_dataRows.itemCount}{_dataRows.itemCount === 1 ? ' item' : ' items'}</span>
                            {renderAddSourceDropdown()}
                        </>
                        :
                        renderAddSourceDropdown()
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
            <OnDeleteConfirm
                item={_itemDelete}
                onDeleteComplete={onDeleteComplete}
                urlDelete={`admin/externalsource/delete`}
                caption='Remove Related Items'
                confirmMessage={`You are about to remove all related items from '${_itemDelete?.displayName}'. This action cannot be undone.`}
                successMessage='Related items were removed.'
                errorMessage='An error occurred removing relationships'
            />
            {renderErrorMessage()}
        </>
    )
}

export default AdminProfileList;