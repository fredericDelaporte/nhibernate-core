using System;
using System.Collections.Generic;
using NHibernate.Cfg;

namespace NHibernate.Cache
{
	/// <summary>
	/// Defines a factory for query cache instances.  These factories are responsible for
	/// creating individual QueryCache instances.
	/// </summary>
	public interface IQueryCacheFactory
	{
		// Since v5.2
		[Obsolete("Please use extension overload with an ICache parameter.")]
		IQueryCache GetQueryCache(
			string regionName,
			UpdateTimestampsCache updateTimestampsCache,
			Settings settings,
			IDictionary<string, string> props);
	}

	// 6.0 TODO: move to interface and purge parameters usefull only for building the regionCache.
	// (Only updateTimestampsCache and regionCache must then remain. Leaving props too for allowing custom factories
	// to have configuration parameters.)
	public static class QueryCacheFactoryExtension
	{
		/// <summary>
		/// Build a query cache.
		/// </summary>
		/// <param name="factory">The query cache factory.</param>
		/// <param name="regionName">The cache region.</param>
		/// <param name="updateTimestampsCache">The cache of updates timestamps.</param>
		/// <param name="settings">The NHibernate settings.</param>
		/// <param name="props">The NHibernate settings properties.</param>
		/// <param name="regionCache">The <see cref="ICache" /> to use for the region.</param>
		/// <returns>A query cache.</returns>
		public static IQueryCache GetQueryCache(
			this IQueryCacheFactory factory,
			string regionName,
			UpdateTimestampsCache updateTimestampsCache,
			Settings settings,
			IDictionary<string, string> props,
			ICache regionCache)
		{
			if (factory is StandardQueryCacheFactory standardFactory)
			{
				return standardFactory.GetQueryCache(updateTimestampsCache, props, regionCache);
			}
#pragma warning disable 618
			return factory.GetQueryCache(regionName, updateTimestampsCache, settings, props);
#pragma warning restore 618
		}
	}
}
