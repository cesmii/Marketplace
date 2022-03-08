///--------------------------------------------------------------------------
/// Global constants - purely static settings that remain unchanged during the lifecycle
///--------------------------------------------------------------------------
export const AppSettings = {
    BASE_API_URL: process.env.REACT_APP_BASE_API_URL  //mock api server url - environment specific
    , Titles: { Anonymous: 'CESMII | Marketplace', Main: 'CESMII | Marketplace', Caption: "SM Marketplace"}
    , GoogleAnalyticsId: 'G-M6FGMLFKM7' //'G-EPPSP1B05X'
    , TrackAnalytics: process.env.REACT_APP_USE_GOOGLE_ANALYTICS  //false in dev, true in prod
    , PageSize: 10
    , PageSizeOptions: [10,25,50]
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
        MembershipStatus: 9
    },
     requestInfoNew: {
        marketplaceItemId: null,
        publisherId: null,
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
    }


    
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


