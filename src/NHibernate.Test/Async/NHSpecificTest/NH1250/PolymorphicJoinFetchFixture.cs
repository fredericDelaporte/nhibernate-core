﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using NHibernate.Dialect;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH1250
{
	using System.Threading.Tasks;
	/// <summary>
	/// http://nhibernate.jira.com/browse/NH-1250
	/// http://nhibernate.jira.com/browse/NH-1340
	/// </summary>
	/// <remarks>Failure occurs in MsSql2005Dialect only</remarks>
	[TestFixture]
	public class PolymorphicJoinFetchFixtureAsync : BugTestCase
	{
		protected override bool AppliesTo(Dialect.Dialect dialect)
		{
			return dialect is MsSql2000Dialect;
		}

		[Test]
		public async Task FetchUsingICriteriaAsync()
		{
			using (ISession s = OpenSession())
			using (ITransaction tx = s.BeginTransaction())
			{
				await (s.CreateCriteria(typeof(Party))
					.SetMaxResults(10)
					.ListAsync());
				await (tx.CommitAsync());
			}
		}

		[Test]
		public async Task FetchUsingIQueryAsync()
		{
			using (ISession s = OpenSession())
			using (ITransaction tx = s.BeginTransaction())
			{
				await (s.CreateQuery("from Party")
					.SetMaxResults(10)
					.ListAsync());
				await (tx.CommitAsync());
			}
		}
	}
}
