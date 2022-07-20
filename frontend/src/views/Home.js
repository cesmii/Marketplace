import React, { useState, useEffect } from 'react'
import { useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";

import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService'
import MarketplaceItemRow from './shared/MarketplaceItemRow';
import { useAuthState } from "../components/authentication/AuthContext";
import { useLoadingContext } from "../components/contexts/LoadingContext";

import MarketplaceFilter from './shared/MarketplaceFilter';
import HeaderSearch from '../components/HeaderSearch';
import MarketplaceTileList from './shared/MarketplaceTileList';

//slider / carousel - https://github.com/akiran/react-slick
import Slider from "react-slick";
import "slick-carousel/slick/slick.css";
import "slick-carousel/slick/slick-theme.css";
import '../components/styles/SlickSlider.scss';
import { clearSearchCriteria } from '../services/MarketplaceService';
import './styles/Home.scss';


const CLASS_NAME = "Home";

function Home() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const authTicket = useAuthState();
    const [_refreshData, setRefreshData] = useState(true);
    //const [_dataRows, setDataRows] = useState({
    //    featured: [], new: [], popular: []
    //});
    const [_dataRowsFeatured, setDataRowsFeatured] = useState([]);
    const [_dataRowsNew, setDataRowsNew] = useState([]);
    const [_dataRowsPopular, setDataRowsPopular] = useState([]);

    const _caption = 'Home';
    const { loadingProps, setLoadingProps } = useLoadingContext();
    //make a copy of this. We don't want to update the global search state unless 
    const [_searchCriteriaLocal, setSearchCriteriaLocal] = useState(null);

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {

        //1st time visitor - get the search criteria info from server if not present
        if (loadingProps == null || loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null) {
            setLoadingProps({ refreshLookupData: true, refreshSearchCriteria: true});
            return;
        }

        //do a one time search criteria init on load of page
        if (_searchCriteriaLocal == null) {
            var criteria = clearSearchCriteria(loadingProps.searchCriteria);
            setSearchCriteriaLocal(criteria);
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };

    }, [loadingProps.searchCriteria, _searchCriteriaLocal]);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const handleOnSearchChange = (val) => {
        //raised from header nav
        //console.log(generateLogMessageString('handleOnSearchChange||Search value: ' + val, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        var criteria = clearSearchCriteria(loadingProps.searchCriteria);
        criteria.query = val;
        criteria.skip = 0;
        setLoadingProps({ searchCriteria: criteria });
        history.push('/library');
    };

    //called when an item is selected in the filter panel
    const filterOnItemClick = (criteria) => {
        //filter event handler - set global state and navigate to search page
        setLoadingProps({ searchCriteria: criteria });
        history.push('/library');
    }

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    //useEffect(() => {
    //    async function fetchData() {
    //        //show a spinner
    //        setLoadingProps({ isLoading: true, message: null });

    //        var url = `marketplace/home`;
    //        console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

    //        await axiosInstance.post(url, loadingProps).then(result => {
    //            if (result.status === 200) {

    //                //set state on fetch of data
    //                setDataRows({
    //                    featured: result.data.featuredItems, new: result.data.newItems, popular: result.data.popularItems
    //                });

    //                //hide a spinner
    //                setLoadingProps({ isLoading: false, message: null });

    //            } else {
    //                setLoadingProps({
    //                    isLoading: false, message: null, inlineMessages: [
    //                        { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace items.', isTimed: true }]
    //                });
    //            }
    //            //hide a spinner
    //            setLoadingProps({ isLoading: false, message: null });
    //            setRefreshData(false);

    //        }).catch(e => {
    //            if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
    //                //do nothing, this is handled in routes.js using common interceptor
    //                //setAuthTicket(null); //the call of this will clear the current user and the token
    //            }
    //            else {
    //                setLoadingProps({
    //                    isLoading: false, message: null, inlineMessages: [
    //                        { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace items.', isTimed: true }]
    //                });
    //            }
    //        });
    //    }

    //    if (_refreshData) {
    //        fetchData();
    //    }

    //    //this will execute on unmount
    //    return () => {
    //        console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
    //        //setFilterValOnChild('');
    //    };
    //}, [_refreshData]);
    useEffect(() => {
        async function fetchDataFeatured() {
            var url = `marketplace/featured`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.post(url, loadingProps).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setDataRowsFeatured(result.data);
                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace items.', isTimed: true }]
                    });
                }
            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace items.', isTimed: true }]
                    });
                }
            });
        }
        //new
        async function fetchDataNew() {
            var url = `marketplace/recent`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.post(url, loadingProps).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setDataRowsNew(result.data);
                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace items.', isTimed: true }]
                    });
                }
            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace items.', isTimed: true }]
                    });
                }
            });
        }

        //popular
        async function fetchDataPopular() {
            var url = `marketplace/popular`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.post(url, loadingProps).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setDataRowsPopular(result.data);
                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace items.', isTimed: true }]
                    });
                }
            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the marketplace items.', isTimed: true }]
                    });
                }
            });
        }

        //split it out for more parallel processing
        if (_refreshData) {
            fetchDataFeatured();
            fetchDataNew();
            fetchDataPopular();
            setRefreshData(false);
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [_refreshData]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-4">
                {/*<div className="col-sm-3">*/}
                {/*    {renderTitleBlock("Library", null, null)}*/}
                {/*</div>*/}
                <div className="col-sm-12">
                    <HeaderSearch filterVal={_searchCriteriaLocal == null ? null : _searchCriteriaLocal.query} onSearch={handleOnSearchChange} searchMode="standard" currentUserId={authTicket.user == null ? null : authTicket.user.id} />
                </div>
            </div>
        );
    };

    //render popular
    const renderSectionHeading = (caption, showLink) => {
        return (
            <div className="row" >
                <div className="col-sm-12 d-flex align-items-center mt-3 mb-2" >
                    <h2 className="m-0 my-2">
                        {caption}
                    </h2>
                    {showLink && 
                        <a className="ml-auto" href="/library" >View All</a>
                    }
                </div>
            </div>
        )
    }

    //render featured items
    const renderFeatured = () => {

        if (_dataRowsFeatured == null || _dataRowsFeatured.length === 0) {
            return (
                <>
                { renderHeaderBlock() }
                <div className='row mx-0 p-0 marketplace-list-item border mb-0 d-flex align-items-center justify-content-center'>
                    <div className="p-5">Loading...
                    </div>
                </div>
                </>
            )
        }

        const mainBody = _dataRowsFeatured.map((item) => {
            return (<MarketplaceItemRow key={item.id} item={item} currentUserId={authTicket.user == null ? null : authTicket.user.id} showActions={true} cssClass="marketplace-list-item carousel mb-0" />)
        });

        return (
            <>
                {renderHeaderBlock()}
                <div className="row" >
                    <div className="col-sm-12 mb-4 slider-container">
                        {renderSlider(mainBody)}
                    </div>
                </div>
            </>
            );
    }

    //render new
    const renderNew = () => {
        if (loadingProps.isLoading) return;

        return (
            <>
                {renderSectionHeading('New', true)}
                <div className="row" >
                    <div className="col-sm-12">
                        {(_dataRowsNew == null || _dataRowsNew.length === 0) ?
                            (
                                <div className='row mx-0 p-0 marketplace-list-item border mb-0 d-flex align-items-center justify-content-center'>
                                    <div className="p-5">Loading...
                                    </div>
                                </div>
                            )
                            :
                            <MarketplaceTileList items={_dataRowsNew} layout="banner" colCount={3} />
                        }
                    </div>
                </div>
            </>
        );
    }

    //render popular
    const renderPopular = () => {
        if (loadingProps.isLoading) return;

        return (
            <>
                {renderSectionHeading('Popular', true)}
                <div className="row" >
                    <div className="col-sm-12">
                        {(_dataRowsPopular == null || _dataRowsPopular.length === 0) ?
                            (
                                <div className='row mx-0 p-0 marketplace-list-item border mb-0 d-flex align-items-center justify-content-center'>
                                    <div className="p-5">Loading...
                                    </div>
                                </div>
                            )
                            :
                            <MarketplaceTileList items={_dataRowsPopular} layout="thumbnail" colCount={2} />
                        }
                    </div>
                </div>
            </>
        );
    }

    const renderSlider = (content) => {
        var settings = {
            dots: true,
            arrows: false,
            infinite: true,
            speed: 2000,
            slidesToShow: 1,
            slidesToScroll: 1,
            autoplay: true,
            autoplaySpeed: 4000,
            adaptiveHeight: true
        };
        return (
            <Slider {...settings}>
                {content}
            </Slider>
        );
    }

    //
    const renderHeaderBlock = () => {
        return (
            <div className="row" >
                <div className="col-sm-12 mb-2">
                    <h1 className="m-0 mb-2 headline-2">
                        Featured Solutions
                    </h1>
                </div>
            </div>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + _caption}</title>
            </Helmet>
            {renderHeaderRow()}
            <div className="row" >
                <div className="col-sm-3 order-2 order-sm-1" >
                    <MarketplaceFilter searchCriteria={_searchCriteriaLocal} selectMode="linkable" onItemClick={filterOnItemClick} showLimited={true} />
                </div>
                <div className="col-sm-9 mb-4 order-1 order-sm-2" >
                    {renderFeatured()}
                    {renderNew()}
                    {renderPopular()}
                </div>
            </div>
        </>
    )
}

export default Home;