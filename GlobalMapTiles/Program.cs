using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Drawing;

namespace GlobalMapTiles {
	class Program {
		static void Main(string[] args) {
			GlobalMapTiles.DrawMap drawMapInstance = new DrawMap();
			drawMapInstance.initialize();
		}
	}


	class DrawMap {
		public void initialize() {

			DateTime startTime = DateTime.Now;
			string now = startTime.ToString("yyyy.MM.dd HH.mm");
			Console.WriteLine("Started at " + now);

			var client = new MongoClient("mongodb://localhost:27017");
			var database = client.GetDatabase("test");
			var collection = database.GetCollection<BsonDocument>("prop");
            int maxZoom = 13;

			//world
            //for(int maxZoom=7;maxZoom<=11;maxZoom+=4){
			process("3", collection, maxZoom);
			process("2", collection, maxZoom);
			process("1", collection, maxZoom);
			process("0", collection, maxZoom);
            //}


			// ////dfw
			// GlobalMercator pr = new GlobalMercator();
			// string fortWorth = pr.tileXYToQuadKey(117, 206, 9);
			// string dallas = pr.tileXYToQuadKey(118, 206, 9);
			// Task tsk = queryToBitmap(fortWorth, collection);
			// tsk.Wait();
			// tsk = queryToBitmap(dallas, collection);
			// tsk.Wait();


			DateTime endTime = DateTime.Now;
			TimeSpan elapsedTime = endTime-startTime;
			Console.WriteLine("elapsedTime = "+elapsedTime);
			pause();


		}
		void process(string quadTree, IMongoCollection<BsonDocument> collection, int maxZoom) {

			Task<long> tsk = queryToBitmap(quadTree, collection);
			tsk.Wait();

			if(tsk.Result>0) {
				int zoom = quadTree.Length;
		
				if(zoom <= maxZoom) {
					string[] childrenTiles = getChildrenTiles(quadTree);
					foreach(string child in childrenTiles) {
						process(child, collection, maxZoom);
					}
				}
			}

		}
		static async Task<long> queryToBitmap(string quadTree, IMongoCollection<BsonDocument> collection) {
			GlobalMercator proj = new GlobalMercator();
			int[] tileXYZ = proj.quadKeyToTileXY(quadTree);
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(256, 256);
			//long count = 0;
			int zoom = quadTree.Length;

			double[] bounds = proj.tileLatLngBounds(tileXYZ[0], tileXYZ[1], tileXYZ[2]);
			var builder = Builders<BsonDocument>.Filter;
			//var filter = builder.Exists("nearMin", true) & builder.Gt("lat", bounds[0]) & builder.Lt("lat", bounds[1]) & builder.Gt("lng", bounds[2]) & builder.Lt("lng", bounds[3]);
			var filter =builder.Gt("lat", bounds[0]) & builder.Lt("lat", bounds[1]) & builder.Gt("lng", bounds[2]) & builder.Lt("lng", bounds[3]);
			
            long count = collection.Find(filter).CountAsync().Result;
			Console.Write(count.ToString()+",");
			if(count==0) { return count; }

			using(var cursor = await collection.FindAsync(filter)) {
				while(await cursor.MoveNextAsync()) {
					var batch = cursor.Current;
					int current = 0;

					foreach(var doc in batch) {
						current++;
						//if(!(5000<current && current<10000 && current%3==0)){continue;}

						if(doc["price"].BsonType!=BsonType.Int32) { continue; }

						int p = (int) doc["price"].AsInt32;
						double latitude =(double) doc["lat"].AsDouble;
						double longitude =(double) doc["lng"].AsDouble;

						Draw.price(p , latitude, longitude, zoom, bitmap);
						//Draw.near(doc["price"].AsInt32, doc["nearMin"].AsInt32, doc["nearMax"].AsInt32, doc["lat"].AsDouble, doc["lng"].AsDouble, zoom, bitmap);
					}
				}
			}

			////save bitmap
			string pathString = @"C:\data\HousePricesDownload\web\images\"+tileXYZ[2]+"/"+tileXYZ[0]+"/";
			System.IO.Directory.CreateDirectory(pathString);
			bitmap.Save(@"C:\data\HousePricesDownload\web\images\"+tileXYZ[2]+"/"+tileXYZ[0]+"/"+tileXYZ[1]+".png");


			return count;

		}
		string[] getChildrenTiles(string quadTree) {
			string[] children = new string[4];
			children[3] = quadTree+"0";
			children[2] = quadTree+"1";
			children[1] = quadTree+"2";
			children[0] = quadTree+"3";

			return children;
		}
		private void pause() {
			Console.WriteLine("Press Enter to Continue...");
			Console.ReadKey(false);
		}

	}



	public class Draw {
		public static int map(int value1, int min1, int max1, int min2, int max2) {
			int value2 = min2 + ( value1 - min1 ) * ( max2 - min2 ) / ( max1 - min1 );
			return value2;
		}
		public static double map(double value1, double min1, double max1, double min2, double max2) {
			double value2 = min2 + ( value1 - min1 ) * ( max2 - min2 ) / ( max1 - min1 );
			return value2;
		}
		public static void price(int price, double lat, double lng, int zoom, Bitmap bitmap) {
			int minPrice = 40000;
			int maxPrice = 800000;

			near(price, minPrice, maxPrice, lat, lng, zoom, bitmap);
		}
		public static void near(int price, int nearMin, int nearMax, double lat, double lng, int zoom, Bitmap bitmap) {

			int minPrice = nearMin;
			int maxPrice = nearMax;

			int r = (int) map(price, minPrice, maxPrice, 0, 255);
			int g = 255-Math.Abs((int) map(price, minPrice, maxPrice, -255, 255));
			int b = (int) map(price, minPrice, maxPrice, 255, 0);
			r = Math.Max(Math.Min(r, 255), 0);
			g = Math.Max(Math.Min(g, 255), 0);
			b = Math.Max(Math.Min(b, 255), 0);
			Color color = Color.FromArgb(255, r, g, b);

			GlobalMercator proj = new GlobalMercator();
			int[] pixel = proj.latLngToTile(lat, lng, zoom);

			if(zoom<=8) {
				bitmap.SetPixel(pixel[0], pixel[1], color);
			} else {

				double size0 = zoom-5.0;
				int size = (int) Math.Max(size0, 1);
				int px = ( pixel[0]-(int) ( size*0.5 ) );
				int py = ( pixel[1]-(int) ( size*0.5 ) );
				Graphics grf = Graphics.FromImage(bitmap);
				Brush brsh = new SolidBrush(color);
				grf.FillEllipse(brsh, px, py, size, size);
				grf.Dispose();
			}
		}
	}
}