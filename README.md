<h1>CESMII - Marketplace</h1>
<h2>Prerequisites</h2>
<ul>
<li>
	Install node.js (version > 10.16) - https://nodejs.org/en/
</li>
<li>
	Install npm (version > 5.6) - https://www.npmjs.com/ (note I just upgraded to 7.17 =>  npm install -g npm)
</li>
<li>
	React - https://reactjs.org/
</li>
<li>
	.NET Core 5, Visual Studio 2019 or equivalent
</li>
<li>
	DB - Mongo DB - details to follow...
</li>
</ul>

<h2>Directories</h2>
<ul>
<li>
	\api - This contains a .NET web API back end for marketplace. Within this solution, the DB database connections to Mongo DB will occur. 
</li>
<li>
	\front-end - This contains the REACT front end for the marketplace app.
</li>
</ul>

<h2>How to Build</h2>
<ol>
<li>
	Clone the repo from GIT.
</li>
<li>
	<b>Build/Run the front end (Using a node.js prompt): </b>
	<ul>
		<li>
			cd \front-end
		</li>
		<li>
			npm install
		</li>
		<li>
			npm run start 
		</li>
		<li>
			Verify the site is running in a browser: http://localhost:3000
		</li>
	</ul>
</li>
<li>
	<b>Build/Run the back end API - CESMII.Marketplace.sln (.NET Solution): </b>
	<p>
		This contains the .NET web API project and supporting projects to interact with marketplace data storage. 
	</p>
</li>
<li>
	<b>Database - Mongo DB: </b>	
	<ul>
		<li>
			We use Mongo DB Compasss to directly inspect, view or edit data as needed.
		</li>
		<li>
			The DB is deployed to an Azure location but could be installed locally or to another hosting provider. 
		</li>
		<li>
			Sample collections of data are stored in the sample-data folder.
		</li>
	</ul>
</li>
</ol>
