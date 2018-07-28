using System.Linq;
using NHibernate.Dialect;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.GH1254
{
	[TestFixture]
	public class Fixture : BugTestCase
	{
		protected override bool AppliesTo(Dialect.Dialect dialect)
		{
			// This test creates a stored procedure with DB2 syntax.
			return dialect is DB2Dialect;
		}

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var e1 = new Entity {Name = "Bob"};
				session.Save(e1);

				var e2 = new Entity {Name = "Sally"};
				session.Save(e2);

				transaction.Commit();
			}
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
				session.CreateQuery("delete from System.Object").ExecuteUpdate();

				transaction.Commit();
			}
		}

		[Test]
		public void CallProcedure()
		{
			using (var session = OpenSession())
			using (session.BeginTransaction())
			{
				var result =
					session
						.GetNamedQuery("GetEntityByName")
						.SetString("name", "Sally")
						.List<Entity>();

				Assert.That(result, Has.Count.EqualTo(1));
			}
		}
	}
}
