using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.ApplicationServer.Caching;
using ServiceStack.Logging;
using InnerClient = Microsoft.ApplicationServer.Caching;

namespace ServiceStack.CacheAccess.Appfabric
{
	/// <summary>
	/// A velocity implementation of the ServiceStack ICacheClient interface.
	/// Good practice not to have dependencies on implementations in your business logic.
	/// 
	/// Basically delegates all calls to Microsoft.ApplicationServer.Caching.DataCache with added diagnostics and logging.
	/// </summary>
    /// 
    public partial class AppfabricClient : AdapterBase, ICacheClient {

        protected override ILog Log { get { return LogManager.GetLogger(GetType()); } }

		private InnerClient.DataCache cache;
        private InnerClient.DataCacheFactory factory;

		public AppfabricClient(IEnumerable<string> hosts)
		{
			const int defaultPort = 22233;
			const int ipAddressIndex = 0;
			const int portIndex = 1;

			var endpoints = new List<DataCacheServerEndpoint>();
			foreach (var host in hosts)
			{
				var hostParts = host.Split(':');
				if (hostParts.Length == 0)
					throw new ArgumentException("'{0}' is not a valid host name or IP Address: e.g. '127.0.0.0[:11211]'");

				var port = (hostParts.Length == 1) ? defaultPort : int.Parse(hostParts[portIndex]);
                endpoints.Add(new DataCacheServerEndpoint(hostParts[ipAddressIndex], port));
			}
			LoadClient(endpoints);
		}

		public AppfabricClient(IEnumerable<IPEndPoint> ipEndpoints)
		{
            var endpoints = new List<DataCacheServerEndpoint>();
            foreach (var ip in ipEndpoints)
            {
                endpoints.Add(new DataCacheServerEndpoint(ip.Address.ToString(), ip.Port));
            }
			LoadClient(endpoints);
		}

		private void LoadClient(List<DataCacheServerEndpoint> endpoints)
		{
			var factoryConfig = new DataCacheFactoryConfiguration();
            factoryConfig.Servers = endpoints;

            factoryConfig.ChannelOpenTimeout = new TimeSpan(0, 0, 10);
            factoryConfig.RequestTimeout = new TimeSpan(0, 2, 0);

			DataCacheFactory mycacheFactory = new DataCacheFactory(factoryConfig);
            this.cache = mycacheFactory.GetDefaultCache();
		}

        public AppfabricClient(InnerClient.DataCacheFactory factory)
		{
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
			}
            this.factory = factory;
            this.cache = factory.GetDefaultCache();
		}

        public AppfabricClient(InnerClient.DataCacheFactory factory, string cacheName)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }
            this.factory = factory;
            this.cache = factory.GetCache(cacheName);
        }

        public void Dispose()
        {
            Execute(() => factory.Dispose());
        }

        public bool Contains(string key)
        {
            return Execute(() => cache.Get(key)) != null;
        }

        public long LockedOffset(string key, int offset)
        {
            DataCacheLockHandle handle = null;
            long value = 0;
            try
            {
                value = (long) Execute(() => cache.GetAndLock(key, new TimeSpan(0, 0, 5), out handle));
            }
            catch (Exception)
            {
                if (handle != null)
                {
                    Execute(() => cache.Unlock(key, handle));
                }
                throw;
            }
            value += offset;
            try
            {
                Execute(() => cache.PutAndUnlock(key, value, handle));
            }
            catch (Exception)
            {
                if (handle != null)
                {
                    Execute(() => cache.Unlock(key, handle));
                }
                throw;
            }
            return value;
        }

        #region ICacheClient Members

        public bool Add<T>(string key, T value) {
            try
            {
                Execute(() => cache.Add(key, value));
                return true;
            }
            catch (DataCacheException ex)
            {
                if (ex.ErrorCode == DataCacheErrorCode.KeyAlreadyExists
                    || ex.ErrorCode == DataCacheErrorCode.CacheItemVersionMismatch)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            return Add(key, value, expiresAt - DateTime.Now);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            try
            {
                Execute(() => cache.Add(key, value, expiresIn));
                return true;
            }
            catch (DataCacheException ex)
            {
                if (ex.ErrorCode == DataCacheErrorCode.KeyAlreadyExists
                    || ex.ErrorCode == DataCacheErrorCode.CacheItemVersionMismatch)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        public long Decrement(string key, uint amount)
        {
            return LockedOffset(key, (-1 * Convert.ToInt32(amount)));
        }

        public void FlushAll()
        {
            foreach (string regionName in cache.GetSystemRegions())
            {     
                Execute(() => cache.ClearRegion(regionName));
            } 
        }

        public T Get<T>(string key)
        {
            return (T) Execute(() => cache.Get(key));
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            var results = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var result = this.Get<T>(key);
                results[key] = result;
            }

            return results;
        }

        public long Increment(string key, uint amount)
        {
            return LockedOffset(key, Convert.ToInt32(amount));
        }

        public bool Remove(string key)
        {
            return Execute(() => cache.Remove(key));
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                try
                {
                    this.Remove(key);
                }
                catch (Exception ex)
                {
                    Log.Error(string.Format("Error trying to remove {0} from appfabric cache", key), ex);
                }
            }
        }

        public bool Replace<T>(string key, T value)
        {
            if (!Contains(key))
            {
                return false;
            }

            return Set(key, value);
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            if (!Contains(key))
            {
                return false;
            }

            return Set(key, value, expiresAt);
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            if (!Contains(key))
            {
                return false;
            }

            return Set(key, value, expiresIn);
        }

        public bool Set<T>(string key, T value)
        {
            try
            {
                Execute(() => cache.Put(key, value));
                return true;
            }
            catch (DataCacheException ex)
            {
                if (ex.ErrorCode == DataCacheErrorCode.CacheItemVersionMismatch)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            return Set(key, value, expiresAt - DateTime.Now);
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            try
            {
                Execute(() => cache.Put(key, value, expiresIn));
                return true;
            }
            catch (DataCacheException ex)
            {
                if (ex.ErrorCode == DataCacheErrorCode.CacheItemVersionMismatch)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            foreach (var entry in values)
            {
                Set(entry.Key, entry.Value);
            }
        }

        #endregion
    }
}