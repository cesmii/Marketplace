import React, { useState, useEffect, useRef } from 'react'
import { useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";
import ReactGA from 'react-ga4';

import { clearSearchCriteria, setMarketplacePageSize } from '../services/MarketplaceService';
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString, renderTitleBlock } from '../utils/UtilityService'
import GridPager from '../components/GridPager'
import MarketplaceItemRow from './shared/MarketplaceItemRow';
import { useLoadingContext, UpdateRecentFileList } from "../components/contexts/LoadingContext";

import MarketplaceFilter from './shared/MarketplaceFilter';
import HeaderSearch from '../components/HeaderSearch';
import './styles/MarketplaceList.scss';
import ProfileItemRow from './shared/ProfileItemRow';
import MarketplaceItemTypeFilter from './shared/MarketplaceItemTypeFilter';

const CLASS_NAME = "MarketplaceList";
const entityInfo = {
    name: "Marketplace Item",
    namePlural: "Marketplace Items",
    entityUrl: "/marketplace/:id",
    listUrl: "/marketplace/all"
}

function MarketplaceList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const _scrollToRef = useRef(null);
    const [_dataRows, setDataRows] = useState({
        all: [], itemCount: 0, listView: true
    });
    const [_currentPage, setCurrentPage] = useState(1);
    const [_filterToggle, setFilterToggle] = useState(false);
    
    const caption = 'Library';
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_queryLocal, setQueryLocal] = useState(loadingProps.query);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const handleOnSearchChange = (val) => {
        //raised from header nav
        //console.log(generateLogMessageString('handleOnSearchChange||Search value: ' + val, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setCurrentPage(1);
        loadingProps.searchCriteria.query = val;
        loadingProps.searchCriteria.skip = 0;
        setLoadingProps({ searchCriteria: JSON.parse(JSON.stringify(loadingProps.searchCriteria)) });
    };

    const handleOnSearchBlur = (val) => {
        setQueryLocal(val);
    };

    ///called when a marketplace item type is selected
    const onTypeSelectionChange = (criteria) => {
        //console.log(generateLogMessageString('onTypeSelectionChange||Search criteria: ' + criteria, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setCurrentPage(1);
        criteria.query = _queryLocal;
        setLoadingProps({ searchCriteria: JSON.parse(JSON.stringify(criteria)) });
    };

    //called when an item is selected in the filter panel
    const filterOnItemClick = (criteria) => {
        //filter event handler - set global state and navigate to search page
        criteria.query = _queryLocal;
        setLoadingProps({ searchCriteria: criteria });
    }

    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setCurrentPage(currentPage);
        loadingProps.searchCriteria.query = _queryLocal;
        loadingProps.searchCriteria.skip = (currentPage - 1) * pageSize; //0-based
        loadingProps.searchCriteria.take = pageSize;
        setLoadingProps({ searchCriteria: JSON.parse(JSON.stringify(loadingProps.searchCriteria)) });

        //scroll screen to top of grid on page change
        ////scroll a bit higher than the top edge so we get some of the header in the view
        window.scrollTo({ top: (_scrollToRef.current.offsetTop - 120), behavior: 'smooth' });
        //scrollToRef.current.scrollIntoView();

        //preserve choice in local storage
        setMarketplacePageSize(pageSize);
    };

    const onClearAll = () => {
        console.log(generateLogMessageString('onClearAll', CLASS_NAME));

        //clear out the selected, the query val
        var criteria = clearSearchCriteria(loadingProps.searchCriteria);

        //this will trigger the API call
        //update state for other components to see
        setLoadingProps({ searchCriteria: criteria });
        setQueryLocal(criteria.query);
        setCurrentPage(1);
    }

    const onToggleFilters = () => {
        console.log(generateLogMessageString('onToggleFilters', CLASS_NAME));

        setFilterToggle(!_filterToggle);
    }

    //const onTileViewToggle = () => {
    //    console.log(generateLogMessageString('onTileViewToggle', CLASS_NAME));
    //    setDataRows({ ..._dataRows, listView: false });
    //}

    //const onListViewToggle = () => {
    //    console.log(generateLogMessageString('onListViewToggle', CLASS_NAME));
    //    setDataRows({ ..._dataRows, listView: true });
    //}

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            var url = `marketplace/search/advanced`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            //analytics - capture search criteria 
            ReactGA.event({
                category: "Marketplace|Search",
                action: "marketplace_search"
            });

            await axiosInstance.post(url, loadingProps.searchCriteria).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setDataRows({
                        ..._dataRows,
                        all: result.data.data, itemCount: result.data.count
                    });

                    //hide a spinner
                    setLoadingProps({ isLoading: false, message: null });

                    //add to recently visited page list
                    var revisedList = UpdateRecentFileList(loadingProps.recentFileList, { url: history.location.pathname, caption: caption, iconName: "folder-setMarketplacePageSize" });
                    setLoadingProps({ recentFileList: revisedList });
                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these marketplace items.', isTimed: true }]
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
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these marketplace items.', isTimed: true }]
                    });
                }
            });
        }

        if (loadingProps.searchCriteria != null && loadingProps.searchCriteria.filters != null) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
        //type passed so that any change to this triggers useEffect to be called again
        //_setMarketplacePageSizePreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [loadingProps.searchCriteria]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-4">
                <div className="col-sm-3 mb-2 mb-sm-0">
                    {renderTitleBlock("Library", null, null)}
                </div>
                <div className="col-lg-4">
                    <HeaderSearch filterVal={loadingProps.searchCriteria == null ? null : loadingProps.searchCriteria.query} onSearch={handleOnSearchChange} onSearchBlur={handleOnSearchBlur} searchMode="standard" />
                </div>
                <div className="col-lg-5 pl-0">
                    <MarketplaceItemTypeFilter onSearchCriteriaChanged={onTypeSelectionChange} searchCriteria={loadingProps.searchCriteria} />
                </div>
            </div>
        );
    };

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
        return <GridPager currentPage={_currentPage} pageSize={loadingProps.searchCriteria.take} itemCount={_dataRows.itemCount} onChangePage={onChangePage} />
    }

    //render the main grid
    const renderItemsGrid = () => {
        if (!loadingProps.isLoading && (_dataRows.all == null || _dataRows.all.length === 0)) {
            return (
                <div className="flex-grid no-data">
                    {renderNoDataRow()}
                </div>
            )
        }
        const mainBody = _dataRows.all.map((item) => {
            if (item.type != null && item.type.code === AppSettings.itemTypeCode.smProfile) {
                return (<ProfileItemRow key={item.id} item={item} showActions={true} cssClass="marketplace-list-item" />)
            }
            else {
                return (<MarketplaceItemRow key={item.id} item={item} showActions={true} cssClass="marketplace-list-item" />)
            }
        });

        return mainBody;
    }

    //
    const renderSubTitle = () => {
        return (
            <>
                {(_dataRows.itemCount != null && _dataRows.itemCount > 0) &&
                    <span className="pl-1 text-left headline-2">{_dataRows.itemCount}{_dataRows.itemCount === 1 ? ' item' : ' items'}</span>
                }
                <span onClick={onToggleFilters} className="ml-auto d-flex d-sm-none px-2 justify-content-end clickable hover rounded" title="Show/Hide Filters" role="button" >{`${_filterToggle ? "Hide" : "Show"}`}<i className="pl-1 material-icons">filter_alt</i></span>
                <span onClick={onClearAll} className="ml-2 ml-sm-auto d-flex px-2 justify-content-end clickable hover rounded" title="Clear All Button" role="button" >Clear All<i className="pl-1 material-icons">update</i></span>
            </>
        );
    }

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
    const _description = `Search the CESMII SM Marketplace Library for apps, hardware and SM Profiles that can integrate into your SM Innovation Platform. ${AppSettings.MetaDescription.Abbreviated}`;
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
                <meta name="description" content={_description} />
                <meta property="og:title" content={AppSettings.Titles.Main + " | " + caption} />
                <meta property="og:description" content={_description} />
            </Helmet>
            {renderHeaderRow()}

            <div className="row pb-2" >
                <div className="col-sm-3 d-flex align-items-center" >
                </div>
                <div className="col-sm-9 d-flex align-items-center justify-content-end" >
                    {renderSubTitle()}
                    {/*{renderGridActions()}*/}
                </div>
            </div>

            <div className="row" >
                <div className={`col-sm-3 d-sm-block ${_filterToggle ? "" : "d-none"}`} >
                    <MarketplaceFilter searchCriteria={loadingProps.searchCriteria} selectMode="selectable" onItemClick={filterOnItemClick} showLimited={true} />
                </div>
                <div ref={_scrollToRef} className="col-sm-9 mb-4" >
                    {/*<MarketplaceFilterSelected />*/}
                    {renderItemsGrid()}
                    {renderPagination()}
                </div>
            </div>
        </>
    )
}

export default MarketplaceList;