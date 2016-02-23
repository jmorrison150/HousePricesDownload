using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Data {
	class Program {
		static void Main(string[] args) {


			DateTime startTime = DateTime.Now;
			string now = startTime.ToString("yyyy.MM.dd HH.mm");
			Console.WriteLine("Started at " + now);

			Data.analyzeData myClassInstance = new analyzeData();
			myClassInstance.run();


			DateTime endTime = DateTime.Now;
			TimeSpan elapsedTime = endTime-startTime;
			Console.WriteLine("elapsedTime = "+elapsedTime);

			Console.WriteLine("Press Enter to Continue...");
			Console.ReadKey(false);



		}
	}
}
