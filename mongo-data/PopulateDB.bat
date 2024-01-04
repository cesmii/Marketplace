set DATABASE="Marketplace"
mongoimport  --db=%DATABASE% --file=app_log.json
mongoimport  --db=%DATABASE% --file=app_log.json
mongoimport  --db=%DATABASE% --file=ImageItem.json
mongoimport  --db=%DATABASE% --file=JobLog.json
mongoimport  --db=%DATABASE% --file=LookupItem.json
mongoimport  --db=%DATABASE% --file=MarketplaceItem.json
mongoimport  --db=%DATABASE% --file=MarketplaceItemAnalytics.json
mongoimport  --db=%DATABASE% --file=Organization.json
mongoimport  --db=%DATABASE% --file=Permission.json
mongoimport  --db=%DATABASE% --file=ProfileItem.json
mongoimport  --db=%DATABASE% --file=Publisher.json
mongoimport  --db=%DATABASE% --file=RequestInfo.json
mongoimport  --db=%DATABASE% --file=SearchKeyword.json
mongoimport  --db=%DATABASE% --file=User.json
