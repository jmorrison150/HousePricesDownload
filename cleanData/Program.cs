using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
//using MongoDB.Driver.Core;

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
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", a[i][0]);
                        //Task<long> previousCount = cleanData.Find(filter).CountAsync();
			            //previousCount.Wait();
                        //if(previousCount.Result>0) {continue;}

						if(!(a[i][7][0].BsonType==BsonType.Int32)){	continue; }
                        
                    
						
						double lat = (double) a[i][1].AsInt32 /1000000.0;
						double lng = (double) a[i][2].AsInt32 /1000000.0;
						int price = (int) a[i][7][0].AsInt32;
                        string quad = latlngToQuadKey(lat,lng,23);
						
						var update = Builders<BsonDocument>.Update
							.Set("lat", lat)
							.Set("lng", lng)
							.Set("price",price)
							.Set("bed", a[i][7][1])
							.Set("bath", a[i][7][2])
							.Set("sf", a[i][7][3])
							.Set("type",a[i][7][4])
							.Set("img",a[i][7][5])
                            .Set("quad",quad);
						var options = new UpdateOptions { IsUpsert = true};
						var result = cleanData.UpdateOneAsync(filter, update, options);

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
            
        DateTime startTime = DateTime.Now;
        string now = startTime.ToString("yyyy.MM.dd HH.mm");
        Console.WriteLine("Started at " + now);
            
            
		MongoDB.Driver.IMongoClient client = new MongoClient(); // connect to localhost
		MongoDB.Driver.IMongoDatabase test = client.GetDatabase("test");
		IMongoCollection<BsonDocument> collection = test.GetCollection<BsonDocument>("prop");
		IMongoCollection<BsonDocument> collection1 = test.GetCollection<BsonDocument>("prop1");

		Task tsk = insert(collection1, collection);
		tsk.Wait();
        
        Task t = createIndex(collection);
        t.Wait();
       // await collection.Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending(doc => doc["quad"]));
  
		// Task calculateNear = near(collection);
		// calculateNear.Wait();


        DateTime endTime = DateTime.Now;
        TimeSpan elapsedTime = endTime-startTime;
        Console.WriteLine("elapsedTime = "+elapsedTime);

		Console.WriteLine("Press Enter to Continue...");
		Console.ReadKey(false);
		}
	
    
    
static async Task createIndex(IMongoCollection<BsonDocument> collection)
{
    await collection.Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending(_ => _["quad"]));
}
       
        
    public static string latlngToQuadKey(double lat, double lng, int zoom){
            int tileSize = 256;
            //whole world as pixels
            double sinLatitude = Math.Sin(lat * Math.PI/180.0);
            int pixelX = (int) ( ( ( lng + 180.0 ) / 360.0 ) * 256.0 * Math.Pow(2.0, zoom) );
            int pixelY = (int) ( ( 0.5 - Math.Log(( 1.0 + sinLatitude ) / ( 1.0 - sinLatitude )) / ( 4.0 * Math.PI ) ) * 256.0 * Math.Pow(2.0, zoom) );

            //get whole tiles
            int tileX = (int) ( Math.Ceiling(pixelX / (double) ( tileSize )) - 1 );
            int tileY = (int) ( Math.Ceiling(pixelY / (double) ( tileSize )) - 1 );

            string quad = tileXYToQuadKey(tileX,tileY,zoom);
            return quad;
            
        }
    private static string tileXYToQuadKey(int tileX, int tileY, int levelOfDetail) {
            StringBuilder quadKey = new StringBuilder();
            int[] tile = googleTile(tileX, tileY, levelOfDetail);
            
            for(int i = tile[2]; i > 0; i--) {
                char digit = '0';
                int mask = 1 << ( i - 1 );
                if(( tile[0] & mask ) != 0) {
                    digit++;
                }
                if(( tile[1] & mask ) != 0) {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }
    private static int[] googleTile(int tx, int ty, int zoom) {
            //"Converts TMS tile coordinates to Google Tile coordinates"

            //# coordinate origin is moved from bottom-left to top-left corner of the extent
            int[] t = new int[3];
            t[0] = tx;
            t[1] = (int) ( ( Math.Pow(2, zoom) - 1 ) - ty );
            t[2] = zoom;
            return t;
        }
    
    
    }
}
