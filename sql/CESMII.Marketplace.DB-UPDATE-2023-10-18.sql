---------------------------------------------------------------------
--  Marketplace DB - Update
--	Date: 2023-10-18
--	Who: SeanC
--	Details:
--	Major changes to support external source approach
---------------------------------------------------------------------

--change db - depends on target
use marketplace_db_stage;

--rename a field - example
//db.students.updateMany( {}, { $rename: { "nmae": "name" } } )

//-----------------------------------------------------------------------------------------
// MarketplaceItem
// loop over relatedProfiles collection in marketplace item and update to ExternalSource structure
//-----------------------------------------------------------------------------------------
db.MarketplaceItem.find({ 'RelatedProfiles': { $exists: 1 } })
	.forEach(function(item)
	{    
		for(i = 0; i != item.RelatedProfiles.length; ++i)
		{
			item.RelatedProfiles[i].ExternalSource = {
				"Code":"cloudlib", 
				"SourceId": ObjectId("6525a74016a01652b87feae9"), 
				"ID":item.RelatedProfiles[i].ProfileId} ;
			item.RelatedItemsExternal = item.RelatedProfiles;
		}
		
		db.MarketplaceItem.replaceOne({_id: item._id}, item);
	}
);

//-----------------------------------------------------------------------------------------
// ProfileItem
// loop over collection, rename field, convert over to ExternalSource structure
//-----------------------------------------------------------------------------------------
db.ProfileItem.find({ 'RelatedProfiles': { $exists: 1 } })
	.forEach(function(item)
	{    
		for(i = 0; i != item.RelatedProfiles.length; ++i)
		{
			item.RelatedProfiles[i].ExternalSource = {
				"Code":"cloudlib", 
				"SourceId": ObjectId("6525a74016a01652b87feae9"), 
				"ID":item.RelatedProfiles[i].ProfileId} ;
			item.RelatedItemsExternal = item.RelatedProfiles;
		}
		db.ProfileItem.replaceOne({_id: item._id}, item);
	}
);

db.ProfileItem.find({ 'ProfileId': { $exists: 1 } })
	.forEach(function(item)
	{    
		item.ExternalSource = {
			"Code":"cloudlib", 
			"SourceId": ObjectId("6525a74016a01652b87feae9"), 
			"ID":item.ProfileId} ;
		db.ProfileItem.replaceOne({_id: item._id}, item);
	}
);

//-----------------------------------------------------------------------------------------
// MarketplaceItemAnalytics
// loop over analytics collection and convert over to ExternalSource structure
//-----------------------------------------------------------------------------------------
db.MarketplaceItemAnalytics.find({ 'CloudLibId': { $ne: null } })
	.forEach(function(item)
	{    
		item.ExternalSource = {
			"Code":"cloudlib", 
			"SourceId": ObjectId("6525a74016a01652b87feae9"), 
			"ID":item.CloudLibId} ;
		
		db.MarketplaceItemAnalytics.replaceOne({_id: item._id}, item);
	}
);


//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
// MarketplaceItem
// Remove obsolete fields. Wait on this till after we convert over to perform the remove
//-----------------------------------------------------------------------------------------
db.MarketplaceItem.find({ 'RelatedProfiles': { $exists: 1 } })
	.forEach(function(item)
	{    
		for(i = 0; i != item.RelatedProfiles.length; ++i)
		{
			delete item.RelatedProfiles;
			delete item.RelatedItemsExternal[i].ProfileId;
			delete item.RelatedItemsExternal[i].ExternalSourceId;
		}
		db.MarketplaceItem.replaceOne({_id: item._id}, item);
	}
);

//-----------------------------------------------------------------------------------------
// ProfileItem
// Remove obsolete fields. Wait on this till after we convert over to perform the remove
//-----------------------------------------------------------------------------------------
db.ProfileItem.find({ 'RelatedProfiles': { $exists: 1 } })
	.forEach(function(item)
	{    
		for(i = 0; i != item.RelatedProfiles.length; ++i)
		{
			delete item.RelatedProfiles;
			delete item.RelatedItemsExternal[i].ProfileId;
			delete item.RelatedItemsExternal[i].ExternalSourceId;
		}
		db.ProfileItem.replaceOne({_id: item._id}, item);
	}
);

db.ProfileItem.find({ 'ProfileId': { $exists: 1 } })
	.forEach(function(item)
	{    
		delete item.ProfileId;
		db.ProfileItem.replaceOne({_id: item._id}, item);
	}
);

//-----------------------------------------------------------------------------------------
// MarketplaceItemAnalytics
// Remove obsolete fields. Wait on this till after we convert over to perform the remove
//-----------------------------------------------------------------------------------------
db.MarketplaceItemAnalytics.find({ 'CloudLibId': { $exists: 1 } })
	.forEach(function(item)
	{    
		delete item.CloudLibId;
		db.MarketplaceItemAnalytics.replaceOne({_id: item._id}, item);
	}
);


//-----------------------------------------------------------------------------------------
// ProfleItem
// Rename collection to reflect broader usage
//-----------------------------------------------------------------------------------------
db.getCollection('ProfileItem').aggregate( [ $out: "ExternalItem" }]);
