﻿using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.GH0714
{
	[TestFixture]
	public class Fixture : BugTestCase
	{
		public override string BugNumber => "GH0714";

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var e2 = new Entity2();
				session.Save(e2);

				var e2_2 = new Entity2();
				session.Save(e2_2);

				var e1 = new Entity1 { ID = new Component { PK1 = e2, PK2 = e2_2 } };

				session.Save(e1);
				transaction.Commit();
			}

			Sfi.Statistics.IsStatisticsEnabled = true;
		}

		protected override void OnTearDown()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				// The HQL delete does all the job inside the database without loading the entities, but it does
				// not handle delete order for avoiding violating constraints if any. Use
				// session.Delete("from System.Object");
				// instead if in need of having NHbernate ordering the deletes, but this will cause
				// loading the entities in the session.
				session.CreateQuery("delete Entity1").ExecuteUpdate();
				session.CreateQuery("delete Entity2").ExecuteUpdate();

				transaction.Commit();
			}
		}

		[Test]
		public void GH0714()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				Sfi.Statistics.Clear();
				var r = session.CreateQuery("from Entity1 e1 inner join fetch e1.ID.PK1 inner join fetch e1.ID.PK2").List<Entity1>();
				Assert.That(Sfi.Statistics.PrepareStatementCount, Is.EqualTo(1));
				foreach (var e in r)
				{
					Assert.That(NHibernateUtil.IsInitialized(e), Is.True, "An Entity1 was lazily loaded instead of being fully loaded");
					Assert.That(NHibernateUtil.IsInitialized(e.ID.PK1), Is.True, "An Entity1 PK1 was lazily loaded instead of being fully loaded");
					Assert.That(NHibernateUtil.IsInitialized(e.ID.PK2), Is.True, "An Entity1 PK2 was lazily loaded instead of being fully loaded");
				}
				transaction.Commit();
			}
		}
	}
}
