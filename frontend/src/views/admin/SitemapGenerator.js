import React, { useEffect, useState } from 'react'
import axiosInstance from "../../services/AxiosService";

import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString } from '../../utils/UtilityService';
import { Form } from 'react-bootstrap';
import { Helmet } from 'react-helmet';

const CLASS_NAME = "SitemapGenerator";

function SitemapGenerator() {

    const formatDate = (val) => {
        const d = new Date(val);
        return `${d.getFullYear()}-${d.getMonth() + 1 < 10 ? '0' : ''}${d.getMonth() + 1}-${d.getDate() < 10 ? '0' : ''}${d.getDate()}`
    };

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _minDate = new Date(new Date() - (60 * 24 * 60 * 60 * 1000));
    const [_item, setItem] = useState({
        deployDate: `${_minDate.getFullYear()}-${_minDate.getMonth() + 1 < 10 ? '0' : ''}${_minDate.getMonth() + 1}-${_minDate.getDate() < 10 ? '0' : ''}${_minDate.getDate()}`,
        maxDate: _minDate,
        domainName: 'https://marketplace.cesmii.net'
    });
    const [_items, setItems] = useState(null);
    const [_itemsLoaded, setItemsLoaded] = useState(false);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_isValid, setIsValid] = useState({ domainName: true, domainNameFormat: true, deployDate: true, deployDateValid: true });
    const [_xmlSiteMap, setXmlSiteMap] = useState('');

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            var url = `system/dynamic/sitemap`;

            await axiosInstance.post(url, loadingProps.searchCriteria).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setItems(result.data);

                    //get most recent change date
                    var maxDate = _item.maxDate;
                    result.data.forEach(itm => {
                        if (itm.updated != null && Date.parse(itm.updated) > Date.parse(_item.maxDate)) {
                            maxDate = itm.updated;
                        }
                    });
                    if (maxDate !== _item.maxDate) setItem({ ..._item, maxDate: new Date(maxDate) });

                    //hide a spinner
                    setLoadingProps({ isLoading: false, message: null });
                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the sitemap items.', isTimed: true }]
                    });
                }
                //hide a spinner
                setLoadingProps({ isLoading: false, message: null });
                setItemsLoaded(true);

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the sitemap items.', isTimed: true }]
                    });
                }
            });
        }

        if (_itemsLoaded) return;

        fetchData();

    }, [_itemsLoaded]);


    //-------------------------------------------------------------------
    // Region: useEffect
    //  re-generate sitemap XML - based on certain triggers
    //-------------------------------------------------------------------
    useEffect(() => {
        if (_items == null) return;

        //dynamic items
        setXmlSiteMap(`<?xml version="1.0" encoding="UTF-8"?>
    <sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
    ${generateItems()}
    </sitemapindex>`);

    }, [_items, _item.deployDate, _item.domainName]);

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    const validateForm_domainName = (e) => {
        const isValid = e.target.value != null && e.target.value.trim().length > 0;
        const isValidFormat = true; //validate_Url(e.target.value);
        setIsValid({ ..._isValid, domainName: isValid, domainNameFormat: isValidFormat });
    };

    const validateForm_deployDate = (e) => {
        const isValid = e.target.value != null && e.target.value.trim().length > 0;
        //const dateVal = Date.parse(e.target.value);
        const isValidDate = true; //dateVal >= Date.parse(_minDate);
        setIsValid({ ..._isValid, deployDate: isValid, deployDateValid: isValidDate });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.domainName = _item.domainName != null && _item.domainName.trim().length > 0;
        _isValid.domainNameFormat = true; //validate_Url(_item.domainName);
        _isValid.deployDate = _item.deployDate != null && _item.deployDate.trim().length > 0;
        //const dateVal = Date.parse(_item.deployDate);
        _isValid.deployDateValid = _item.deployDate != null; //&& dateVal >= Date.parse(_minDate);

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.domainName && _isValid.domainNameFormat && _isValid.deployDate && _isValid.deployDateValid);
    }

    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "domainName":
                _item[e.target.id] = e.target.value.toLowerCase();
                break;
            case "deployDate":
                //if user types directly into year field, it prematurely does an onChange event fire.
                //This prevents that:
                if (e.target.value !== '') {
                    var dt = new Date(e.target.value);
                    if (dt.getFullYear() < 2000) return;
                }
                
                //update the state
                _item[e.target.id] = e.target.value === '' ? null : e.target.value;
                //update maxDate value   //fix one day offset
                _item.maxDate = new Date(e.target.value);
                _item.maxDate = new Date(_item.maxDate.setDate(_item.maxDate.getDate() + 1));
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

    const onDownload = () => {
        console.log(generateLogMessageString('onDownload', CLASS_NAME));

        //do validation
        if (!validateForm()) {
            setLoadingProps({
                isLoading: false, message: null, inlineMessages: [
                    { id: new Date().getTime(), severity: "danger", body: `Could not download sitemap. Validation Failed.`, isTimed: true }
                ]
            });
            return;
        }

        const blob = new Blob([_xmlSiteMap], { type: 'application/xml' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = "sitemap.xml";
        document.body.appendChild(link);
        link.click();
        //console.log("clicked");
        document.body.removeChild(link);

        setLoadingProps({
            isLoading: false, message: null, inlineMessages: [
                { id: new Date().getTime(), severity: "success", body: `Sitemap downloaded. Check your downloads folder.`, isTimed: true }
            ]
        });
    };

    //-------------------------------------------------------------------
    // Region: Helper functions
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const generateDynamicItems = () => {

        if (_items == null || _items.length === 0) return '';

        var result = '';
        _items.map((itm, i) => {
            const url =
                itm.type === 'publisher' ? `${_item.domainName}/publisher/${itm.name}` :
                    itm.type === AppSettings.itemTypeCode.smProfile ? `${_item.domainName}/profile/${itm.externalSource.code}/${itm.id}` :
                        `${_item.domainName}/library/${itm.name}`;
            //don't let date be less than min date, otherwise, use updated date
            const modDate = itm.updated == null ? _item.maxDate :
                new Date(itm.updated) < new Date(_item.maxDate) ? _item.maxDate : itm.updated;
            result += `${i === 0 ? '    ' : '                '}<sitemap>
                <loc>${url}</loc>
                <lastmod>${formatDate(modDate)}</lastmod>
                </sitemap>${i === _items.length - 1 ? '' : '\r\n'}`;
        });
        return result;
    }

    const generateItems = () => {

        if (_items == null || _items.length === 0) return '';

        return `        <sitemap>
            <loc>${_item.domainName}</loc>
            <lastmod>${formatDate(_item.maxDate)}</lastmod>
            </sitemap>
            <sitemap>
                <loc>${_item.domainName}/library</loc>
                <lastmod>${formatDate(_item.maxDate)}</lastmod>
            </sitemap>
            ${generateDynamicItems()}
            <sitemap>
                <loc>${_item.domainName}/contact-us</loc>
                <lastmod>${_item.deployDate}</lastmod>
            </sitemap>`;
    }

    const renderForm = () => {
        return (
            <>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label>Domain</Form.Label>
                            {!_isValid.domainName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            {!_isValid.domainNameFormat &&
                                <span className="invalid-field-message inline">
                                    No spaces or special characters
                                </span>
                            }
                            <Form.Control id="domainName" className={(!_isValid.domainName || !_isValid.domainNameFormat ? 'invalid-field minimal pr-5' : 'minimal pr-5')} type=""
                                value={_item.domainName} onBlur={validateForm_domainName} onChange={onChange} />
                            <span className="small text-muted" >This is the base url (no trailing slash) and will be used in the formation of the urls in the sitemap. This must contain no spaces nor special characters.</span>
                        </Form.Group>
                    </div>
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label>Deploy Date</Form.Label>
                            {!_isValid.deployDate &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            {!_isValid.deployDateValid &&
                                <span className="invalid-field-message inline">
                                    Invalid date
                                </span>
                            }
                            <Form.Control id="deployDate" mindate={_minDate} type="date" value={_item.deployDate == null ? '' : _item.deployDate} onBlur={validateForm_deployDate} onChange={onChange} className={(!_isValid.deployDate || !_isValid.deployDateValid ? 'invalid-field minimal pr-5' : 'minimal pr-5')} />
                            <span className="small text-muted" >This date field will be used to derive a last modified date value for entries when a last updated date is not known.</span>
                        </Form.Group>
                    </div>
                </div>
            </>
        )
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`Sitemap Generator | Admin | ${AppSettings.Titles.Main}`}</title>
            </Helmet>
            <div className="row mb-2" >
                <div className="col-12 d-flex" >
                    <h1 className="d-inline-block mb-0">Sitemap Generator</h1>
                    <div className="ml-auto align-content-center" >
                        <a className="btn btn-icon-outline circle d-inline-block" onClick={onDownload} ><i className="material-icons">download</i></a>
                    </div>
                </div>
            </div>
            {renderForm()}
            <div className="row" >
                <div className="col-12" >
                    {(_items == null || _xmlSiteMap == null || _xmlSiteMap === '') ?
                        <pre className="border bg-white d-block w-100 p-1">

                            Generating sitemap...

                        </pre>
                        :
                        <pre className="border bg-white d-block w-100 p-1">
                            {_xmlSiteMap}
                        </pre>
                    }
                </div>
            </div>
        </>
    )
}

export default SitemapGenerator;