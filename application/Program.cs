using System;
using System.Threading;
using application.Logger;
using application.Synchronization;

namespace application
{
	public class Program
	{
		public static void Main(string[] args)
		{

		    using (var amz = new AmazonSynchronizer(new Configuration()))
		    {
                amz.WithInterval(TimeSpan.FromSeconds(1))
                    .StopWhen((obj) => false)
                    .SetLogger(new ConsoleLogger())
                    .Start(CancellationToken.None)
                    .Wait();
            }

            Console.WriteLine("Sync Server Stopped.Press any key to exit ...");
		    Console.ReadKey();
		}
	}
}
