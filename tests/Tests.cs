using System;
using Xunit;

namespace Zoltu.BagsAmazonSynchronizer.Tests
{
	public class Tests
	{
		[Fact]
		public void main_does_not_throw()
		{
			Program.Main(new String[]{});
			Assert.True(true);
		}
	}
}
