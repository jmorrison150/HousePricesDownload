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
		public void run() {
			int dbToggle = 1;
			//prop2
			if(dbToggle==0){
				MongoDB.Driver.IMongoClient client = new MongoClient(); // connect to localhost
			MongoDB.Driver.IMongoDatabase prop2 = client.GetDatabase("prop2");
			IMongoCollection<BsonDocument> prop2Collection = prop2.GetCollection<BsonDocument>("prop2");
			MongoDB.Driver.IMongoDatabase p = client.GetDatabase("p");
			IMongoCollection<BsonDocument> pCollection = p.GetCollection<BsonDocument>("p");

			Task tsk = insertDFW_geoJSON(prop2Collection, pCollection);
			tsk.Wait();

			//Task t = createIndex(pCollection);
			//t.Wait();
			}

			//test db
			if(dbToggle==1) {
				MongoDB.Driver.IMongoClient client = new MongoClient(); // connect to localhost
				MongoDB.Driver.IMongoDatabase prop2 = client.GetDatabase("test");
				IMongoCollection<BsonDocument> prop2Collection = prop2.GetCollection<BsonDocument>("prop1");
				MongoDB.Driver.IMongoDatabase p = client.GetDatabase("p");
				IMongoCollection<BsonDocument> pCollection = p.GetCollection<BsonDocument>("p");

				Task tsk = insertDFW_geoJSON(prop2Collection, pCollection);
				tsk.Wait();

				Task t = createIndex(pCollection);
				t.Wait();
			}

			


		}
		static async Task insertAll(IMongoCollection<BsonDocument> rawData, IMongoCollection<BsonDocument> cleanData) {
			GlobalMapTiles.GlobalMercator proj = new GlobalMapTiles.GlobalMercator();

			BsonDocument all = new BsonDocument();
			long count = 0;


			using(var cursor = await rawData.FindAsync(all)) {
				while(await cursor.MoveNextAsync()) {
					var batch = cursor.Current;
					foreach(var doc in batch) {
						BsonArray a = doc["map"]["nearbyProperties"].AsBsonArray;


						BsonDocument[] docs = new BsonDocument[a.Count];
						for(int i = 0; i < a.Count; i++) {
							var filter = Builders<BsonDocument>.Filter.Eq("_id", a[i][0]);
							//Task<long> previousCount = cleanData.Find(filter).CountAsync();
							//previousCount.Wait();
							//if(previousCount.Result>0) {continue;}

							if(!( a[i][7][0].BsonType==BsonType.Int32 )) { continue; }



							double lat = (double) a[i][1].AsInt32 /1000000.0;
							double lng = (double) a[i][2].AsInt32 /1000000.0;
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
							var result = await cleanData.UpdateOneAsync(filter, update, options);

							count++;
						}
					}
					Console.WriteLine(count);
				}
			}
		}
		static async Task insertDFW_geoJSON(IMongoCollection<BsonDocument> rawData, IMongoCollection<BsonDocument> cleanData) {

			BsonDocument all = new BsonDocument();
			GlobalMapTiles.GlobalMercator proj = new GlobalMapTiles.GlobalMercator();


			int minZoom = 9;
			string quadDallas =  proj.latlngToQuadKey(32.7805664, -96.8081442, minZoom);
			string quadFtWorth = proj.latlngToQuadKey(32.7586487, -97.3324023, minZoom);

			List<string> quads = new List<string>();
			quads.Add(quadDallas);
			quads.Add(quadFtWorth);

			int[][] tilesXYZ = new int[quads.Count][];
			double[][] bounds = new double[quads.Count][];

			for(int i = 0; i < quads.Count; i++) {
				tilesXYZ[i] = proj.quadKeyToTileXY(quads[i]);
				bounds[i] = proj.tileLatLngBounds(tilesXYZ[i][0], tilesXYZ[i][1], tilesXYZ[i][2]);
			}

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

							bool contained = false;
							//Console.WriteLine("bounds.Length = "+bounds.Length);
							for(int j = 0; j < bounds.Length; j++) {
								//Console.WriteLine("bounds[j][0] = "+bounds[j][0]);
								//Console.WriteLine("bounds[j][1] = "+bounds[j][1]);
								//Console.WriteLine("bounds[j][2] = "+bounds[j][2]);
								//Console.WriteLine("bounds[j][3] = "+bounds[j][3]);

								if(bounds[j][0] < lat && lat < bounds[j][1] && bounds[j][2] < lng && lng < bounds[j][3]) {
									contained = true;
								}
							}

							if(contained) {

								int price = (int) a[i][7][0].AsInt32;
								string quad = proj.latlngToQuadKey(lat, lng, 23);



								MongoDB.Driver.GeoJsonObjectModel.GeoJson2DCoordinates pt = new MongoDB.Driver.GeoJsonObjectModel.GeoJson2DCoordinates(lng, lat);
								BsonDocument ptBsonDocument = MongoDB.Driver.GeoJsonObjectModel.GeoJson.Point(pt).ToBsonDocument();
								BsonDocument geoJson = new BsonDocument()
									{
									  { "type", "Point"},
										{ "coordinates", ptBsonDocument }
									};


								var update = Builders<BsonDocument>.Update
									.Set("loc",geoJson)
									.Set("lat", lat)
									.Set("lng", lng)
									.Set("price", price)
									.Set("bed", a[i][7][1])
									.Set("bath", a[i][7][2])
									.Set("sf", a[i][7][3])
									.Set("type", a[i][7][4])
									.Set("img", a[i][7][5])
									.Set("quad", quad)
									;
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
		static async Task insertDFW_quadKey(IMongoCollection<BsonDocument> rawData, IMongoCollection<BsonDocument> cleanData) {

			BsonDocument all = new BsonDocument();
			GlobalMapTiles.GlobalMercator proj = new GlobalMapTiles.GlobalMercator();


			int minZoom = 9;
			string quadDallas =  proj.latlngToQuadKey(32.7805664, -96.8081442, minZoom);
			string quadFtWorth = proj.latlngToQuadKey(32.7586487, -97.3324023, minZoom);

			List<string> quads = new List<string>();
			quads.Add(quadDallas);
			quads.Add(quadFtWorth);

			int[][] tilesXYZ = new int[quads.Count][];
			double[][] bounds = new double[quads.Count][];

			for(int i = 0; i < quads.Count; i++) {
				tilesXYZ[i] = proj.quadKeyToTileXY(quads[i]);
				bounds[i] = proj.tileLatLngBounds(tilesXYZ[i][0], tilesXYZ[i][1], tilesXYZ[i][2]);
			}

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

							bool contained = false;
							for(int j = 0; j < bounds.Length; j++) {

								if(bounds[i][0] < lat && lat < bounds[i][1] && bounds[i][2] < lng && lng < bounds[i][3]) {
									contained = true;
								}

							}
							if(contained) {

								int price = (int) a[i][7][0].AsInt32;
								string quad = proj.latlngToQuadKey(lat, lng, 23);



								//MongoDB.Driver.GeoJsonObjectModel.GeoJson2DCoordinates pt = new MongoDB.Driver.GeoJsonObjectModel.GeoJson2DCoordinates(lng, lat);
								//BsonDocument ptBsonDocument = MongoDB.Driver.GeoJsonObjectModel.GeoJson.Point(pt).ToBsonDocument();
								//BsonDocument geoJson = new BsonDocument()
								//	{
								//		{ "type", "Point"},
								//		{ "coordinates", ptBsonDocument }
								//	};


								var update = Builders<BsonDocument>.Update
									//.Set("loc", geoJson)
									.Set("lat", lat)
									.Set("lng", lng)
									.Set("price", price)
									.Set("bed", a[i][7][1])
									.Set("bath", a[i][7][2])
									.Set("sf", a[i][7][3])
									.Set("type", a[i][7][4])
									.Set("img", a[i][7][5])
									.Set("quad", quad)
									;
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
			//await collection.Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending(_ => _["quad"]));
			IndexKeysDefinition<BsonDocument> indexKey = Builders<BsonDocument>.IndexKeys.GeoHaystack("loc");
			//collection.Indexes.CreateOne(indexKey);
			await collection.Indexes.CreateOneAsync(indexKey);
		}
	}
}
