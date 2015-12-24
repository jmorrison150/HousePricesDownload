using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;


namespace HousePricesDownload {
	class Program {
		static void Main(string[] args) {
			HousePricesDownload.MyClass myClassInstance = new MyClass();
			myClassInstance.initialize();
		}
	}

	class MyClass {
		protected static IMongoClient client;
		protected static IMongoDatabase test;
		double sizeMax = 10.0;
		double sizeMin = 0.01;
		double factor = 10;
		//double[] factors = new double[] {10.0, 1.0, 0.1, 0.01};



		public void initialize() {
			//record time
			DateTime startTime = DateTime.Now;
			string now = startTime.ToString("yyyy.MM.dd HH.mm");
			Console.WriteLine("Started at " + now);



			client = new MongoClient();
			test = client.GetDatabase("test");
			IMongoCollection<BsonDocument> collection = test.GetCollection<BsonDocument>("prop1");
			IMongoCollection<BsonDocument> searchCollection = test.GetCollection<BsonDocument>("search");

			double latMin, latMax, lngMin, lngMax;

			////world
			//latMin = -80.0;
			//latMax = 80.0;
			//lngMin = -180.0;
			//lngMax = 180.0;

			//usa
			latMin = 20.0;
			latMax = 50.0;
			lngMin = -110.0;
			lngMax = -60.0;

			////dallas
			//latMin =  32.472300;
			//lngMin = -97.610412;
			//latMax = 33.125659;
			//lngMax =  -96.410155;


			for(double currentLng = lngMax; currentLng >=lngMin; currentLng -= sizeMax) {
				for(double currentLat = latMax; currentLat >=latMin; currentLat -= sizeMax) {
					run(currentLat, currentLng, sizeMax, collection, searchCollection);
				}
			}

			DateTime endTime = DateTime.Now;
			TimeSpan elapsedTime = endTime-startTime;
			Console.WriteLine("elapsedTime = "+elapsedTime);

			pause();
		}
		void run(double latMin, double lngMin, double size, IMongoCollection<BsonDocument> collection, IMongoCollection<BsonDocument> searchCollection) {

			Console.WriteLine("("+lngMin+","+latMin+")");
			int nearbyCount;
			string json = "";
			int page = 1;
			dynamic data;

			//test for previous search
			var builder = Builders<BsonDocument>.Filter;
			FilterDefinition<BsonDocument> filter = builder.Eq("lat", latMin) & builder.Eq("lng", lngMin) & builder.Eq("size", size);
			Task<long> tsk = searchCollection.Find(filter).CountAsync();
			tsk.Wait();
			if(tsk.Result==0) {

				//new download
				try {
					json = getJSON(latMin, lngMin, size, page);
					data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
					insertRawJSON(json, collection);
					nearbyCount = data.map.nearbyProperties.Count;
					Console.WriteLine(nearbyCount+ data.list.page + " of "+ data.list.numPages);
					insertSearch(latMin, lngMin, size, nearbyCount, searchCollection);
				} catch { Console.Write("0"); return; }



				//use old download
			} else {
				BsonDocument previousSearch = searchCollection.Find(filter).FirstAsync().ToBsonDocument();
				nearbyCount = previousSearch["count"].AsInt32;
			}


			//recursive at smaller scale
			if(( nearbyCount>800 ) && ( size > sizeMin )) {
				double size01 = size / factor;
				for(int i = 0; i < factor; i++) {
					for(int j = 0; j < factor; j++) {
						run(( latMin+( size01*i ) ), ( lngMin+( size01*j ) ), size01, collection, searchCollection);
					}
				}

			}

		}
		void insertRawJSON(string json, IMongoCollection<BsonDocument> collection) {
			MongoDB.Bson.BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
			//            BsonDocument document = json.ToBsonDocument();
			collection.InsertOneAsync(document);

		}
		void insertSearch(double lat, double lng, double size, int count, IMongoCollection<BsonDocument> searchCollection) {

			BsonDocument document;
			document = new BsonDocument {
                    
                    {"lat", lat},
                    {"lng", lng},
                    {"size",size},
                    {"count", count}
                    };

			searchCollection.InsertOneAsync(document);






		}
		private string getJSON(double lat, double lng, double size, int page) {

			string latMin = string.Format("{0:0}", ( lat )*1000000.0);
			string lngMin = string.Format("{0:0}", ( lng )*1000000.0);
			string lngMax = string.Format("{0:0}", ( ( lng+( size ) )*1000000 ));
			string latMax = string.Format("{0:0}", ( ( lat+( size ) )*1000000 ));

			//string jmorrisonco = "http://www.jmorrison.co";
			string url = "http://www.zillow.com/search/GetResults.htm"
                + "?" + "spt=homes"
                + "&" + "status=110011"
                + "&" + "lt=111101"
                + "&" + "ht=111111"
                + "&" + "pr=,"
                + "&" + "mp=,"
                + "&" + "bd=0%2C"
                + "&" + "ba=0%2C"
                + "&" + "sf=,"
                + "&" + "lot=,"
                + "&" + "yr=,"
                + "&" + "pho=0"
                + "&" + "pets=0"
                + "&" + "parking=0"
                + "&" + "laundry=0"
                + "&" + "pnd=0"
                + "&" + "red=0"
                + "&" + "zso=0"
                + "&" + "days=any"
                + "&" + "ds=all"
                + "&" + "pmf=1"
                + "&" + "pf=1"
                + "&" + "zoom=19"
                + "&" + "rect="
                + lngMin
                + "," +latMin
                + "," + lngMax
                + "," + latMax
                + "&" + "p="
                + page.ToString()
                + "&" + "sort=days"
                + "&" + "search=maplist"
                + "&" + "disp=1"
                + "&" + "listright=true"
                + "&" + "isMapSearch=true"
                + "&" + "zoom=19";




			HttpWebRequest webReq = (HttpWebRequest) WebRequest.Create(url);
			webReq.Method = "GET";
			HttpWebResponse webRes = null;
			try {
				webRes  = (HttpWebResponse) webReq.GetResponse();
			} catch {
				Console.WriteLine("error = GetResponse");
			}
			//feedback
			//Console.WriteLine(webRes.StatusCode);
			//Console.WriteLine(webRes.Server);

			Stream answer = webRes.GetResponseStream();
			StreamReader _answer = new StreamReader(answer);
			string json = _answer.ReadToEnd();

			System.Threading.Thread.Sleep(2000);
			return json;

		}
		private void pause() {
			Console.WriteLine("Press Enter to Continue...");
			Console.ReadKey(false);
		}
	}
}
