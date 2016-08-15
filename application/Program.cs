using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using application.Logger;
using application.Synchronization;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace application
{
	public class Program
	{
		public static void Main(string[] args)
		{
            var config = new Configuration();
            var telemetry = new TelemetryClient();
		    telemetry.InstrumentationKey = config.InstrumentationKey;
		    telemetry.Context.Device.Id = Environment.MachineName;
            telemetry.Context.Session.Id = Guid.NewGuid().ToString();
		    telemetry.Context.Location.Ip = Dns.GetHostEntryAsync(Dns.GetHostName())
		                                        .Result
		                                        .AddressList
		                                        .FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork)
		                                        .ToString();

            using (var amz = new AmazonSynchronizer(config, telemetry))
		    {
                amz.WithInterval(TimeSpan.FromSeconds(1))
                    .StopWhen((obj) => false)
                    .Start(CancellationToken.None)
                    .Wait();
            }

            if (telemetry != null)
                telemetry.Flush();

            Console.WriteLine("Sync Server Stopped.Press any key to exit ...");
            Console.ReadKey();
		}
	}
}
