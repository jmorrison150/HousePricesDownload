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


            HousePricesDownload.MyClass myClassInstance = new MyClass(args[0], int.Parse(args[1]));
			myClassInstance.initialize();
		}
	}

	class MyClass {
        string proxy;
				int port;
		protected static IMongoClient client;
		protected static IMongoDatabase test;
		double sizeMax = 10.0;
		double sizeMin = 0.1;
		double factor = 10;
		//double[] factors = new double[4] {10.0, 1.0, 0.1, 0.01};
        public MyClass(string ipAddress, int _port){
            proxy = ipAddress;
						port = _port;
        }


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
			lngMin = -130.0;
			lngMax = -60.0;

			// //dallas
			// latMin =  32.4;
			// lngMin = -97.6;
			// latMax = 33.1;
			// lngMax =  -96.4;

			// //debug
			// latMin = 30.0;
			// latMax = 30.0;
			// lngMin = -90.0;
			// lngMax = -90.0;




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

			Console.Write("("+lngMin+","+latMin+")");
			int nearbyCount;
			string json = "";

			//test for previous search
			var builder = Builders<BsonDocument>.Filter;
			FilterDefinition<BsonDocument> filter = builder.Eq("lat", latMin) & builder.Eq("lng", lngMin) & builder.Eq("size", size);
			Task<long> previousCount = searchCollection.Find(filter).CountAsync();
			previousCount.Wait();

			//download(json, latMin, lngMin, size, collection, searchCollection);
			if(previousCount.Result==0) {

				//new download
				nearbyCount = download(json, latMin, lngMin, size, collection, searchCollection);
				


			}
            else {
				//use old download
				Task<BsonDocument> task = searchCollection.Find(filter).FirstAsync();
				BsonDocument previousSearch = task.Result;
				nearbyCount = previousSearch["count"].AsInt32;
				Console.WriteLine("previousCount= "+nearbyCount.ToString());

                //retry error
                if(previousSearch["count"].AsInt32 <0){
                    nearbyCount = download(json, latMin, lngMin, size, collection, searchCollection);
                } 
			}


			//recursive at smaller scale
			if(( nearbyCount>=900 ) && ( size > sizeMin )) {
				double size01 = size / factor;
				for(int i = 0; i < factor; i++) {
					for(int j = 0; j < factor; j++) {
						run(( latMin+( size01*i ) ), ( lngMin+( size01*j ) ), size01, collection, searchCollection);
					}
				}
			}


		}
		int download(string json, double latMin, double lngMin, double size, IMongoCollection<BsonDocument> collection, IMongoCollection<BsonDocument> searchCollection) {

            //insertSearch(latMin,lngMin,size,-3,-3,searchCollection);
			
            dynamic data;
			int nearbyCount = -1;
			int numPages;
			try {
                //new download
				json = getJSON(latMin, lngMin, size);
			}catch{
				//connection error
				insertSearch(latMin, lngMin, size, -1, -1, searchCollection);
				Console.Write(json);
				Console.Write(" connection error");
				System.Threading.Thread.Sleep(10000);
				return -1;
			}


				//captcha
				if(json[0]!='{') {
					Console.Write(json);
					Console.Write(" captcha");

					
					System.IO.File.WriteAllText(@"C:\data\captcha.html", json);
					System.Diagnostics.Process.Start(@"C:\data\captcha.html");




					insertSearch(latMin, lngMin, size, -2, -2, searchCollection);
					pause();
					return -2;
				}

			try{
                //log new search
				int error = 0;
                try{
                    data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                try{
                    insertRawJSON(json, collection);
                try{
                    nearbyCount = (int) data.map.nearbyProperties.Count;
                try{
                    numPages = (int) data.list.numPages;
                try{
                    insertSearch(latMin, lngMin, size, nearbyCount,numPages, searchCollection);
                }catch{error=5;}
                }catch{error=4;}
                }catch{error=3;}
                }catch{error=2;}
                }catch{error=1;
                Console.WriteLine("error= "+error+",");
                pause();
                }
				return nearbyCount;
            

				//error
			} catch {
				insertSearch(latMin, lngMin, size, -1,-1, searchCollection);
				
                System.IO.File.WriteAllText(@"C:\data\error"+latMin+"_"+lngMin+"_"+size+".json", json);

                Console.Write(json);
				Console.Write(" unknown error (writing to file)");
				System.Threading.Thread.Sleep(10000);
				return -1;
			}



		}
		void insertRawJSON(string json, IMongoCollection<BsonDocument> collection) {
			MongoDB.Bson.BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
			//            BsonDocument document = json.ToBsonDocument();
			collection.InsertOneAsync(document);

		}
		void insertSearch(double lat, double lng, double size, int count,int numPages, IMongoCollection<BsonDocument> searchCollection) {
			BsonDocument document;
			document = new BsonDocument {
                    
                    {"lat", lat},
                    {"lng", lng},
                    {"size",size},
                    {"count", count},
                    {"numPages",numPages}
                    };

			searchCollection.InsertOneAsync(document);
			Console.Write("document[\"count\"]= "+document["count"]);

		}
		private string getJSON(double lat, double lng, double size) {

			string latMin = string.Format("{0:0}", ( lat )*1000000.0);
			string lngMin = string.Format("{0:0}", ( lng )*1000000.0);
			string lngMax = string.Format("{0:0}", ( ( lng+( size ) )*1000000 ));
			string latMax = string.Format("{0:0}", ( ( lat+( size ) )*1000000 ));
			int page = 1;
            int zoom = 19;

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
                + "&" + "zoom="+zoom
                + "&" + "rect="
                + lngMin
                + "," + latMin
                + "," + lngMax
                + "," + latMax
                + "&" + "p="
                + page.ToString()
                + "&" + "sort=days"
                + "&" + "search=maplist"
                + "&" + "disp=1"
                + "&" + "listright=true"
                + "&" + "isMapSearch=true"
                + "&" + "zoom="+zoom;



			HttpWebRequest webReq = (HttpWebRequest) WebRequest.Create(url);
			webReq.Method = "GET";
			webReq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
			webReq.Proxy = new WebProxy(proxy,port);     
			//webReq.Proxy = System.Net.IWebProxy "127.0.0.1";
            HttpWebResponse webRes = null;
			try {
				webRes  = (HttpWebResponse) webReq.GetResponse();
			} catch {
				Console.WriteLine("error = GetResponse");
			}


			////feedback
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
