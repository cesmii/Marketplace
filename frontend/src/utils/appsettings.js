//#region MSAL Helper Settings
// Browser check variables
// If you support IE, our recommendation is that you sign-in using Redirect APIs
// If you as a developer are testing using Edge InPrivate mode, please add "isEdge" to the if check
const _ua = window.navigator.userAgent;
const _msie = _ua.indexOf("MSIE ");
const _msie11 = _ua.indexOf("Trident/");
const _msedge = _ua.indexOf("Edge/");
const _firefox = _ua.indexOf("Firefox");
const _isIE = _msie > 0 || _msie11 > 0;
const _isEdge = _msedge > 0;
const _isFirefox = _firefox > 0; // Only needed if you need to support the redirect flow in Firefox incognito
//#region MSAL Helper Settings

///--------------------------------------------------------------------------
/// Global constants - purely static settings that remain unchanged during the lifecycle
///--------------------------------------------------------------------------
export const AppSettings = {
    BASE_API_URL: process.env.REACT_APP_BASE_API_URL  //mock api server url - environment specific
    , Titles: { Anonymous: 'CESMII | SM Marketplace', Main: 'CESMII | SM Marketplace', Caption: "SM Marketplace"}
    , GoogleAnalyticsId: 'G-M6FGMLFKM7' //'G-EPPSP1B05X'
    , TrackAnalytics: process.env.REACT_APP_USE_GOOGLE_ANALYTICS  //false in dev, true in prod
    , PageSize: 10
    , PageSizeOptions: { FrontEnd: [10, 25, 50], Admin: [10, 25, 50, 1000] }
    , DateSettings: {
        DateFormat: 'M/d/yyyy'
        , DateFormat_Grid: 'MM/dd/yyyy'
        , DateTimeFormat_Grid: 'M/dd/yyyy h:mm a'
        , DateTimeZeroDate_Grid: '1900-01-01T00:00:00'
    }
    , SelectOneCaption: '--Select One--'
    , Messages: {
        SessionTimeoutMessage: "Your session has timed out. Please log in to continue."
        , RouteForbiddenMessage: "You are not permitted to enter this area. You have been redirected to the home page."
        , InvalidPasswordMessage: {
            MinLength: "Password must be at least 8 characters"
            , UpperCase: "Password must include an uppercase character"
            , LowerCase: "Password must include a lowercase character"
            , Number: "Password must include a number"
            , SpecialCharacter: "Password must contain a special character (ie: $@!%*?&)"
            , Zipper: "Invalid password. "
        }
    }
    , ValidatorPatterns: {
        //Email: '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$',
          Password: '(?=[^a-z]*[a-z])'
        , HasUpperLowerCase: '^(?=.*[a-z])(?=[A-Z])$' // (?=.*[A-Za-z])'
        , HasNumber: '^[0-9]$'
        , HasSpecialCharacter: '^[a-z]$' //'^[$@!%*?&]$'
        , MinLength: '{8,}'
    }
    , LookupTypeEnum: {
        Process: 1,
        IndustryVertical: 2,
        MarketplaceStatus: 5,
        Publisher: 6,
        RequestInfo: 7,
        TaskStatus: 8,
        MembershipStatus: 9,
        SmItemType: 10
    },
     requestInfoNew: {
        marketplaceItemId: null,
        publisherId: null,
        smProfileId: null,
        requestTypeCode: null,
        firstName: '',
        lastName: '',
        email: '',
        companyName: '',
        companyUrl: '',
        description: '',
        phone: '',
        industries: '',
        membershipStatusId: null
    },
    itemTypeCode: { smApp: 'sm-app', smProfile: 'sm-profile' },
    JobLogStatus: {
        NotStarted: 0,
        InProgress: 1,
        Completed: 2,
        Failed: 10,
        Cancelled: 11
    }
    //MSAL (Authentication) Config
    ,MsalConfig : {
        auth: {
            clientId: process.env.REACT_APP_MSAL_CLIENT_ID, //Application (client) id in Azure of the registered application
            //authority: process.env.REACT_APP_MSAL_AUTHORITY, //MSAL code will append client id, oauth path
            //clientId: '1b34784c-0861-404f-875b-4195e47db212', //Application (client) id in Azure of the registered application
            //authority: 'https://login.microsoftonline.com/a1590c8d-0700-4132-8b49-37fbddb2c1c2', //MSAL code will append client id, oauth path
            redirectUri: "/library", //must match with the redirect url specified in the Azure App Application. Note Azure will also need https://domainname.com/library
            postLogoutRedirectUri: "/"
        },
        cache: {
            cacheLocation: "localStorage",
            storeAuthStateInCookie: _isIE || _isEdge || _isFirefox
        }
    }
    //, MsalScopes: ["api://1b34784c-0861-404f-875b-4195e47db212/cesmii.marketplace"]  //tied to scope defined in app registration / scope, set in Azure AAD
    , MsalScopes: [process.env.REACT_APP_MSAL_SCOPE]  //tied to scope defined in app registration / scope, set in Azure AAD
    
}

export const LookupData = {
    socialMediaLinks: [
        {
            icon: "twitter",
            css: "twitter",
            url: "https://twitter.com/cesmii_sm?lang=en"
        },
        {
            icon: "linkedIn",
            css: "linkedin",
            url: "https://www.linkedin.com/company/clean-energy-smart-manufacturing-innovation-institute/"
        },
        {
            icon: "github",
            css: "github",
            url: "https://github.com/cesmii"
        }
    ],
    searchFields: [
          { caption: "Author", val: "author.fullName", dataType: "string" }
        , { caption: "Description", val: "description", dataType: "string" }
        , { caption: "Id", val: "id", dataType: "numeric" }
        , { caption: "Interface", val: "interface.name", dataType: "string" }
        , { caption: "Meta Tags", val: "metaTagsConcatenated", dataType: "string" }
        , { caption: "Name", val: "name", dataType: "string" }
        , { caption: "Type", val: "type.name", dataType: "string" }
    ],
    searchOperators: [
        { caption: "Contains", val: "contain", dataType: "string" }
        , { caption: "Equals", val: "equal", dataType: "string" }
        , { caption: "Does not contain", val: "!contain", dataType: "string" }
        , { caption: "=<", val: "lte", dataType: "numeric" }
        , { caption: "<", val: "lt", dataType: "numeric" }
        , { caption: "=", val: "=", dataType: "numeric" }
        , { caption: ">", val: "gt", dataType: "numeric" }
        , { caption: ">=", val: "gte", dataType: "numeric" }
        , { caption: "<>", val: "!equal", dataType: "numeric" }
    ]
}


