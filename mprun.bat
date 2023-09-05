docker run -d -p 5000:5000 -p 5001:5001 --name=MyMPServer -e ENABLECLOUDLIBSEARCH=false -e MARKETPLACE_MONGODB_CONNECTIONSTRING=mongodb://testuser:password@localhost:27017 -e MARKETPLACE_MONGODB_DATABASE=test mpserver

