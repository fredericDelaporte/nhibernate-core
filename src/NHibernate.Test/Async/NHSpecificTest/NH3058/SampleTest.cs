﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using NHibernate.Context;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH3058
{
	using System.Threading.Tasks;
	[TestFixture]
	public class SampleTestAsync : BugTestCase
	{
		protected override void Configure(Cfg.Configuration configuration)
		{
			configuration.Properties.Add("current_session_context_class", "thread_static");
		}

		protected override ISession OpenSession()
		{
			var session = base.OpenSession();

			CurrentSessionContext.Bind(session);

			return session;
		}

		protected override void OnSetUp()
		{
			base.OnSetUp();
			using (var s = OpenSession())
			using (var tx = s.BeginTransaction())
			{
				var book = new DomainClass
				{
					Name = "Some name",
					ALotOfText = "Some text",
					Id = 1
				};

				s.Persist(book);
				tx.Commit();
			}
		}

		protected override void OnTearDown()
		{
			base.OnTearDown();
			using (var s = OpenSession())
			using (var tx = s.BeginTransaction())
			{
				Assert.That(s.CreateSQLQuery("delete from DomainClass").ExecuteUpdate(), Is.EqualTo(1));
				tx.Commit();
			}
		}

		[Test]
		public async Task MethodShouldLoadLazyPropertyAsync()
		{
			using (var s = OpenSession())
			using (var tx = s.BeginTransaction())
			{
				var book = await (s.LoadAsync<DomainClass>(1));
				
				Assert.False(NHibernateUtil.IsPropertyInitialized(book, "ALotOfText"));

				string value = book.LoadLazyProperty();

				Assert.That(value, Is.EqualTo("Some text"));
				Assert.That(NHibernateUtil.IsPropertyInitialized(book, "ALotOfText"), Is.True);

				await (tx.CommitAsync());
			}
		}
	}
}
