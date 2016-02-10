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
		//string[] proxies;
		//int[] ports;
		Search[] searches;
		protected static IMongoClient client;
		protected static IMongoDatabase test;
		double sizeMax = 10.0;
		double sizeMin = 0.01;
		double factor = 10;
		double latMin, latMax, lngMin, lngMax;
		//double[] factors = new double[5] {10.0, 1.0, 0.1, 0.01, 0.001};


		public MyClass() {
			//getProxies(out proxies, out ports);
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

			FilterDefinition<BsonDocument> emptyFilter = new BsonDocument();
			Task<List<BsonDocument>> search = searchCollection.Find(emptyFilter).ToListAsync();
			search.Wait();
			searches = new Search[search.Result.Count()];
			List<Search> s= new List<Search>();
			for(int i = 0; i < searches.Length; i++) {
				try {
					s.Add(new Search(search.Result[i]));
				} catch {
					Console.Write(search.Result[i].ToJson());
				}
			}
			searches = s.ToArray();

			//Array.Sort(searches, delegate(Search search1, Search search2) {
			//	return search1.latMin.CompareTo(search2.latMin);
			//});
			//Array.Sort(searches, delegate(Search search1, Search search2) {
			//	return search1.lngMin.CompareTo(search2.lngMin); 
			//});
			//Array.Sort(searches, delegate(Search search1, Search search2) {
			//							return search1.size.CompareTo(search2.size);
			//						});

			//Console.Write(search.Status+",");
			//Console.Write(search.Result.Count()+",");
			//Console.Write(search.ToString());



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


			////midwest
			//latMin = 20.0;
			//latMax = 50.0;
			//lngMin = -100.0;
			//lngMax = -90.0;

			//			//midwest2
			//latMin = 30.0;
			//latMax = 50.0;
			//lngMin = -100.0;
			//lngMax = -90.0;

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


			// //south
			// latMin = 30.0;
			// latMax = 50.0;
			// lngMin = -130.0;
			// lngMax = -60.0;

			// sizeMin = 1.0;
			// east(collection, searchCollection);

			// sizeMin = 0.1;
			// east(collection, searchCollection);

			sizeMin = 0.001;
			southwest(collection, searchCollection);

			//sizeMin = 0.001;
			//east(collection, searchCollection);


			////start west
			//for(double currentLng = lngMin; currentLng <=lngMax; currentLng += sizeMax) {
			//	for(double currentLat = latMin; currentLat <=latMax; currentLat += sizeMax) {
			//		run(currentLat, currentLng, sizeMax, collection, searchCollection);
			//	}
			//}




			DateTime endTime = DateTime.Now;
			TimeSpan elapsedTime = endTime-startTime;
			Console.WriteLine("elapsedTime = "+elapsedTime);

			pause();
		}
		void run(double latMin, double lngMin, double size, IMongoCollection<BsonDocument> collection, IMongoCollection<BsonDocument> searchCollection) {

			//Console.Write("("+latMin+","+lngMin+")");
			int nearbyCount;
			string json = "";

			//test for previous search
			//			Task<long> previousCount = searchCollection.Find(filter).CountAsync();
			//			previousCount.Wait();
			//Console.Write(previousCount.Result);
			Search s = previousSearch(latMin, lngMin, size);




			//new download
			if(s.count<0 || s == null) {
				var builder = Builders<BsonDocument>.Filter;
				FilterDefinition<BsonDocument> filter = builder.Eq("size", size) & builder.Eq("lng", lngMin) & builder.Eq("lat", latMin);
				FilterDefinition<BsonDocument> delete = filter & builder.Lt("count", 0);
				searchCollection.DeleteManyAsync(delete);


				nearbyCount = download(json, latMin, lngMin, size, collection, searchCollection);

			} else {
				nearbyCount = s.count;
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
			//Console.Write("downloading");
            Console.Write("("+latMin+","+lngMin+")");
			//insertSearch(latMin, lngMin, size, -1, -1, searchCollection);

			dynamic data;
			int nearbyCount = -2;
			int numPages;
			try {
				//new download
				json = getJSON(latMin, lngMin, size);
			} catch {
				//connection error
				insertSearch(latMin, lngMin, size, -2, -2, searchCollection);
				Console.Write(json);
				Console.Write(" connection error");
				System.Threading.Thread.Sleep(10000);
				return -2;
			}


			//captcha
			if(json[0]!='{') {
				Console.Write(json);
				Console.Write(" captcha");


				System.IO.File.WriteAllText(@"C:\data\captcha.html", json);
				System.Diagnostics.Process.Start(@"C:\data\captcha.html");




				insertSearch(latMin, lngMin, size, -3, -3, searchCollection);
				pause();
				return -3;
			}

			try {
				//log new search
				int error = 0;
				try {
					data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
					try {
						insertRawJSON(json, collection);
						try {
							nearbyCount = (int) data.map.nearbyProperties.Count;
							try {
								numPages = (int) data.list.numPages;
								try {
									insertSearch(latMin, lngMin, size, nearbyCount, numPages, searchCollection);
								} catch { error=5; Console.WriteLine("error= "+error+","); }
							} catch { error=4; Console.WriteLine("error= "+error+","); }
						} catch { error=3; Console.WriteLine("error= "+error+","); }
					} catch { error=2; Console.WriteLine("error= "+error+","); }
				} catch {
					error=1;
					insertSearch(latMin, lngMin, size, -4, -4, searchCollection);
					Console.WriteLine("error= "+error+",");
					pause();
				}

				return nearbyCount;


				//error
			} catch {
				insertSearch(latMin, lngMin, size, -2, -2, searchCollection);

				System.IO.File.WriteAllText(@"C:\data\error"+latMin+"_"+lngMin+"_"+size+".json", json);

				Console.Write(json);
				Console.Write(" unknown error (writing to file)");
				System.Threading.Thread.Sleep(10000);
				return -2;
			}



		}

		void getProxies(out string[] proxies, out int[] ports) {
			//string[] args = System.IO.File.ReadAllLines(@"C:\data\proxies.tsv");

			StreamReader reader = new StreamReader(File.OpenRead(@"C:\data\HousePricesDownload\proxies.tsv"));
			List<string> listA = new List<string>();
			List<int> listB = new List<int>();
			while(!reader.EndOfStream) {
				string line = reader.ReadLine();
				string[] values = line.Split('\t');

				listA.Add(values[0]);
				listB.Add(int.Parse(values[1]));

				Console.Write(values[1]+","+values[0]+",");
			}

			proxies = listA.ToArray();
			ports = listB.ToArray();


			//proxies = new string[(int) ( ( args.Length-1 )*0.5 )+1];
			//ports = new int[(int) ( ( args.Length-1 )*0.5 )+1];
			//for(int i=0; i<args.Length-1; i+=2) {
			//	int index = (int) ( i*0.5 );

			//	proxies[index] = args[i];
			//	ports[index] = int.Parse(args[i+1]);
			//}
			//System.IO.File.WriteAllText(@"C:\data\captcha.html", json);
			//System.Diagnostics.Process.Start(@"C:\data\captcha.html");

		}
		void insertRawJSON(string json, IMongoCollection<BsonDocument> collection) {
			MongoDB.Bson.BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
			//            BsonDocument document = json.ToBsonDocument();
			collection.InsertOneAsync(document);

		}
		void insertSearch(double lat, double lng, double size, int count, int numPages, IMongoCollection<BsonDocument> searchCollection) {
			BsonDocument document;
			document = new BsonDocument {
                    
				//{"_id",lng.ToString()+"_"+lat.ToString()+"_"+size.ToString()},
				{"lat", lat},
				{"lng", lng},
				{"size",size},
				{"count", count},
				{"numPages",numPages}
			};
			FilterDefinition<BsonDocument> index = new BsonDocument{
				{"lat", lat},
				{"lng", lng},
				{"size",size}
			};

			//searchCollection.ReplaceOneAsync(index, document);
			searchCollection.InsertOneAsync(document);
			//Console.Write("document[\"count\"]= "+document["count"]);
			Console.WriteLine(document["count"]);

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
			//webReq.Proxy = new WebProxy(proxies[0], ports[0]);
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
		void east(IMongoCollection<BsonDocument> collection, IMongoCollection<BsonDocument> searchCollection) {

			//start east
			for(double currentLng = lngMax; currentLng >=lngMin; currentLng -= sizeMax) {
				for(double currentLat = latMax; currentLat >=latMin; currentLat -= sizeMax) {
					run(currentLat, currentLng, sizeMax, collection, searchCollection);
				}
			}


		}
        
        void west(IMongoCollection<BsonDocument> collection, IMongoCollection<BsonDocument> searchCollection) {
            //start west
            for(double currentLng = lngMin; currentLng <=lngMax; currentLng += sizeMax) {
                for(double currentLat = latMin; currentLat <=latMax; currentLat += sizeMax) {
                    run(currentLat, currentLng, sizeMax, collection, searchCollection);
                }
            }
		}

        void southwest(IMongoCollection<BsonDocument> collection, IMongoCollection<BsonDocument> searchCollection) {
            //start south
            for(double currentLat = latMin; currentLat <=latMax; currentLat += sizeMax) {
                for(double currentLng = lngMin; currentLng <=lngMax; currentLng += sizeMax) {
                    Console.Write("("+latMin+","+lngMin+")");
                    run(currentLat, currentLng, sizeMax, collection, searchCollection);
                }
            }
		}


        
 
		Search previousSearch(double lat, double lng, double size) {
			Search s = searches.FirstOrDefault(search => ( ( search.latMin == lat ) && ( search.lngMin == lng ) && ( search.size == size ) ));
			if(s==null) {
				return new Search(lat, lng, size, -1);
			}
			return s;

		}
		private void pause() {
			Console.WriteLine("Press Enter to Continue...");
			Console.ReadKey(false);
		}
	}

	class Search {
		public double latMin;
		public double lngMin;
		public double size;
		public int count;

		public Search(double _lat, double _lng, double _size, int _count) {
			this.latMin= _lat;
			this.lngMin = _lng;
			this.size = _size;
			this.count = _count;

		}
		public Search(BsonDocument search) {
			this.latMin = search["lat"].AsDouble;
			this.lngMin = search["lng"].AsDouble;
			this.size = search["size"].AsDouble;
			this.count = search["count"].AsInt32;
		}

		public int CompareTo(Search other) {

			int c = 0;

			c = this.size.CompareTo(other.size);
			if(c==0) { c = this.lngMin.CompareTo(other.lngMin); }
			if(c==0) { c = this.latMin.CompareTo(other.latMin); }
			return c;

		}

	}
}
