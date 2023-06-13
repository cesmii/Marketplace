import React, { useState, useEffect, useRef, useMemo } from 'react'
import { useHistory, useLocation } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";
import ReactGA from 'react-ga4';
import { isNumeric } from 'jquery';

import { clearSearchCriteria, generateSearchQueryString, setMarketplacePageSize } from '../services/MarketplaceService';
import { useLoginStatus } from '../components/OnLoginHandler';
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
import { renderSchemaOrgContentMarketplaceItemList } from '../utils/schemaOrgUtil';

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
    const { search } = useLocation();
    const searchParams = useMemo(() => new URLSearchParams(search), [search]);

    const _scrollToRef = useRef(null);
    const [_dataRows, setDataRows] = useState({
        all: [], itemCount: 0, listView: true
    });
    const [_currentPage, setCurrentPage] = useState(1);
    const [_currentPageCursors, setCurrentPageCursors] = useState(null);
    //const [_currentPageEndCursor, setCurrentPageEndCursor] = useState(null);
    const [_filterToggle, setFilterToggle] = useState(false);
    
    const caption = 'Library';
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_queryLocal, setQueryLocal] = useState(loadingProps.query);
    const [_criteria, setCriteria] = useState(null);
    const { isAuthenticated, isAuthorized } = useLoginStatus(null, [AppSettings.AADAdminRole]);

    //-------------------------------------------------------------------
    // Region: Generate a new query string based on the selections
    //-------------------------------------------------------------------
/*
    const generateSearchQueryString = (criteria, currentPage) => {
        let result = [];
        //query
        if (criteria.query != null && criteria.query !== '') {
            result.push(`q=${criteria.query}`);
        }
        //sm types
        if (criteria.itemTypes != null) {
            const selTypes = criteria.itemTypes.filter(x => x.selected).map(x => x.code);
            if (selTypes != null && selTypes.length > 0) {
                result.push(`sm=${selTypes.join(',')}`);
            }
        }
        //verts, processes, etc. 
        if (criteria.filters != null) {
            let resultFilters = [];
            criteria.filters.forEach((x) => {
                const selFilters = x.items.filter(x => x.selected).map(x => x.id);
                if (selFilters != null && selFilters.length > 0) {
                    resultFilters.push(`${x.enumValue}::${selFilters.join(',')}`);
                }
            });
            if (resultFilters.length > 0) {
                result.push(`f=${resultFilters.join('|')}`);
            }
        }
        //page
        result.push(`p=${currentPage == null ? 0 : currentPage}`);
        //page size
        result.push(`t=${criteria.take}`);
        return result.join('&');
    }
*/

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const handleOnSearchChange = (val) => {
        //raised from header nav
        //console.log(generateLogMessageString('handleOnSearchChange||Search value: ' + val, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setCurrentPage(1);
        let criteria = JSON.parse(JSON.stringify(_criteria));
        criteria.query = val;
        criteria.skip = 0;
        setCriteria(criteria);
        //_currentPage = 1
        setCurrentPageCursors(null);
        //setCriteria(JSON.parse(JSON.stringify(_criteria)));
        //reload page
    //    history.push({
    //        pathname: '/library',
    //        search: `?${generateSearchQueryString(_criteria, _currentPage)}`
    //    });
    };

    const handleOnSearchBlur = (val) => {
        setQueryLocal(val);
        //_criteria.query = val;
        //setCriteria(JSON.parse(JSON.stringify(_criteria)));
    };

    ///called when a marketplace item type is selected
    const onTypeSelectionChange = (criteria) => {
        //console.log(generateLogMessageString('onTypeSelectionChange||Search criteria: ' + criteria, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setCurrentPage(1);
        criteria.query = _queryLocal;
        setCriteria(JSON.parse(JSON.stringify(criteria)));
        setCurrentPageCursors(null);
        //reload page
    //    history.push({
    //        pathname: '/library',
    //        search: `?${generateSearchQueryString(criteria, _currentPage)}`
    //    });
    };

    //called when an item is selected in the filter panel
    const filterOnItemClick = (criteria) => {

        //scroll screen to top of grid on page change
        ////scroll a bit higher than the top edge so we get some of the header in the view
        window.scrollTo({ top: (_scrollToRef.current.offsetTop - 120), behavior: 'smooth' });
        //scrollToRef.current.scrollIntoView();

        //filter event handler - set global state and navigate to search page
        criteria.query = _queryLocal;
        setCriteria(JSON.parse(JSON.stringify(criteria)));
        setCurrentPage(1);
        setCurrentPageCursors(null);
        //reload page
    //    history.push({
    //        pathname: '/library',
    //        search: `?${generateSearchQueryString(criteria, _currentPage)}`
    //    });
    }

    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setCurrentPage(currentPage);
        let criteria = JSON.parse(JSON.stringify(_criteria));
        criteria.query = _queryLocal;
        criteria.skip = (currentPage - 1) * pageSize; //0-based
        criteria.take = pageSize;
        setCriteria(criteria);

        //scroll screen to top of grid on page change
        ////scroll a bit higher than the top edge so we get some of the header in the view
        window.scrollTo({ top: (_scrollToRef.current.offsetTop - 120), behavior: 'smooth' });
        //scrollToRef.current.scrollIntoView();

        //preserve choice in local storage
        setMarketplacePageSize(pageSize);

        //reload page
    //    history.push({
    //        pathname: '/library',
    //        search: `?${generateSearchQueryString(_criteria, currentPage)}`
    //    });
    };

    const onClearAll = () => {
        console.log(generateLogMessageString('onClearAll', CLASS_NAME));

        setCriteria(clearSearchCriteria(_criteria));
        setCurrentPage(1);
        setQueryLocal(null);
        setCurrentPageCursors(null);
        //reload page
        //history.push('/library');
    }

    const onToggleFilters = () => {
        console.log(generateLogMessageString('onToggleFilters', CLASS_NAME));

        setFilterToggle(!_filterToggle);
    }

    useEffect(() => {
        if (_criteria == null) {
            // this typically happens when navigating to a page or refreshing: ignore
            return;
        }
        let newLocation = {
            pathname: '/library',
            search: `?${generateSearchQueryString(_criteria, _currentPage)}`
        };
        if (history.location.pathname !== newLocation.pathname || history.location.search != newLocation.search) {
            history.push(newLocation);
        }
    }, [_criteria, _currentPage]);


    //const onTileViewToggle = () => {
    //    console.log(generateLogMessageString('onTileViewToggle', CLASS_NAME));
    //    setDataRows({ ..._dataRows, listView: false });
    //}

    //const onListViewToggle = () => {
    //    console.log(generateLogMessageString('onListViewToggle', CLASS_NAME));
    //    setDataRows({ ..._dataRows, listView: true });
    //}

    //-------------------------------------------------------------------
    // Region: go get the data
    //-------------------------------------------------------------------
    async function fetchData(criteria) {
        //show a spinner
        setLoadingProps({ isLoading: true, message: null });

        var url = `marketplace/search/advanced`;
        console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

        //analytics - capture search criteria 
        ReactGA.event({
            category: "Marketplace|Search",
            action: "marketplace_search"
        });
        criteria.pageCursors = _currentPageCursors;

        await axiosInstance.post(url, criteria).then(result => {
            if (result.status === 200) {

                //set state on fetch of data
                setDataRows({
                    ..._dataRows,
                    all: result.data.data, itemCount: result.data.count
                });

                setCurrentPageCursors(result.data.pageCursors);
                //setCurrentPageEndCursor(result.data.endCursor);

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

    //-------------------------------------------------------------------
    // Region: Extract query string info. Build up search criteria then
    //          set state which will trigger an API call to get data. 
    //-------------------------------------------------------------------
    useEffect(() => {

        //if (searchParams == null) return;
        //set up the search criteria based on query string params
        //sample: /library?q=lit&p=0&t=25&sm=sm-app,sm-hardware&f=2::624c6de6a649292c49921f0d,629a6b34605dda89466cf5b8|1::618aa924557c7b88d5fb487b
        //q = query
        const q = searchParams.get("q");
        //p = page
        const p = searchParams.get("p");
        //t = page size
        const t = searchParams.get("t");
        //sm = sm type(s)
        const sm = searchParams.get("sm");
        //f = filters - verts, processes
        const f = searchParams.get("f");

        //build up the criteria values based on query string
        let originalCriteriaJson = JSON.stringify(_criteria);
        let criteria = (_criteria == null) ? JSON.parse(JSON.stringify(loadingProps.searchCriteria)) :
            JSON.parse(originalCriteriaJson);
        const currentPage = p == null || !isNumeric(p) ? 1 : parseInt(p);
        const pageSize = t == null || !isNumeric(t) ? criteria.take : parseInt(t);
        setCurrentPage(currentPage);
        criteria.skip = (currentPage - 1) * pageSize; //0-based
        criteria.take = pageSize;

        //no filterable query strings, just get the default list
        if (!q && !sm && !f) {
            //this will trigger a fetch from the API to pull the data for the filtered criteria
            let clearedCriteria = clearSearchCriteria(criteria, true);
            let newCriteriaJson = JSON.stringify(clearedCriteria);
            if (newCriteriaJson !== originalCriteriaJson) {
                setCriteria(clearedCriteria);
            }
            else {
                // no change: avoid triggering refetch
            }

            return;
        }

        //if we get here, then we build out filterable values (if present)
        //apply query string params
        setQueryLocal(q);
        criteria.query = q;
        //item types - update selected items, deselect all others
        //sample: sm=sm-app,sm-hardware
        const selTypes = sm == null ? [] : sm.split(",");
        criteria.itemTypes?.forEach((x) => {
            x.selected = selTypes.find(y => y.toLowerCase() === x.code.toLowerCase()) != null;
        });
        //filters
        //for each filter type (verts, processes, etc.), figure out if the query string 
        //contains the enum value which indicates this filter is being applied. If so, then 
        //apply selected to any child item that is present in the query string. Presence is indicated by the id value.
        //sample: f=2::624c6de6a649292c49921f0d,629a6b34605dda89466cf5b8|1::618aa924557c7b88d5fb487b
        const filterEnums = f == null ? [] : f.split("|");
        criteria.filters?.forEach((x) => {
            //const selFilters = filterEnums.split("::");
            const filterQS = filterEnums.find(z => z.indexOf(`${x.enumValue}::`) === 0);
            const selFilters = filterQS == null ? [] : filterQS.replace(`${x.enumValue}::`, '').split(",");
            x.items?.forEach((y) => {
                y.selected = selFilters.find(z => z.toLowerCase() === y.id.toLowerCase()) != null;
            });
        });

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        let newCriteriaJson = JSON.stringify(criteria);
        if (newCriteriaJson !== originalCriteriaJson) {
            setCriteria(JSON.parse(newCriteriaJson));
        }
        else {
            // no change: avoid triggering refetch
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [searchParams]);

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {

        if (_criteria != null && _criteria.filters != null) {
            fetchData(_criteria);
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
        //type passed so that any change to this triggers useEffect to be called again
        //_setMarketplacePageSizePreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [_criteria]);

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
                    <HeaderSearch filterVal={_criteria == null ? null : _criteria.query} onSearch={handleOnSearchChange} onSearchBlur={handleOnSearchBlur} searchMode="standard" />
                </div>
                <div className="col-lg-5 pl-0">
                    <MarketplaceItemTypeFilter onSearchCriteriaChanged={onTypeSelectionChange} searchCriteria={_criteria} />
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
        return <GridPager currentPage={_currentPage} pageSize={_criteria.take} itemCount={_dataRows.itemCount} onChangePage={onChangePage} />
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
                return (<ProfileItemRow key={item.id} item={item} showActions={true} cssClass="marketplace-list-item" isAuthenticated={isAuthenticated} isAuthorized={isAuthorized} />)
            }
            else {
                return (<MarketplaceItemRow key={item.id} item={item} showActions={true} cssClass="marketplace-list-item" isAuthenticated={isAuthenticated} isAuthorized={isAuthorized} />)
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
    const _title = `${caption} | ${AppSettings.Titles.Main}`;
    const _description = `Search the CESMII SM Marketplace Library for apps, hardware and SM Profiles that can integrate into your SM Innovation Platform. ${AppSettings.MetaDescription.Abbreviated}`;

    return (
        <>
            <Helmet>
                <title>{_title}</title>
                <meta name="description" content={_description} />
                <meta property="og:title" content={_title} />
                <meta property="og:description" content={_description} />
                {renderSchemaOrgContentMarketplaceItemList(_title, _description)}
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
                    <MarketplaceFilter searchCriteria={_criteria} selectMode="selectable" onItemClick={filterOnItemClick} showLimited={true} />
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