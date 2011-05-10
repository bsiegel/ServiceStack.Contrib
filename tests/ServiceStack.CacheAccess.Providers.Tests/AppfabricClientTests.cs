using System;
using NUnit.Framework;
using ServiceStack.CacheAccess.Appfabric;
using ServiceStack.Configuration;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[Ignore("Integration test which requires Appfabric installed")]
	[TestFixture]
	public class AppfabricClientTests : CacheClientTestBase
	{
		[TestFixtureSetUp]
		protected void SetUp()
		{
			var appfabricServers = ConfigUtils.GetListFromAppSetting("AppfabricServers");
            this.cacheClient = new AppfabricClient(appfabricServers);
		}

		[Ignore("Debug output only, not really a test")][Test]
		public void AppfabricCache_test_everything()
		{
			const string cacheKey = "testEvery";

            TestEverySet(cacheKey);
		}

		[Test]
        public void AppfabricCache_CacheAdd()
		{
			const string cacheKey = "testCacheKey";

			CacheAdd(cacheKey);
		}

		[Test]
        public void AppfabricCache_CacheSet()
		{
			var cacheKey = Guid.NewGuid().ToString();

			CacheSet(cacheKey);
		}
	}
}