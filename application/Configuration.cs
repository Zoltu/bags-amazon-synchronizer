using System;
using Microsoft.Extensions.Configuration;

namespace application
{
	public class Configuration
	{
		public readonly String AmazonAssociateTag;
		public readonly String AmazonAccessKey;
		public readonly String AmazonSecretKey;
		public readonly String SqlServerConnectionString;

		public Configuration()
		{
			var configuration = new ConfigurationBuilder()
			.AddEnvironmentVariables()
			.AddUserSecrets("bags-amazon-synchronization")
			.Build();

			AmazonAssociateTag = configuration["AmazonAssociateTag"];
			if (AmazonAssociateTag == null)
				throw new Exception("Required configuration option AmazonAssociateTag not found.");

			AmazonAccessKey = configuration["AmazonAccessKeyId"];
			if (AmazonAccessKey == null)
				throw new Exception("Required configuration option AmazonAccessKeyId not found.");

			AmazonSecretKey = configuration["AmazonSecretAccessKey"];
			if (AmazonSecretKey == null)
				throw new Exception("Required configuration option AmazonSecretAccessKey not found.");

			SqlServerConnectionString = configuration["SqlServerConnectionString"];
			if (SqlServerConnectionString == null)
				throw new Exception("Required configuration option SqlServerConnectionString not found.");
		}
	}
}
