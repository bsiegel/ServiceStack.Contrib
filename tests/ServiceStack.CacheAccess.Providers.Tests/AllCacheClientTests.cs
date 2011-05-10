using NUnit.Framework;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.CacheAccess.Memcached;
using ServiceStack.Redis;
using ServiceStack.CacheAccess.Appfabric;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[TestFixture]
	[Ignore("Ignoring integration tests that require infracture")]
	public class AllCacheClientTests : AllCacheClientsTestBase
	{
		[Test]
		public void Memory_GetAll_returns_missing_keys()
		{
			AssertGetAll(new MemoryCacheClient());
		}

		[Test]
		public void Redis_GetAll_returns_missing_keys()
		{
			AssertGetAll(new RedisClient(TestConfig.SingleHost));
		}

		[Test]
		public void Memcached_GetAll_returns_missing_keys()
		{
			AssertGetAll(new MemcachedClientCache(TestConfig.MasterHosts));
		}

        [Test]
        public void Appfabric_GetAll_returns_missing_keys() {
            AssertGetAll(new AppfabricClient(TestConfig.MasterHosts));
        }

		[Test]
		public void Memory_GetSetIntValue_returns_missing_keys()
		{
			AssertGetSetIntValue(new MemoryCacheClient());
		}

		[Test]
		public void Redis_GetSetIntValue_returns_missing_keys()
		{
			AssertGetSetIntValue(new RedisClient(TestConfig.SingleHost));
		}

		[Test]
		public void Memcached_GetSetIntValue_returns_missing_keys()
		{
			var client = new MemcachedClientCache(TestConfig.MasterHosts);
			AssertGetSetIntValue((IMemcachedClient)client);
			AssertGetSetIntValue((ICacheClient)client);
		}

        [Test]
        public void Appfabric_GetSetIntValue_returns_missing_keys() {
            var client = new AppfabricClient(TestConfig.MasterHosts);
            AssertGetSetIntValue((ICacheClient) client);
        }
	}
}