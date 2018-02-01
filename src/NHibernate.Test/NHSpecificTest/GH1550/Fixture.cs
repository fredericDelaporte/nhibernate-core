using System;
using System.Linq;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.GH1550
{
	[TestFixture]
	public class Fixture : BugTestCase
	{
		private KeyClass _idOne;
		private KeyClass _idTwo;

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				_idOne = new KeyClass();
				_idTwo = new KeyClass();

				session.Save(_idOne);
				session.Save(_idTwo);

				session.Save(
					new DerivativeOne
					{
						IdOne = _idOne,
						IdTwo = _idTwo
					});

				transaction.Commit();
			}
		}

		protected override void OnTearDown()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				session.CreateQuery("delete from System.Object").ExecuteUpdate();

				transaction.Commit();
			}
		}

		[Test]
		public void Criteria()
		{
			using (var session = OpenSession())
			using (var tx = session.BeginTransaction())
			{
				Assert.That(session.CreateCriteria<AbstractClass>().UniqueResult<AbstractClass>, Throws.Nothing);
				tx.Commit();
			}
		}

		[Test]
		public void Linq()
		{
			using (var session = OpenSession())
			using (var tx = session.BeginTransaction())
			{
				Assert.That(session.Query<AbstractClass>().Single, Throws.Nothing);
				tx.Commit();
			}
		}

		[Test]
		public void QueryOver()
		{
			using (var session = OpenSession())
			using (var tx = session.BeginTransaction())
			{
				Assert.That(session.QueryOver<AbstractClass>().SingleOrDefault, Throws.Nothing);
				tx.Commit();
			}
		}
	}
}
