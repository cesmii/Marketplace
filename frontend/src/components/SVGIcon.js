import React from 'react';
import color from './Constants'
import './styles/SVGicon.scss';

const _defaultIconSize = 24;

const icons = {
    "folder-profile": "M10,4 L12,6 L20,6 C21.1,6 22,6.9 22,8 L22,18 C22,19.1 21.1,20 20,20 L4,20 C2.9,20 2,19.1 2,18 L2.01,6 C2.01,4.9 2.9,4 4,4 L10,4 Z M19,11.0337361 L14,11.0337361 L14,18 L16.125,18 L17.875,17 L19,17 L19,11.0337361 Z",
    "folder-shared": "M20 6h-8l-2-2H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm-5 3c1.1 0 2 .9 2 2s-.9 2-2 2-2-.9-2-2 .9-2 2-2zm4 8h-8v-1c0-1.33 2.67-2 4-2s4 .67 4 2v1z",
    "edit": "M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z",
    "search": "M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z",
    "new-file-filled": "M18,2 C19.1045695,2 20,2.8954305 20,4 L20,19 C20,19.5522847 19.5522847,20 19,20 L14.7082039,20 C14.2424686,20 13.7831294,20.1084353 13.3665631,20.3167184 L10.6334369,21.6832816 C10.2168706,21.8915647 9.75753135,22 9.29179607,22 L5,22 C4.44771525,22 4,21.5522847 4,21 L4,4 C4,2.8954305 4.8954305,2 6,2 L18,2 Z M12,7 C11.4477153,7 11,7.39175084 11,7.875 L11,10 L9.875,10 C9.39175084,10 9,10.4477153 9,11 C9,11.5522847 9.39175084,12 9.875,12 L11,12 L11,13.125 C11,13.6082492 11.4477153,14 12,14 C12.5522847,14 13,13.6082492 13,13.125 L13,12 L15.125,12 C15.6082492,12 16,11.5522847 16,11 C16,10.4477153 15.6082492,10 15.125,10 L13,10 L13,7.875 C13,7.39175084 12.5522847,7 12,7 Z",
    "profile": "M6,2 L18,2 C19.1045695,2 20,2.8954305 20,4 L20,19 C20,19.5522847 19.5522847,20 19,20 L14.7082039,20 C14.2424686,20 13.7831294,20.1084353 13.3665631,20.3167184 L10.6334369,21.6832816 C10.2168706,21.8915647 9.75753135,22 9.29179607,22 L5,22 C4.44771525,22 4,21.5522847 4,21 L4,4 C4,2.8954305 4.8954305,2 6,2 Z",
    "extend": "M19,18 C19.5522925,18 20,18.4477153 20,19 C20,19.5128358 19.6139675,19.9355072 19.1166289,19.9932723 L19,20 L14.7519701,20 C14.4886782,20 14.2286414,20.0519803 13.9865736,20.1522527 L13.8086218,20.2364542 L11.1741386,21.6456805 C10.6370897,21.9329564 10.0425803,22.093256 9.43621775,22.1158221 L9.17575534,22.1170293 L4.97208607,21.9996101 C4.42001665,21.9841894 3.98497691,21.5241478 4.00039761,20.9720783 C4.01471684,20.4594425 4.4124082,20.0477148 4.91116588,20.0038587 L5.02792939,20.0003899 L9.23159866,20.1178091 C9.52151219,20.1259071 9.80900492,20.070844 10.0743782,19.9572661 L10.2307903,19.8821347 L12.8652735,18.4729083 C13.3731315,18.2012471 13.9327829,18.0429226 14.505723,18.0075862 L14.7519701,18 L19,18 Z M18.0000077,2 C19.1045772,2 20.0000077,2.8954305 20.0000077,4 L20.0000077,16 C20.0000077,16.5522847 19.5522925,17 19.0000077,17 L14.7082117,17 C14.2424764,17 13.7831372,17.1084353 13.3665709,17.3167184 L10.6334446,18.6832816 C10.2168783,18.8915647 9.75753908,19 9.2918038,19 L5.00000773,19 C4.44772298,19 4.00000773,18.5522847 4.00000773,18 L4.00000773,4 C4.00000773,2.8954305 4.89543823,2 6.00000773,2 L18.0000077,2 Z M12.0000077,6 C11.447723,6 11.0000077,6.39175084 11.0000077,6.875 L11,9 L9.87500773,9 C9.39175857,9 9.00000773,9.44771525 9.00000773,10 C9.00000773,10.5522847 9.39175857,11 9.87500773,11 L11,11 L11.0000077,12.125 C11.0000077,12.6082492 11.447723,13 12.0000077,13 C12.5522925,13 13.0000077,12.6082492 13.0000077,12.125 L13,11 L15.1250077,11 C15.6082569,11 16.0000077,10.5522847 16.0000077,10 C16.0000077,9.44771525 15.6082569,9 15.1250077,9 L13,9 L13.0000077,6.875 C13.0000077,6.39175084 12.5522925,6 12.0000077,6 Z",
    "error-outline": "M11 15h2v2h-2zm0-8h2v6h-2zm.99-5C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8z",
    "close": "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z",
    "chevron-left": "M15.41 7.41L14 6l-6 6 6 6 1.41-1.41L10.83 12z",
    "chevron-right": "M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z",
    "more-vert": "M12 8c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm0 2c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z",
    "account-circle": "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z",
    "delete": "M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z",
    "group": "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z",
    "add": "M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z",
    "trash": "M13.6,3 C14.9254834,3 16,4.0745166 16,5.4 L16,5.4 L16,6.2 L19.2,6.2 C19.607841,6.2 19.9444016,6.50518815 19.9937669,6.8996497 L20,7 C20,7.4418278 19.6418278,7.8 19.2,7.8 L19.2,7.8 L18.4,7.8 L18.4,18.2 C18.4,19.4702549 17.413161,20.5100212 16.1643187,20.5944631 L16,20.6 L8,20.6 C6.6745166,20.6 5.6,19.5254834 5.6,18.2 L5.6,18.2 L5.599,7.8 L4.8,7.8 C4.39215895,7.8 4.05559842,7.49481185 4.00623314,7.1003503 L4,7 C4,6.5581722 4.3581722,6.2 4.8,6.2 L4.8,6.2 L8,6.2 L8,5.4 C8,4.12974508 8.98683903,3.08997876 10.2356813,3.00553687 L10.4,3 Z M16.8,7.8 L7.199,7.8 L7.2,18.2 C7.2,18.607841 7.50518815,18.9444016 7.8996497,18.9937669 L8,19 L16,19 C16.4418278,19 16.8,18.6418278 16.8,18.2 L16.8,18.2 L16.8,7.8 Z M10.4,10.2 C10.8418278,10.2 11.2,10.5581722 11.2,11 L11.2,11 L11.2,15.8 C11.2,16.2418278 10.8418278,16.6 10.4,16.6 C9.9581722,16.6 9.6,16.2418278 9.6,15.8 L9.6,15.8 L9.6,11 C9.6,10.5581722 9.9581722,10.2 10.4,10.2 Z M13.6,10.2 C14.0418278,10.2 14.4,10.5581722 14.4,11 L14.4,11 L14.4,15.8 C14.4,16.2418278 14.0418278,16.6 13.6,16.6 C13.1581722,16.6 12.8,16.2418278 12.8,15.8 L12.8,15.8 L12.8,11 C12.8,10.5581722 13.1581722,10.2 13.6,10.2 Z M13.6,4.6 L10.4,4.6 C9.9581722,4.6 9.6,4.9581722 9.6,5.4 L9.6,5.4 L9.6,6.2 L14.4,6.2 L14.4,5.4 C14.4,4.99215895 14.0948119,4.65559842 13.7003503,4.60623314 L13.6,4.6 Z",
    "key": "M12.65 10C11.83 7.67 9.61 6 7 6c-3.31 0-6 2.69-6 6s2.69 6 6 6c2.61 0 4.83-1.67 5.65-4H17v4h4v-4h2v-4H12.65zM7 14c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2z",
    //"old-key": "M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4",
    "schema": "M14,9v2h-3V9H8.5V7H11V1H4v6h2.5v2H4v6h2.5v2H4v6h7v-6H8.5v-2H11v-2h3v2h7V9H14z",
    "access-time": "M11.99,2 C17.52,2 22,6.48 22,12 C22,17.52 17.52,22 11.99,22 C6.47,22 2,17.52 2,12 C2,6.48 6.47,2 11.99,2 Z M12,4 C7.58,4 4,7.58 4,12 C4,16.42 7.58,20 12,20 C16.42,20 20,16.42 20,12 C20,7.58 16.42,4 12,4 Z M12.5,7 L12.5,12.25 L17,14.92 L16.25,16.15 L11,13 L11,7 L12.5,7 Z",
    "favorite":"M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z",
    "favorite-border": "M16.5 3c-1.74 0-3.41.81-4.5 2.09C10.91 3.81 9.24 3 7.5 3 4.42 3 2 5.42 2 8.5c0 3.78 3.4 6.86 8.55 11.54L12 21.35l1.45-1.32C18.6 15.36 22 12.28 22 8.5 22 5.42 19.58 3 16.5 3zm-4.4 15.55l-.1.1-.1-.1C7.14 14.24 4 11.39 4 8.5 4 6.5 5.5 5 7.5 5c1.54 0 3.04.99 3.57 2.36h1.87C13.46 5.99 14.96 5 16.5 5c2 0 3.5 1.5 3.5 3.5 0 2.89-3.14 5.74-7.9 10.05z",
    "expand-less": "M12 8l-6 6 1.41 1.41L12 10.83l4.59 4.58L18 14z",
    "expand-more": "M16.59 8.59L12 13.17 7.41 8.59 6 10l6 6 6-6z",
    "arrow-drop-down": "M7 10l5 5 5-5z",
    "arrow-drop-up": "M7 14l5-5 5 5z",
    "account-tree": "M22 11V3h-7v3H9V3H2v8h7V8h2v10h4v3h7v-8h-7v3h-2V8h2v3z",
    "playlist-add": "M14 10H2v2h12v-2zm0-4H2v2h12V6zm4 8v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zM2 16h8v-2H2v2z",
    "warning": "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z",
    "customdatatype": "M20.5 11H19V7c0-1.1-.9-2-2-2h-4V3.5C13 2.12 11.88 1 10.5 1S8 2.12 8 3.5V5H4c-1.1 0-1.99.9-1.99 2v3.8H3.5c1.49 0 2.7 1.21 2.7 2.7s-1.21 2.7-2.7 2.7H2V20c0 1.1.9 2 2 2h3.8v-1.5c0-1.49 1.21-2.7 2.7-2.7 1.49 0 2.7 1.21 2.7 2.7V22H17c1.1 0 2-.9 2-2v-4h1.5c1.38 0 2.5-1.12 2.5-2.5S21.88 11 20.5 11z",
    "vertical-split": "M3 15h8v-2H3v2zm0 4h8v-2H3v2zm0-8h8V9H3v2zm0-6v2h8V5H3zm10 0h8v14h-8V5z"
}


//note React gives error if we use stroke-width (multi-name attribute). Changing it to camelcase to resolve.
export const SVGIcon = (props) => {

    //special handling of download icon which has more complex structure
    if (props.name === 'download') {
        return (
            SVGDownloadIcon(props)
        );
    }
    if (props.name === 'check') {
        return (
            SVGCheckIcon(props)
        );
    }
    if (props.name === 'view') {
        return (
            SvgVisibilityIcon(props)
        );
    }


    return (
        <svg
            className={props.className}
            width={props.size == null ? _defaultIconSize : props.size}
            height={props.size == null ? _defaultIconSize : props.size}
            viewBox='0 0 24 24'
            fill={props.fill == null ? color.shark : props.fill}
            stroke={props.stroke == null ? "none" : props.stroke}
            strokeWidth={props.strokeWidth == null ? "none" : props.strokeWidth} >
            <path d={icons[props.name]}></path>
        </svg>
    )
}

//special icon with a slightly different structure
export const LoadingIcon = (props) => {
    return (
        <svg xmlns="http://www.w3.org/2000/svg"
            width={props.size == null ? _defaultIconSize : props.size}
            height={props.size == null ? _defaultIconSize : props.size}
            viewBox="0 0 24 24"
            fill={props.fill == null ? "none" : props.fill}
            stroke={props.stroke == null ? "currentColor" : props.stroke}
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round" >
            <line x1="12" y1="2" x2="12" y2="6"></line><line x1="12" y1="18" x2="12" y2="22"></line><line x1="4.93" y1="4.93" x2="7.76" y2="7.76"></line><line x1="16.24" y1="16.24" x2="19.07" y2="19.07"></line><line x1="2" y1="12" x2="6" y2="12"></line><line x1="18" y1="12" x2="22" y2="12"></line><line x1="4.93" y1="19.07" x2="7.76" y2="16.24"></line><line x1="16.24" y1="7.76" x2="19.07" y2="4.93"></line>
        </svg>
    )
}

//re-use chevron, rotate it 45 degrees
export const TreeIndentIcon = (props) => {
    return (
        <svg
            width={props.size == null ? _defaultIconSize : props.size}
            height={props.size == null ? _defaultIconSize : props.size}
            viewBox='0 0 24 24'
            fill={props.fill == null ? "none" : props.fill}
            stroke={props.stroke == null ? "currentColor" : props.stroke}
            strokeWidth={props.strokeWidth == null ? "none" : props.strokeWidth}
            className="rotate45Negative"
            >
            <path d={icons["chevron-left"]}></path>
        </svg>
    );
}


//use some feature icons which have a different pattern than declared above.
const featherIcons = {
    "star": "12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"
}
export const SVGFeatherIcon = (props) => {
    return (
    <svg
        width={props.size == null ? _defaultIconSize : props.size}
        height={props.size == null ? _defaultIconSize : props.size}
        viewBox='0 0 24 24'
        fill={props.fill == null ? "none" : props.fill}
        stroke={props.stroke == null ? "currentColor" : props.stroke}
        strokeWidth={props.strokeWidth == null ? "2" : props.strokeWidth}
        className={props.cssClass + " feather-icon"}
    >
        <polygon points={featherIcons[props.name]} />
    </svg>
    );
            
}

//placeholder check icon
export const SVGCheckIcon = (props) => {
    return (
        <svg
            width={props.size == null ? _defaultIconSize : props.size}
            height={props.size == null ? _defaultIconSize : props.size}
            viewBox='0 0 24 24'
            fill="none"
            stroke={props.fill == null ? "currentColor" : props.fill}
            strokeWidth={props.strokeWidth == null ? "2" : props.strokeWidth}
            className={props.cssClass + " feather-icon"}
            strokeLinecap="round" strokeLinejoin="round" 
        >
            <polyline points="20 6 9 17 4 12"></polyline>
        </svg>
    );
}

export const SVGDownloadIcon = (props) => {
    return (
        <svg
            width={props.size == null ? _defaultIconSize : props.size}
            height={props.size == null ? _defaultIconSize : props.size}
            viewBox='0 0 24 24'
            fill="none"
            stroke={props.fill == null ? "currentColor" : props.fill}
            strokeWidth={props.strokeWidth == null ? "2" : props.strokeWidth}
            className={props.cssClass + " feather-icon"}
            strokeLinecap="round" strokeLinejoin="round"
        >
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
            <polyline points="7 10 12 15 17 10"></polyline>
            <line x1="12" y1="15" x2="12" y2="3"></line>
        </svg>
    );
}

export const SvgVisibilityIcon = (props) => {
    return (
        <svg
            className={props.className}
            width={props.size == null ? _defaultIconSize : props.size}
            height={props.size == null ? _defaultIconSize : props.size}
            viewBox='0 -5 24 24'
            fill="none"
        >
            <path fillRule="evenodd" clipRule="evenodd" d="M1.37493 6C1.44202 6.12972 1.54087 6.30978 1.67363 6.52424C1.96084 6.9882 2.40366 7.60812 3.02279 8.22725C4.25635 9.46081 6.18299 10.6875 9.00004 10.6875C11.8171 10.6875 13.7437 9.46081 14.9773 8.22725C15.5964 7.60812 16.0392 6.9882 16.3265 6.52424C16.4592 6.30978 16.5581 6.12972 16.6252 6C16.5581 5.87027 16.4592 5.69022 16.3265 5.47576C16.0392 5.0118 15.5964 4.39188 14.9773 3.77275C13.7437 2.53919 11.8171 1.3125 9.00004 1.3125C6.18299 1.3125 4.25635 2.53919 3.02279 3.77275C2.40366 4.39188 1.96084 5.0118 1.67363 5.47576C1.54087 5.69022 1.44202 5.87027 1.37493 6ZM17.25 6C17.7671 5.77842 17.7669 5.77816 17.7668 5.77787L17.7665 5.7772L17.7658 5.7755L17.7637 5.77067L17.7569 5.75539C17.7513 5.74274 17.7433 5.72523 17.733 5.70317C17.7125 5.65906 17.6825 5.59672 17.6427 5.51877C17.5632 5.36296 17.4443 5.14424 17.283 4.88361C16.9608 4.3632 16.4662 3.67062 15.7728 2.97725C14.3813 1.58581 12.183 0.1875 9.00004 0.1875C5.8171 0.1875 3.61874 1.58581 2.2273 2.97725C1.53393 3.67062 1.03924 4.3632 0.717082 4.88361C0.555738 5.14424 0.436886 5.36296 0.357388 5.51877C0.317618 5.59672 0.287634 5.65906 0.267049 5.70317C0.256754 5.72523 0.248804 5.74274 0.243151 5.75539L0.236385 5.77067L0.234282 5.7755L0.233547 5.7772L0.233259 5.77787C0.233137 5.77816 0.233024 5.77842 0.750043 6L0.233024 5.77842L0.138062 6L0.233024 6.22158L0.750043 6C0.233024 6.22158 0.233137 6.22184 0.233259 6.22213L0.233547 6.2228L0.234282 6.2245L0.236385 6.22933L0.243151 6.24461C0.248804 6.25726 0.256754 6.27477 0.267049 6.29683C0.287634 6.34094 0.317618 6.40328 0.357388 6.48123C0.436886 6.63704 0.555738 6.85576 0.717082 7.11639C1.03924 7.6368 1.53393 8.32938 2.2273 9.02275C3.61874 10.4142 5.8171 11.8125 9.00004 11.8125C12.183 11.8125 14.3813 10.4142 15.7728 9.02275C16.4662 8.32938 16.9608 7.6368 17.283 7.11639C17.4443 6.85576 17.5632 6.63704 17.6427 6.48123C17.6825 6.40328 17.7125 6.34094 17.733 6.29683C17.7433 6.27477 17.7513 6.25726 17.7569 6.24461L17.7637 6.22933L17.7658 6.2245L17.7665 6.2228L17.7668 6.22213C17.7669 6.22184 17.7671 6.22158 17.25 6ZM17.25 6L17.7671 6.22158L17.862 6L17.7671 5.77842L17.25 6ZM9.00004 3.5625C7.65385 3.5625 6.56254 4.65381 6.56254 6C6.56254 7.34619 7.65385 8.4375 9.00004 8.4375C10.3462 8.4375 11.4375 7.34619 11.4375 6C11.4375 4.65381 10.3462 3.5625 9.00004 3.5625ZM5.43754 6C5.43754 4.03249 7.03253 2.4375 9.00004 2.4375C10.9676 2.4375 12.5625 4.03249 12.5625 6C12.5625 7.96751 10.9676 9.5625 9.00004 9.5625C7.03253 9.5625 5.43754 7.96751 5.43754 6Z"
                fill={props.fill == null ? "currentColor" : props.fill} />
        </svg>
    );
}

export const SvgExpandMoreIcon = (props) => {
    return (
        <svg
            className={props.className}
            width="18"
            height="11"
            viewBox='0 0 18 11'
            fill="none"
            transform="rotate(-180)"
        >
            <path fillRule="evenodd" clipRule="evenodd" d="M9.00006 0.939453L17.5304 9.46978L16.4697 10.5304L9.00006 3.06077L1.53039 10.5304L0.469727 9.46978L9.00006 0.939453Z"
                fill={props.fill == null ? "currentColor" : props.fill} />
        </svg>
    );
}

export const SvgExpandLessIcon = (props) => {
    return (
        <svg
            className={props.className}
            width="18"
            height="11"
            viewBox='0 0 18 11'
            fill="none"
        >
            <path fillRule="evenodd" clipRule="evenodd" d="M9.00006 0.939453L17.5304 9.46978L16.4697 10.5304L9.00006 3.06077L1.53039 10.5304L0.469727 9.46978L9.00006 0.939453Z"
                fill={props.fill == null ? "currentColor" : props.fill} />
        </svg>
    );
}
