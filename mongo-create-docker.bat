docker login
docker run -d -p 27017:27017 --name=MyMongoDB -e MONGO_INIT_DATABASE=snapshot -e MONGO_INITDB_ROOT_USERNAME=testuser -e MONGO_INITDB_ROOT_PASSWORD=password --mount src="c:/CESMII.github/Marketplace/mongo-data",target=/data,type=bind mongo:4.0
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=JobLog --file=/data/JobLog.json  --uri="mongodb://testuser:password@localhost:27017"
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=LookupItem --file=/data/LookupItem.json  --uri="mongodb://testuser:password@localhost:27017"
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=MarketplaceItem --file=/data/MarketplaceItem.json  --uri="mongodb://testuser:password@localhost:27017" 
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=MarketplaceItemAnalytics --file=/data/MarketplaceItemAnalytics.json  --uri="mongodb://testuser:password@localhost:27017" 
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=Organization --file=/data/Organization.json  --uri="mongodb://testuser:password@localhost:27017" 
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=Permission --file=/data/Permission.json  --uri="mongodb://testuser:password@localhost:27017" 
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=ProfileItem --file=/data/ProfileItem.json  --uri="mongodb://testuser:password@localhost:27017" 
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=Publisher --file=/data/Publisher.json  --uri="mongodb://testuser:password@localhost:27017" 
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=RequestInfo --file=/data/RequestInfo.json  --uri="mongodb://testuser:password@localhost:27017" 
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongostat --version
docker exec -d MyMongoDB mongoimport  --collection=SearchKeyword --file=/data/SearchKeyword.json  --uri="mongodb://testuser:password@localhost:27017" 
dotnet test ./api/Tests/CESMII.Marketplace.MongoDB/CESMII.Marketplace.MongoDB.csproj
