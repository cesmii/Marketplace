# CESMII - Marketplace

## Prerequisites

- Install node.js (version > 10.16) - https://nodejs.org/en/
- Install npm (version > 5.6) - https://www.npmjs.com/ (note I just upgraded to 7.17 =>  npm install -g npm)
- React - https://reactjs.org/
- .NET Core 5, Visual Studio 2019 or equivalent
- DB - Mongo DB - details to follow...

## Directories

- \api - This contains a .NET web API back end for marketplace. Within this solution, the DB database connections to Mongo DB will occur. 
- \frontend - This contains the REACT front end for the marketplace app.
- \images - These are starter images loaded into the system to be used in content areas.
- \sample-data - This contains JSON data that mimics the data in the stage Azure Cosmos Mongo DB.

## How to Build

1. Clone the repo from GIT.

2. **Build/Run the front end (Using a node.js prompt):**

    ```ps
    cd \frontend
    npm install
    npm run start
    ```

    Verify the site is running in a browser: http://localhost:3000

3. **Build/Run the back end API - CESMII.Marketplace.sln (.NET Solution):**

    This contains the .NET web API project and supporting projects to interact with marketplace data storage.

4. **Database - Mongo DB:**
    - We use Mongo DB Compasss to directly inspect, view or edit data as needed.
    - The DB is deployed to an Azure location but could be installed locally or to another hosting provider. 
    - Sample collections of data are stored in the sample-data folder.

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

## Local development environment

### Clone a mongo database to a local instance

1. Download all documents to a .\dump folder:

```ps1
mongodump /uri:mongodb:... /db:marketplace_db
```

If using a cloud database/ssl, make sue the uri ends in ?ssl=true.

2. Restore into a local database

```ps1
mongorestore /nsFrom:marketplace_db.* /nsTo:marketplace_db_dev.*
```