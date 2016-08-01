using System;
using System.Linq;
using System.Xml.Linq;
using application.Amazon;
using application.Data;
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
			Console.WriteLine("Starting.");

            using (var sync = new SyncManager(new Configuration()))
            {
                sync.WithInterval(TimeSpan.FromMinutes(15))
                    .Start();
            }

			Console.WriteLine("Ending.");
		    Console.ReadKey();
		}
	}
}
