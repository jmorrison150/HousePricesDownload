using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
//using MongoDB.Driver.Core;
using MongoDB.Driver.Linq;
using System.Drawing;
using System.Linq;


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
			int maxZoom = 9;

            //detailed dfw
            dfw(collection,17,21);
            dfw(collection,15,20);
            dfw(collection,13,19);
            dfw(collection,11,17);
            dfw(collection,9,17);
            dfw(collection,9,18);
            dfw(collection,9,19);
            dfw(collection,9,20);
            dfw(collection,9,21);
            
			//quick feedback
			for(int i = 1; i < maxZoom; i++) {
				process("0", collection, i);
				process("1", collection, i);
				process("2", collection, i);
				process("3", collection, i);
			}

			//detailed cities
			cities(collection, maxZoom, 13);
			cities(collection, 12, 16);
			cities(collection, 15, 17);

			//let it run
			for(int i = maxZoom; i < 17; i++) {
				process("0", collection, i);
				process("1", collection, i);
				process("2", collection, i);
				process("3", collection, i);
			}




			DateTime endTime = DateTime.Now;
			TimeSpan elapsedTime = endTime-startTime;
			Console.WriteLine("elapsedTime = "+elapsedTime);
			pause();

		}
        
        void dfw(IMongoCollection<BsonDocument> collection, int minZoom, int maxZoom) {

			GlobalMercator proj = new GlobalMercator();



			string quadDallas =  proj.latlngToQuadKey(32.7805664, -96.8081442, minZoom);
			string quadFtWorth = proj.latlngToQuadKey(32.7586487, -97.3324023, minZoom);
			
			List<string> quads = new List<string>();
			quads.Add(quadDallas);
			quads.Add(quadFtWorth);
		

			foreach(string quad in quads) {
				process(quad, collection, maxZoom);
			}



		}
		void cities(IMongoCollection<BsonDocument> collection, int minZoom, int maxZoom) {

			GlobalMercator proj = new GlobalMercator();



			string quadDallas =  proj.latlngToQuadKey(32.7805664, -96.8081442, minZoom);
			string quadFtWorth = proj.latlngToQuadKey(32.7586487, -97.3324023, minZoom);
			string quadHouston = proj.latlngToQuadKey(29.758169, -95.3684179, minZoom);
			string quadLosAngeles = proj.latlngToQuadKey(34.0507041, -118.243092, minZoom);
			string quadSanFrancisco = proj.latlngToQuadKey(37.7608648, -122.4153602, minZoom);
			string quadSeattle = proj.latlngToQuadKey(47.6059903, -122.3292994, minZoom);
			string quadChicago = proj.latlngToQuadKey(41.8814234, -87.6292672, minZoom);
			string quadAtlanta = proj.latlngToQuadKey(33.7515908, -84.3858375, minZoom);
			string quadNewYork = proj.latlngToQuadKey(40.7322625, -73.9924373, minZoom);
			string quadBoston = proj.latlngToQuadKey(42.3742784, -71.1165778, minZoom);
			string quadDetroit = proj.latlngToQuadKey(42.3314207, -83.0458919, minZoom);
			string quadAustin = proj.latlngToQuadKey(30.2682476, -97.7417557, minZoom);
			string quadDC = proj.latlngToQuadKey(38.9073708, -77.0363579, minZoom);

			List<string> quads = new List<string>();
			quads.Add(quadDallas);
			quads.Add(quadFtWorth);
			quads.Add(quadNewYork);
			quads.Add(quadHouston);
			quads.Add(quadSanFrancisco);
			quads.Add(quadLosAngeles);
			quads.Add(quadBoston);
			quads.Add(quadAustin);
			quads.Add(quadAtlanta);
			quads.Add(quadChicago);
			quads.Add(quadDC);
			quads.Add(quadSeattle);
			quads.Add(quadDetroit);



			foreach(string quad in quads) {
				process(quad, collection, maxZoom);
			}



		}
		void process(string quadTree, IMongoCollection<BsonDocument> collection, int maxZoom) {
			long result = previousBitmap(quadTree);
			Console.Write(result+",");
			Console.WriteLine(quadTree);

			if(result<0) {
				Task<long> tsk = queryToBitmap(quadTree, collection);
				tsk.Wait();
				result = tsk.Result;
			}
			if(result>0) {
				int zoom = quadTree.Length;

				if(zoom <= maxZoom) {
					string[] childrenTiles = getChildrenTiles(quadTree);
					foreach(string child in childrenTiles) {
						process(child, collection, maxZoom);
					}
				}
			}

		}
		/// <summary>
		/// Tests for previous bitmap.  none == -1, blank == 0, full == 1
		/// </summary>
		/// <param name="quadTree"></param>
		/// <returns></returns>
		int previousBitmap(string quadTree) {

			GlobalMercator projection = new GlobalMercator();
			int[] tileXYZ = projection.quadKeyToTileXY(quadTree);

			// bitmap.Save(@"C:\data\HousePricesDownload\web\images\"+tileXYZ[2]+"/"+tileXYZ[0]+"/"+tileXYZ[1]+".png");
			string pathString = @"C:\data\HousePricesDownload\web\images\"+tileXYZ[2]+"/"+tileXYZ[0]+"/"+tileXYZ[1]+".png";
			bool previous = System.IO.File.Exists(pathString);
			if(!previous) { return -1; }

			bool blank = isBlank(pathString);
			if(blank) {
				return 0;
			} else { return 1; }
		}
		bool isBlank(string pathString) {


			//first get all the bytes
			Bitmap bitmap = new Bitmap(pathString);
			System.Drawing.Imaging.BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

			//int stride = bmData.Stride;
			IntPtr Scan0 = bmData.Scan0;
			int byteCount = Math.Abs(bmData.Stride)* bitmap.Height;
			byte[] rgbValues = new byte[byteCount];
			System.Runtime.InteropServices.Marshal.Copy(Scan0, rgbValues, 0, byteCount);

			//look at all alphas
			for(int i = 0; i < rgbValues.Length; i+=4) {
				if(rgbValues[i]>0) {
					bitmap.UnlockBits(bmData);
					return false;
				}
			}

			bitmap.UnlockBits(bmData);
			return true;

		}
		bool equalsZero(byte b) {
			//Console.WriteLine(b.ToString()+",");
			return b==0;
			//if(b==137 || b==80) { return true; } else { return false; }
		}
		// Converting Bitmap to byte array
		private byte[] convertBitmapToByteArray(Bitmap imageToConvert) {
			System.IO.MemoryStream ms = new System.IO.MemoryStream();
			imageToConvert.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

			return ms.ToArray();
		}
        // string[] quadPlusOne(string quad){
        //     string[] quads = new string[4];
        //     quads[0] = quad + "0";
        //     quads[1] = quad + "1";
        //     quads[2] = quad + "2";
        //     quads[3] = quad + "3";
        //     return quads;
        // }
		static async Task<long> queryToBitmap(string quadTree, IMongoCollection<BsonDocument> collection) {
            
//             // //TODO: query by quadTree
//             // string queryQuad;
//             // bool query =queryQuad.StartsWith(quadTree);
//             // //max zoom = 23
 //MongoDB.Driver.Legacy.dll Version 2.2.0
 
//             var result = UserConnectionHandler.MongoCollection.Find(query);
            
            
			GlobalMercator proj = new GlobalMercator();
			int[] tileXYZ = proj.quadKeyToTileXY(quadTree);
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(256, 256);
			long count = 0;
			int zoom = quadTree.Length;




			//IMongoQueryable<BsonDocument> quadRecords = collection.AsQueryable().Where(b => ( b["quad"].AsString ).StartsWith(quadTree));
             var query = MongoDB.Driver.Builders.Query.Matches("quad", new BsonRegularExpression(string.Format("^{0}", quadTree)));
             var results = collection.FindAs(BsonDocument,query);
            //MongoDB.Driver.Legacy.dll Version 2.2.0
            //MongoDB.Driver.Builders.Query
            
            
			//collection.AsQueryable().Where(b => b["quad"].AsBsonValue);
//             var query =
//     from c in collection.AsQueryable<C>()
//     where c.S.StartsWith("abc")
//     select c;
// // or
// var query =
//     collection.AsQueryable<C>()
//     .Where(c => c.S.StartsWith("abc"));
// This is translated to the following MongoDB query (using regular expressions):

// { S : /^abc/ }
//collection.AsQueryable<BsonDocument>().Where(c => c["quad"])


			foreach(BsonDocument doc in query) {

				if(doc["price"].BsonType!=BsonType.Int32) { continue; }

				int p = (int) doc["price"].AsInt32;
				double latitude =(double) doc["lat"].AsDouble;
				double longitude =(double) doc["lng"].AsDouble;

				Draw.price(p, latitude, longitude, zoom, bitmap);


			}

			//double[] bounds = proj.tileLatLngBounds(tileXYZ[0], tileXYZ[1], tileXYZ[2]);
			//FilterDefinitionBuilder<BsonDocument> builder = Builders<BsonDocument>.Filter;
			//FilterDefinition<BsonDocument> filter;
			//filter = builder.Gt("lat", bounds[0]);
			//filter = filter & builder.Lt("lat", bounds[1]);
			//filter = filter & builder.Gt("lng", bounds[2]);
			//filter = filter & builder.Lt("lng", bounds[3]);
			////filter = filter & builder.Exists("nearMin",true);


			//count =  collection.Find(filter).CountAsync().Result;
			////if(count==0) { return count; }

			////if(count>0) {
			//using(var cursor = await collection.FindAsync(filter)) {
			//	while(await cursor.MoveNextAsync()) {
			//		var batch = cursor.Current;
			//		//int current = 0;

			//		foreach(var doc in batch) {
			//			//current++;

			//			if(doc["price"].BsonType!=BsonType.Int32) { continue; }

			//			int p = (int) doc["price"].AsInt32;
			//			double latitude =(double) doc["lat"].AsDouble;
			//			double longitude =(double) doc["lng"].AsDouble;

			//			Draw.price(p, latitude, longitude, zoom, bitmap);
			//			//Draw.near(doc["price"].AsInt32, doc["nearMin"].AsInt32, doc["nearMax"].AsInt32, doc["lat"].AsDouble, doc["lng"].AsDouble, zoom, bitmap);
			//		}
			//	}
			//}
			////}


			////save bitmap
			string pathString = @"C:\data\HousePricesDownload\web\images\"+tileXYZ[2]+"/"+tileXYZ[0]+"/";
			System.IO.Directory.CreateDirectory(pathString);
			bitmap.Save(@"C:\data\HousePricesDownload\web\images\"+tileXYZ[2]+"/"+tileXYZ[0]+"/"+tileXYZ[1]+".png");

			return count;

		}
		string[] getChildrenTiles(string quadTree) {
			string[] children = new string[4];
			children[0] = quadTree+"0";
			children[1] = quadTree+"1";
			children[2] = quadTree+"2";
			children[3] = quadTree+"3";

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