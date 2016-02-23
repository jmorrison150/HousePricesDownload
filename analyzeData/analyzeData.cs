using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using MathNet.Numerics;

namespace Data {
	class analyzeData {
		int tileSize = 256;
		double initialResolution = 156543.03392804062;
		double originShift = 20037508.342789244;

		public void run() {


			MongoDB.Driver.IMongoClient client = new MongoClient(); // connect to localhost
			MongoDB.Driver.IMongoDatabase prop2 = client.GetDatabase("prop2");
			IMongoCollection<BsonDocument> prop2Collection = prop2.GetCollection<BsonDocument>("prop2");
			MongoDB.Driver.IMongoDatabase p = client.GetDatabase("p");
			IMongoCollection<BsonDocument> pCollection = p.GetCollection<BsonDocument>("p");

			Task tsk = insertDFW(prop2Collection, pCollection);
			//tsk.ConfigureAwait(true);
			tsk.Wait();

			Task t = createIndex(pCollection);
			t.Wait();
			// await collection.Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending(doc => doc["quad"]));

			// Task calculateNear = near(collection);
			// calculateNear.Wait();



		}



		public static async Task insertDFW(IMongoCollection<BsonDocument> rawData, IMongoCollection<BsonDocument> cleanData) {

			BsonDocument all = new BsonDocument();
			GlobalMapTiles.GlobalMercator proj = new GlobalMapTiles.GlobalMercator();


			int minZoom = 9;
			string quadDallas =  proj.latlngToQuadKey(32.7805664, -96.8081442, minZoom);
			//string quadFtWorth = proj.latlngToQuadKey(32.7586487, -97.3324023, minZoom);

			//List<string> quads = new List<string>();
			//quads.Add(quadDallas);
			//quads.Add(quadFtWorth);


			//foreach(string quad in quads) {
			//	process(quad, collection, maxZoom);
			//}

			int[] tileXYZ = proj.quadKeyToTileXY(quadDallas);

			double[] bounds = proj.tileLatLngBounds(tileXYZ[0], tileXYZ[1], tileXYZ[2]);
			//FilterDefinitionBuilder<BsonDocument> builder = Builders<BsonDocument>.Filter;
			//FilterDefinition<BsonDocument> filter;
			//filter = builder.Gt("lat", bounds[0]);
			//filter = filter & builder.Lt("lat", bounds[1]);
			//filter = filter & builder.Gt("lng", bounds[2]);
			//filter = filter & builder.Lt("lng", bounds[3]);


			long count = 0;

			using(var cursor = await rawData.FindAsync(all)) {
				while(await cursor.MoveNextAsync()) {
					var batch = cursor.Current;
					foreach(var doc in batch) {
						BsonArray a = doc["map"]["nearbyProperties"].AsBsonArray;


						BsonDocument[] docs = new BsonDocument[a.Count];
						for(int i = 0; i < a.Count; i++) {
							var filterID = Builders<BsonDocument>.Filter.Eq("_id", a[i][0]);
							//Task<long> previousCount = cleanData.Find(filter).CountAsync();
							//previousCount.Wait();
							//if(previousCount.Result>0) {continue;}

							if(!( a[i][7][0].BsonType==BsonType.Int32 )) { continue; }


							double lat = (double) a[i][1].AsInt32 /1000000.0;
							double lng = (double) a[i][2].AsInt32 /1000000.0;

							if(bounds[0] < lat && lat < bounds[1] && bounds[2] < lng && lng < bounds[3]) {



								int price = (int) a[i][7][0].AsInt32;
								string quad = proj.latlngToQuadKey(lat, lng, 23);

								var update = Builders<BsonDocument>.Update
									.Set("lat", lat)
									.Set("lng", lng)
									.Set("price", price)
									.Set("bed", a[i][7][1])
									.Set("bath", a[i][7][2])
									.Set("sf", a[i][7][3])
									.Set("type", a[i][7][4])
									.Set("img", a[i][7][5])
																.Set("quad", quad);
								var options = new UpdateOptions { IsUpsert = true };
								var result = await cleanData.UpdateOneAsync(filterID, update, options);

								count++;
							}
						}
					}
					Console.WriteLine(count);
				}
			}
		}
		static async Task createIndex(IMongoCollection<BsonDocument> collection) {
			await collection.Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending(_ => _["quad"]));
		}
	}
}
