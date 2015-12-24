using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;

namespace cleanData {
class Program {

	public static async Task insert(IMongoCollection<BsonDocument> rawData, IMongoCollection<BsonDocument> cleanData) {

		BsonDocument all = new BsonDocument();
		long count = 0;


		using(var cursor = await rawData.FindAsync(all)) {
			while(await cursor.MoveNextAsync()) {
				var batch = cursor.Current;
				foreach(var doc in batch) {
					BsonArray a = doc["map"]["nearbyProperties"].AsBsonArray;

					BsonDocument[] docs = new BsonDocument[a.Count];
					for(int i = 0; i < a.Count; i++) {
						if(!(a[i][7][0].BsonType==BsonType.Int32)){
						continue;	
						}
						
						double lat = (double) a[i][1].AsInt32 /1000000.0;
						double lng = (double) a[i][2].AsInt32 /1000000.0;
						int price = (int) a[i][7][0].AsInt32;
						
						var filter = Builders<BsonDocument>.Filter.Eq("_id", a[i][0]);
						var update = Builders<BsonDocument>.Update
							.Set("lat", lat)
							.Set("lng", lng)
							.Set("price",price)
							.Set("bed", a[i][7][1])
							.Set("bath", a[i][7][2])
							.Set("sf", a[i][7][3])
							.Set("type",a[i][7][4])
							.Set("img",a[i][7][5]);
						var options = new UpdateOptions { IsUpsert = true};
						var result = await cleanData.UpdateOneAsync(filter, update, options);

						count++;
					}
				}
				Console.WriteLine(count);
			}
		}
	}

public static async Task near(IMongoCollection<BsonDocument> collection) {
			long count = 0;
		
		var filterNear = Builders<BsonDocument>.Filter.Exists("nearMin",false); 
		using(var cursor = await collection.FindAsync(filterNear)) {
				while(await cursor.MoveNextAsync()) {
					var batch = cursor.Current;
					foreach(var doc in batch) {
						
						count++;
						try{
						double lat = doc["lat"].AsDouble;
						double lng = doc["lng"].AsDouble;
						var builder = Builders<BsonDocument>.Filter;
						FilterDefinition<BsonDocument> near = builder.Gt("lat", lat-0.01) & builder.Lt("lat", lat+0.01) & builder.Gt("lng", lng-0.01) & builder.Lt("lng", lng+0.01) & builder.Gt("price", 25000);
			
						if(collection.Find(near).CountAsync().Result==0){continue;}
												
						Task<List<BsonDocument>> tskList = collection.Find(near).Limit(1000).ToListAsync();
						tskList.Wait();
						List<BsonDocument> list = tskList.Result;
						
						int minPrice = 40000;
						minPrice = (int) list.Min(i => i["price"].AsInt32);
						int maxPrice  = 800000;
						maxPrice =(int) list.Max(i => i["price"].AsInt32);
						
						minPrice = Math.Min(minPrice,maxPrice-10);
						maxPrice = Math.Max(maxPrice,minPrice+20);
															
						var filter = Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]);
						var update = Builders<BsonDocument>.Update
							.Set("nearMin",minPrice)
							.Set("nearMax",maxPrice);
						var options = new UpdateOptions { IsUpsert = false};
						var result = await collection.UpdateOneAsync(filter, update, options);

						}
						catch{
							Console.Write(count.ToString()+",");
						}
					}
					Console.WriteLine(count);
				}
			}
}
		static void Main(string[] args) {

		MongoDB.Driver.IMongoClient client = new MongoClient(); // connect to localhost
		MongoDB.Driver.IMongoDatabase test = client.GetDatabase("test");
		IMongoCollection<BsonDocument> collection = test.GetCollection<BsonDocument>("prop");
		IMongoCollection<BsonDocument> collection1 = test.GetCollection<BsonDocument>("prop1");

		Task tsk = insert(collection1, collection);
		tsk.Wait();
		
		// Task calculateNear = near(collection);
		// calculateNear.Wait();

		Console.WriteLine("Press Enter to Continue...");
		Console.ReadKey(false);
		}
	}
}
