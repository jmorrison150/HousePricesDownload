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
					//Console.WriteLine(document1["map"].ToBsonDocument()[1][0][0]);// .ElementCount);
					//int c = doc["map"]["nearbyProperties"].AsBsonArray.Count;
					//Console.WriteLine(c.ToString());
					//BsonValue v = doc["map"]["nearbyProperties"];
					BsonArray a = doc["map"]["nearbyProperties"].AsBsonArray;

					BsonDocument[] docs = new BsonDocument[a.Count];
					for(int i = 0; i < a.Count; i++) {
						
	
						//test for price>1
						//db.prop.find({price:{$not:{$gt:1}}}).count()
						//db.prop.remove({price:{$not:{$gt:1}}})

						if(!(a[i][7][0].BsonType==BsonType.Int32)){
						// Console.Write(a[i][7][0].ToString());
						// Console.Write(a[i][7][0].BsonType);
						continue;	
						}
						
						
		
						double lat = (double) a[i][1].AsInt32 /1000000.0;
						double lng = (double) a[i][2].AsInt32 /1000000.0;
						int price = (int) a[i][7][0].AsInt32;
						



						// BsonDocument document = new BsonDocument {
						// 	{"_id", a[i][0]},
						// 	{"lat", lat},
						// 	{"lng", lng},
						// 	{"price",price},
						// 	{"bed", a[i][7][1]},
						// 	{"bath", a[i][7][2]},
						// 	{"sf", a[i][7][3]},
						// 	{"type",a[i][7][4]},
						// 	{"img",a[i][7][5]}
						// 	};
						//Console.WriteLine(document);
						//await cleanData.InsertOneAsync(document);
						//cleanData.BulkWriteAsync(docs);
											
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

						// ReplaceOneResult result = await cleanData.ReplaceOneAsync(
						// 	filter: new BsonDocument("_id", a[i][0]),
						// 	options: new UpdateOptions { IsUpsert = true },
						// 	replacement: document);

						count++;

					}

						//await cleanData.InsertManyAsync(docs);
					//Console.WriteLine(docs);
						
  




					//Console.WriteLine(datas.map.nearbyProperties[i]);
            //Console.WriteLine(datas.map.nearbyProperties[i][1]);
            //Console.WriteLine(datas.map.nearbyProperties[i][2]);
            //Console.WriteLine(datas.map.nearbyProperties[i][7][0]);
            //Console.WriteLine(datas.map.nearbyProperties[i][7][1]);
            //Console.WriteLine(datas.map.nearbyProperties[i][7][2]);
            //Console.WriteLine(datas.map.nearbyProperties[i][7][3]);
				}


				Console.WriteLine(count);

			}

			//save bitmap
			//if(count==0) { return; }
			//bitmap.Save("./data/"+quadTree+".png");
			//Console.WriteLine(quadTree);
		}




		//count = await rawData.CountAsync(new BsonDocument());
		////count = await rawData.CountAsync(document);
		//Console.WriteLine(count);


		//var document = await rawData.Find(new BsonDocument()).FirstOrDefaultAsync();
		//Console.WriteLine(document.ToString());


		//ProjectionDefinition<BsonDocument> projection = Builders<BsonDocument>.Projection.Exclude("list").Exclude("map"); //.Include("map");// 
		//BsonDocument document = await rawData.Find(new BsonDocument()).Project(projection).FirstAsync();
		//Console.WriteLine(document.ToString());



		//ProjectionDefinition<BsonDocument> projection2 = Builders<BsonDocument>.Projection.Include("map");
		//BsonDocument document2 = await rawData.Find(new BsonDocument()).Project(projection2).FirstAsync();
		//Console.WriteLine(document2.ToString());


		//works
		//ProjectionDefinition<BsonDocument> projection1 = Builders<BsonDocument>.Projection.Include("map").Exclude("_id");
		//BsonDocument document1 = await rawData.Find(new BsonDocument()).Project(projection1).FirstAsync();
		//Console.WriteLine(document1.Names.ToJson());

		//Console.WriteLine(( from p in rawData.AsQueryable() select p["FullName"] ).toList());
		//IndexKeysDefinition<BsonDocument> keys = Builders<BsonDocument>.IndexKeys.Ascending("map");
		
		
		//works
		//Console.WriteLine(document1["map"].ToString());
		//Console.WriteLine(document1["map"].ToBsonDocument()[1][0][0]);// .ElementCount);
		//Console.WriteLine(document1["map"].ToBsonDocument()[2]);
		//Console.WriteLine(document1["map"].ToBsonDocument()[3]);

		//var foos = await rawData.Find(f => f.Bars.Any(fb => fb.BarId == "123")).ToListAsync();
		//var doc = document1.ToList();
		//Console.WriteLine(doc.Count);
		//Console.WriteLine(document1.ToList());
		//BsonArray docArray = document1.AsBsonArray;
		//for(int i = 0; i < docArray.Count; i++) {
		//	//Console.WriteLine(docArray[i]);
		//}

		//foreach(var doc in document1.AsBsonArray) {
		//	var country = doc.AsBsonDocument["country"].AsString;

		//}


		//foreach(var addr in document1["shipping_address"].AsBsonArray) {
		//	var country = addr.AsBsonDocument["country"].AsString;
		//}



		//var findFluent = rawData.Find(f => f.map.Any(map => map.index == "1"));
		//Console.WriteLine(findFluent.ToString());
		//	//rawData.Find(Builders<Foo>.Filter.ElemMatch(
			//foo => foo.Bars,
			//foobar => foobar.BarId == "123"
			//));


		//	//builder.All<BsonDocument>( document,rawData);
		//// = builder.Gt("lat", bounds[0]) & builder.Lt("lat", bounds[1]) & builder.Gt("lng", bounds[2]) & builder.Lt("lng", bounds[3]);


		//using(var cursor = await rawData.FindAsync(new BsonDocument())) {
		//	while(await cursor.MoveNextAsync()) {
		//		var batch = cursor.Current;
		//		foreach(var doc in batch) {
		//			//Draw.circle((double) doc["price"], (double) doc["lat"], (double) doc["lng"], zoom, bitmap);


		//			//collection.InsertOneAsync(document);
		//			count++;


		//			}
		//		}
		//}
		//	Console.WriteLine(count);

	}

public static async Task near(IMongoCollection<BsonDocument> collection) {

			// double[] bounds = proj.tileLatLngBounds(tileXYZ[0], tileXYZ[1], tileXYZ[2]);
			// var builder = Builders<BsonDocument>.Filter;
			// var filter = builder.Gt("lat", bounds[0]) & builder.Lt("lat", bounds[1]) & builder.Gt("lng", bounds[2]) & builder.Lt("lng", bounds[3]);
			// long count = collection.Find(filter).CountAsync().Result;
			// if(count==0) { return count; }
			long count = 0;
		
		var filterNear = Builders<BsonDocument>.Filter.Exists("nearMin",false); 
		//filter = new BsonDocument();
		//count = collection.Find(filter).CountAsync().Result;
		//Console.WriteLine(count.ToString());
		using(var cursor = await collection.FindAsync(filterNear)) {
				while(await cursor.MoveNextAsync()) {
					var batch = cursor.Current;
					foreach(var doc in batch) {
						
						count++;

						//if(count%100!=0){continue;}
						
						try{
						//Draw.near(doc["price"].AsInt32, doc["lat"].AsDouble, doc["lng"].AsDouble, zoom, bitmap, collection);
						double lat = doc["lat"].AsDouble;
						double lng = doc["lng"].AsDouble;
						var builder = Builders<BsonDocument>.Filter;
						FilterDefinition<BsonDocument> near = builder.Gt("lat", lat-0.01) & builder.Lt("lat", lat+0.01) & builder.Gt("lng", lng-0.01) & builder.Lt("lng", lng+0.01) & builder.Gt("price", 25000);
			
			// Console.WriteLine(lat.ToString());
			// Console.WriteLine(lng.ToString());
			// Console.WriteLine(doc["price"].ToString());
			
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
															
						
						// //if(doc["price"].BsonType!=BsonType.Int32) { continue; }
						// BsonDocument document = new BsonDocument {
						// 	{"_id", doc["_id"]},
						// 	{"lat", doc["lat"]},
						// 	{"lng", doc["lng"]},
						// 	{"price",doc["price"]},
						// 	{"bed", doc["bed"]},
						// 	{"bath", doc["bath"]},
						// 	{"sf", doc["sf"]},
						// 	{"type",doc["type"]},
						// 	{"img",doc["img"]},
						// 	{"nearMin",minPrice},
						// 	{"nearMax",maxPrice}
							

						// 	};
						// ReplaceOneResult result = await collection.ReplaceOneAsync(
						// 	filter: new BsonDocument("_id", doc["_id"]),
						// 	options: new UpdateOptions { IsUpsert = true },
						// 	replacement: document);
						
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
