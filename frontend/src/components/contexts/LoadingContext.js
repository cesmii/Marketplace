import React, { useContext, useReducer, useEffect } from "react";

let reducer = (info, newInfo) => {
    if (newInfo === null) {
        localStorage.removeItem("appContext");
        sessionStorage.removeItem("searchCriteria");
        sessionStorage.removeItem("lookupDataStatic");
        return initialState;
    }
    return { ...info, ...newInfo };
};

const initialState = {
    isLoading: false,
    message: null,
    inlineMessages: [],
    recentFileList: [],
    favoritesList: [],
    marketplaceCount: null,
    refreshMarketplaceCount: null,
    isImporting: null,
    hasSidebar: false,
    searchCriteria: [],
    lookupDataStatic: [],
    refreshSearchCriteria: null,
    downloadNodesetCounter: 0
};

//Split between local storage and session storage, unify the response
//session storage would expire at end of session which forces us to get latest from server more often
const localState = () => {
    var result = JSON.parse(localStorage.getItem("appContext"));
    var searchCriteria = JSON.parse(sessionStorage.getItem("searchCriteria"));
    var lookupDataStatic = JSON.parse(sessionStorage.getItem("lookupDataStatic"));

    if (result == null && searchCriteria == null && lookupDataStatic) return initialState;

    if (result == null) result = initialState;

    result.searchCriteria = searchCriteria;
    result.lookupDataStatic = lookupDataStatic;

    return result;
};

const LoadingContext = React.createContext();

export function useLoadingContext() {
    return useContext(LoadingContext);
}

//Split between local storage and session storage, unify the response
//session storage would expire at end of session which forces us to get latest from server more often
function LoadingContextProvider(props) {
    const [loadingProps, setLoadingProps] = useReducer(reducer, localState() || initialState);

    useEffect(() => {
        if (loadingProps == null) {
            localStorage.removeItem("appContext");
            sessionStorage.removeItem("searchCriteria");
            sessionStorage.removeItem("lookupDataStatic");
        }
        else {
            var copy = JSON.parse(JSON.stringify(loadingProps));
            sessionStorage.setItem("searchCriteria", JSON.stringify(copy.searchCriteria));
            sessionStorage.setItem("lookupDataStatic", JSON.stringify(copy.lookupDataStatic));
            sessionStorage.setItem("favoritesList", JSON.stringify(copy.favoritesList));
            copy.searchCriteria = null;
            copy.lookupDataStatic = null;
            copy.favoritesList = null;
            localStorage.setItem("appContext", JSON.stringify(copy));
        }
    }, [loadingProps]);

    return (
        <LoadingContext.Provider value={{ loadingProps, setLoadingProps }}>
            {props.children}
        </LoadingContext.Provider>
    );
}


//-------------------------------------------------------------------
// Region: Update an array of recent links to use in the recent file list ui
//          Calling code will handle updating the context & storage
//-------------------------------------------------------------------
function UpdateRecentFileList(existingList, link) {
    if (existingList == null) existingList = [];

    //Check for dup entry.If dup, remove old one.
    var i = existingList.findIndex(x => x.url === link.url);
    //item found, remove it and then we re-add to top of list
    if (i >= 0) {
        existingList.splice(i, 1);
    }
    //add item to top of list
    var newList = [];
    newList.push(link);
    //TBD - limit to 20 most recent items - change this??
    //return new list
    return newList.concat(existingList.slice(0, existingList.length > 19 ? 19 : existingList.length));
}

//-------------------------------------------------------------------
// Region: Update an array of favorite links to use in the favorites list ui
//          This is a toggle. If it finds one, it removes. If not found, it adds. 
//          Calling code will handle updating the context & storage
//-------------------------------------------------------------------
function toggleFavoritesList(existingList, link) {
    if (existingList == null) existingList = [];

    //Check for existing entry.If dup, remove old one.
    var i = existingList.findIndex(x => x.url === link.url);
    //item found, remove it
    if (i >= 0) { existingList.splice(i, 1); }
    if (i < 0) { existingList.push(link) };
    return JSON.parse(JSON.stringify(existingList));
}
export { LoadingContext, LoadingContextProvider, UpdateRecentFileList, toggleFavoritesList};

