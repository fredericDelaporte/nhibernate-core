﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.Linq;
using NUnit.Framework;
using NHibernate.Cfg;
using NHibernate.Type;

namespace NHibernate.Test.NHSpecificTest.GH1486
{
	using System.Threading.Tasks;
	using System.Threading;
	[TestFixture]
	public class FixtureAsync : BugTestCase
	{
		private readonly OnFlushDirtyInterceptor _interceptor = new OnFlushDirtyInterceptor();

		protected override void Configure(Configuration configuration)
		{
			configuration.SetInterceptor(_interceptor);
		}

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			{
				using (var transaction = session.BeginTransaction())
				{
					var john = new Person(1, "John", new Address());
					session.Save(john);

					var mary = new Person(2, "Mary", null);
					session.Save(mary);

					var bob = new Person(3, "Bob", new Address("1", "A", "B"));
					session.Save(bob);

					session.Flush();
					transaction.Commit();
				}
			}
		}

		protected override void OnTearDown()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				session.Delete("from System.Object");
				session.Flush();
				transaction.Commit();
			}
		}

		/// <summary>
		/// The test case was imported from Hibernate HHH-11237 and adjusted for NHibernate. 
		/// </summary>
		[Test]
		public async Task TestSelectBeforeUpdateAsync()
		{
			using (var session = OpenSession())
			{
				using (var transaction = session.BeginTransaction())
				{
					var john = await (session.GetAsync<Person>(1));
					_interceptor.Reset();
					john.Address = null;
					await (session.FlushAsync());
					Assert.That(_interceptor.CallCount, Is.EqualTo(0), "unexpected flush dirty count for John");

					_interceptor.Reset();
					var mary = await (session.GetAsync<Person>(2));
					mary.Address = new Address();
					await (session.FlushAsync());
					Assert.That(_interceptor.CallCount, Is.EqualTo(0), "unexpected flush dirty count for Mary");
					await (transaction.CommitAsync());
				}
			}

			Person johnObj;
			Person maryObj;
			using (var session = OpenSession())
			{
				using (var transaction = session.BeginTransaction())
				{
					johnObj = await (session.GetAsync<Person>(1));
				}
			}

			using (var session = OpenSession())
			{
				using (var transaction = session.BeginTransaction())
				{
					maryObj = await (session.GetAsync<Person>(2));
				}
			}

			using (var session = OpenSession())
			{
				using (var transaction = session.BeginTransaction())
				{
					_interceptor.Reset();
					johnObj.Address = null;
					await (session.UpdateAsync(johnObj));
					await (session.FlushAsync());
					Assert.That(_interceptor.CallCount, Is.EqualTo(0), "unexpected flush dirty count for John update");

					_interceptor.Reset();
					maryObj.Address = new Address();
					await (session.UpdateAsync(maryObj));
					await (session.FlushAsync());
					Assert.That(_interceptor.CallCount, Is.EqualTo(0), "unexpected flush dirty count for Mary update");
					await (transaction.CommitAsync());
				}
			}
		}

		[Test]
		public async Task TestDirectCallToIsModifiedAsync()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var person = await (session.LoadAsync<Person>(3));
				Assert.That(person, Is.Not.Null, "Bob is not found.");
				Assert.That(person.Address, Is.Not.Null, "Bob's address is missing.");
				var sessionImplementor = session.GetSessionImplementation();

				var metaData = session.SessionFactory.GetClassMetadata(typeof(Person));
				foreach (var propertyType in metaData.PropertyTypes)
				{
					if (!(propertyType is ComponentType componentType) || componentType.ReturnedClass.Name != "Address")
						continue;

					var checkable = new [] { true, true, true };
					Assert.That(
						() => componentType.IsModifiedAsync(new object[] { "", "", "" }, person.Address, checkable, sessionImplementor, CancellationToken.None),
						Throws.Nothing,
						"Checking component against an array snapshot failed");
					var isModified = await (componentType.IsModifiedAsync(person.Address, person.Address, checkable, sessionImplementor, CancellationToken.None));
					Assert.That(isModified, Is.False, "Checking same component failed");
					isModified = await (componentType.IsModifiedAsync(new Address("1", "A", "B"), person.Address, checkable, sessionImplementor, CancellationToken.None));
					Assert.That(isModified, Is.False, "Checking equal component failed");
				}
				await (transaction.RollbackAsync());
			}
		}
	}
}
