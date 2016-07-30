using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Zoltu.BagsAmazonSynchronizer
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Starting.");

			var configuration = new Configuration();
			using (var db = new Models.BagsContext(configuration))
			{
				// this hooks up console logging to the DbContext so we can see the queries being executed in the console output
				var serviceProvider = db.GetInfrastructure<IServiceProvider>();
				var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
				loggerFactory.AddConsole(LogLevel.Information);

				// TODO
				Console.WriteLine(db.Products.First().Name);
			}

			Console.WriteLine("Ending.");
		}
	}
}
