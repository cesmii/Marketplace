# CESMII - Marketplace

Last revision date: January 4, 2024

## Prerequisites

- Node.JS (v 14.17.3) - https://nodejs.org/en/blog/release/v14.17.3
    - npm (v 6.14.13) Node Package Manager is installed as part of Node.JS
- .NET Core 6.0.417 SDK - https://dotnet.microsoft.com/en-us/download/dotnet/6.0
- Visual Studio 2022 (17.0 or later) - https://visualstudio.microsoft.com/downloads/
- Mongo DB Community Server (6.0.12) - https://www.mongodb.com/try/download/community
- MongoDB Command Line Database Tools (100.9.4): https://www.mongodb.com/try/download/database-tools 


## Documentation
- The front-end uses React - https://reactjs.org/
- Mongo database - 
- MonoDB Command Line Database Tools - https://www.mongodb.com/docs/database-tools/


## Directories
- .\.github - Holds workflows and actions to run on github.com, or locally using nektos/act.
- .\api - This contains a .NET web API back end for marketplace. Within this solution, the DB database connections to Mongo DB will occur. 
- .\common - references submodule CESMII.Common
- .\frontend - This contains the REACT front end for the marketplace app.
- .\mongo-data - Import to populate the mongoDB database
- .\tools - Standalone helper dev tools, not used in a production system.
    - DumpAppLog - dumps records from the application log table (app-log).


## Building the Marketplace Components

1. **Clone the repo from GIT.**

2. **Setup MongoDB:**
    - Install the MongoDB Community Server. After the server has been installed, an instance of MongoDB Compass is started.
    - In MongoDB Compass, click the plus '+' sign to create a database:
        - Database Name: Marketplace
        - Collection Name: app_log
    - Open appsettings.json (in .\api\CESMII.Marketplace.API), find the MongoDBSettings object, and review the values for the following keys:
        - DatabaseName - set to "Marketplace"
        - NLogCollectionName - set to "app-log".
        - ConnectionString - set to "mongodb://localhost:27107"

3. **Populate the MongoDB Database:**
    - Download the MongoDB Command Line Database Tools. Copy the file named mongoimport.exe 
    from the bin folder to the .\mongo-data folder.
    - Open the file populateDB.bat (in .\mongo-data)
    - Check that the name of the database in the first line is correct.
        set DATABASE="Marketplace"
    - Open a command line prompt, change directory to mongo-data, then run populateDB.bat


4. **Build the front end:**

    Enter these commands in a command prompt window:

    ```ps
    cd \frontend
    npm install
    npm run start
    ```

    Verify the site is running in a browser: http://localhost:3000

4. **Build the back end API:**

    From within Visual Studio, open the solution file CESMII.Marketplace.sln. Start building the back end using the Visual Studio menu item: Build| Build Solution.


## Starting CESMII Marketplace

1. **Start the front end (in a command prompt window):**
    ```ps
    cd \frontend
    npm run start
    ```
2. **Start the back end**
    - In Visual Studio 2022, open the solution CESMII.Marketplace.sln.
    - Select the Visual Studio command Debug|Start Without Debugging


## Provision Azure AD tenant

1. In the Azure Portal for AAD, create a new Application Registration "CESMII MarketPlace", configure it for SPA using MSAL 2.0 as per https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-spa-app-registration:
   - Choose all account types: any organization and personal Microsoft accounts, don't specify a redirect URI at this point
2. Under Authentication, add platform choose Single-Page Application.

   - Enter a redirect URL pointing to the marketplace instance:

     Local development: http://localhost:3000/library.

     Staging: https://marketplace-front-stage.azurewebsites.net/library.

     Production: https://marketplace.cesmii.net/library.

   - Enter a Front-channel logout URL. Local development: https://localhost:3000/logout. Similar for stage / production.
   - Under Implicit/Hybrid Grant Flows, leave Access and ID token unchecked.

3. Under Expose an API, create

    - Add a scope "cesmii.marketplace", consent admin and users.
    - Add an Authorized client application using the appid of the SPA registration from step 1 and grant it access to the cesmii.marketplace scope.

4. Under App roles, create the following roles (all for users/groups):

```
cesmii.marketplace.user
cesmii.marketplace.marketplaceadmin
cesmii.marketplace.useradmin
cesmii.marketplace.jobadmin
```

5. Configure the application itself (local config files or in the Azure Portal Settings/Configuration) with the AAD tenant information.

