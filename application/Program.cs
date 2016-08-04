using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using application.Amazon;
using application.Data;
using application.Log;
using application.Synchronization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace application
{
	public class Program
	{
		public static void Main(string[] args)
		{
            //string input = "";
            using (var sync = new SyncManager(new Configuration()))
            {
                sync.Add<AmazonSynchronizer>(
                                                TimeSpan.FromMinutes(1),
                                                (obj) => false,//input.Equals("stop", StringComparison.OrdinalIgnoreCase)
                                                new ConsoleLogger()
                                            );

                //Add more synchronizers here if you want before starting all
                //if you use the console logger for more than one synchronizer, the console will get messy

                //new CancellationTokenSource(1 * 60 * 1000).Token
                sync.StartAll(CancellationToken.None);
            }

            Console.WriteLine("Sync Server Stopped.Press any key to exit ...");
		    Console.ReadKey();
		}
	}
}
