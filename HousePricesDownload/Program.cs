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



        public void initialize() {
            //record time
            DateTime startTime = DateTime.Now;
            string now = startTime.ToString("yyyy.MM.dd HH.mm");
            Console.WriteLine("Started at " + now);



            client = new MongoClient();
            test = client.GetDatabase("test");
            IMongoCollection<BsonDocument> collection = test.GetCollection<BsonDocument>("prop1");

            double latMin = -90.0;
            double latMax = 90.0;
            double lngMin = -180.0;
            double lngMax = 180.0;

            int sleep = 2000;

            //dallas
            latMin =  32.472300;
            lngMin = -97.610412;
            latMax = 33.125659;
            lngMax =  -96.410155;


            //double latMin = -73.984750;
            //double lngMin = 40.777982;
            double size = 0.01;



            for(double currentLng = lngMax; currentLng >=lngMin; currentLng -= size) {
                for(double currentLat = latMax; currentLat >=latMin; currentLat -= size) {
                    //for(double currentLng = lngMin; currentLng <=lngMax; currentLng += size) {
                    //    for(double currentLat = latMin; currentLat <=latMax; currentLat += size) {
                    
                    Console.WriteLine("("+currentLng+","+currentLat+")");
                    run(currentLat, currentLng, size, collection);
                    System.Threading.Thread.Sleep(sleep);
                }
            }
            DateTime endTime = DateTime.Now;
            TimeSpan elapsedTime = endTime-startTime;
            Console.WriteLine("elapsedTime = "+elapsedTime);
            
            pause();
        }
        void run(double latMin, double lngMin, double size, IMongoCollection<BsonDocument> collection) {

            string json = "";
            int page = 1;
            //int numPages = 0;
            dynamic data;


            //while((page-1)<=numPages) {
            try {
                //json = System.IO.File.ReadAllText(@"C:\Data\Dropbox\john\code\HousePricesDownload\HousePricesDownload\obj\nearbyProperties.json");
                json = getJSON(latMin, lngMin, size, page);

                data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                Console.WriteLine(data.map.nearbyProperties.Count);
                Console.Write(data.list.page);
                Console.Write(" of ");
                Console.WriteLine(data.list.numPages);

                //insertDB(data, collection);
                insertRawJSON(json, collection);

                //page = data.list.page +1;
                //numPages = data.list.numPages;
                //}
            } catch {
                Console.Write("0"); return;
            }
        }

        void insertRawJSON(string json, IMongoCollection<BsonDocument> collection) {
            MongoDB.Bson.BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
//            BsonDocument document = json.ToBsonDocument();
            collection.InsertOneAsync(document);
           
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

            //Console.WriteLine(json);
            return json;

        }
        private void insertDB(dynamic datas, IMongoCollection<BsonDocument> collection) {

            Console.WriteLine(datas.map.nearbyProperties.Count);
            for(int i = 0; i < datas.map.nearbyProperties.Count; i++) {
                //    //add document to mongodb

                if(!( datas.map.nearbyProperties[i][7][0]>0 )) {
                    Console.Write(i.ToString()+",");
                    continue;
                }

                bool isVacantLand = true;
                int id = 0, type = 0;
                double lat = 0, lng = 0, price = 0, bed = 0, bath = 0, sf = 0;
                string img = "";

                try {
                    id = (int) datas.map.nearbyProperties[i][0];
                    lat = (double) datas.map.nearbyProperties[i][1]/1000000.0;
                    lng = (double) datas.map.nearbyProperties[i][2]/1000000.0;
                    price = (double) datas.map.nearbyProperties[i][7][0];

                    try {
                        type = (int) datas.map.nearbyProperties[i][4];
                    } catch {
                        //Console.WriteLine("error type");
                    }
                    try {
                        bed = (double) datas.map.nearbyProperties[i][7][1];
                    } catch {
                    }
                    try {
                        bath = (double) datas.map.nearbyProperties[i][7][2];
                    } catch {
                    }
                    try {
                        sf = (double) datas.map.nearbyProperties[i][7][3];
                    } catch {
                    }
                    try {
                        isVacantLand = datas.map.nearbyProperties[i][7][4];
                    } catch {
                    }
                    try {
                        if(datas.map.nearbyProperties[i][7][5]!= null) {
                            img = (string) datas.map.nearbyProperties[i][7][5];
                        }
                    } catch {
                    }

                } catch {
                    //Console.Write("error on "+i.ToString());
                }


                if(!( price>0 )) {
                    Console.WriteLine("price==0");
                    continue;
                }

                BsonDocument document;
                document = new BsonDocument {
                    
                    {"_id", id},
                    {"lat", lat},
                    {"lng", lng},
                    {"price",price},
                    {"bed", bed},
                    {"bath", bath},
                    {"sf", sf},
                    {"type",type},
                    {"img",img}
                    };


                collection.InsertOneAsync(document);

            }

        }
        private string url(double lat, double lng, double size, int page) {
            string request = "http://www.zillow.com/search/GetResults.htm"
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
                + "&" + "zoom=18"
                + "&" + "rect="
                + ( ( lat-( size*0.5 ) )*1000000.0 ).ToString()
                + "," + ( ( lng-( size*0.5 ) )*1000000 ).ToString()
                + "," + ( ( lat+( size*0.5 ) )*1000000 ).ToString()
                + "," + ( ( lng+( size*0.5 ) )*1000000 ).ToString()
                + "&" + "p="
                + page.ToString()
                + "&" + "sort=days"
                + "&" + "search=maplist"
                + "&" + "disp=1"
                + "&" + "listright=true"
                + "&" + "isMapSearch=true"
                + "&" + "zoom=18";

            //Console.WriteLine(request);
            return request;
        }
        private void pause() {
            Console.WriteLine("Press Enter to Continue...");
            Console.ReadKey(false);
        }


    }

}
