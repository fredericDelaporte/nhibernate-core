using System;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Mapping;
using System.Collections.Generic;
using NHibernate.Connection;

namespace NHibernate.Tool.hbm2ddl
{
	// Candidate to be exstensions of ISessionFactory and Configuration
	public static class SchemaMetadataUpdater
	{
		public static void Update(ISessionFactoryImplementor sessionFactory)
		{
			var reservedWords = GetReservedWords(sessionFactory.ConnectionProvider, sessionFactory.Dialect);
			sessionFactory.Dialect.Keywords.UnionWith(reservedWords);
		}

		public static void Update(Configuration configuration, Dialect.Dialect dialect)
		{
			dialect.Keywords.UnionWith(GetReservedWords(configuration, dialect));
		}

		[Obsolete("Use the overload that passes dialect so keywords will be updated and persisted before auto-quoting")]
		public static void QuoteTableAndColumns(Configuration configuration)
		{
			// Instantiates a new instance of the dialect so doesn't benefit from the Update call.
			var dialect = Dialect.Dialect.GetDialect(configuration.GetDerivedProperties());
			Update(configuration, dialect);
			QuoteTableAndColumns(configuration, dialect);
		}

		public static void QuoteTableAndColumns(Configuration configuration, Dialect.Dialect dialect)
		{
			ISet<string> reservedDb = dialect.Keywords;

			foreach (var cm in configuration.ClassMappings)
			{
				QuoteTable(cm.Table, reservedDb);
			}
			foreach (var cm in configuration.CollectionMappings)
			{
				QuoteTable(cm.Table, reservedDb);
			}
		}

		private static ISet<string> GetReservedWords(Configuration configuration, Dialect.Dialect dialect)
		{
			IConnectionHelper connectionHelper = new ManagedProviderConnectionHelper(configuration.GetDerivedProperties());
			connectionHelper.Prepare();
			try
			{
				return GetReservedWords(dialect, connectionHelper);
			}
			finally
			{
				connectionHelper.Release();
			}
		}

		private static ISet<string> GetReservedWords(IConnectionProvider connectionProvider, Dialect.Dialect dialect)
		{
			IConnectionHelper connectionHelper = new SuppliedConnectionProviderConnectionHelper(connectionProvider);
			connectionHelper.Prepare();
			try
			{
				return GetReservedWords(dialect, connectionHelper);
			}
			finally
			{
				connectionHelper.Release();
			}
		}

		private static ISet<string> GetReservedWords(Dialect.Dialect dialect, IConnectionHelper connectionHelper)
		{
			ISet<string> reservedWords = new HashSet<string>(dialect.Keywords);
			var metaData = dialect.GetDataBaseSchema(connectionHelper.Connection);
			foreach (var rw in metaData.GetReservedWords())
			{
				reservedWords.Add(rw.ToLowerInvariant());
			}
			return reservedWords;
		}

		private static void QuoteTable(Table table, ICollection<string> reservedDb)
		{
			if (!table.IsQuoted && reservedDb.Contains(table.Name.ToLowerInvariant()))
			{
				table.Name = GetNhQuoted(table.Name);
			}
			foreach (var column in table.ColumnIterator)
			{
				if (!column.IsQuoted && reservedDb.Contains(column.Name.ToLowerInvariant()))
				{
					column.Name = GetNhQuoted(column.Name);
				}
			}
		}

		private static string GetNhQuoted(string name)
		{
			return "`" + name + "`";
		}
	}
}