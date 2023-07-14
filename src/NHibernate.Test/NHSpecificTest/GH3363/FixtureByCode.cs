﻿using System.Linq;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.GH3363
{
	/// <summary>
	/// Fixture for wrong statement generated when child classes have same propery name but different type.
	/// </summary>
	[TestFixture]
	public class ByCodeFixture : TestCaseMappingByCode
	{
		protected override HbmMapping GetMappings()
		{
			var mapper = new ModelMapper();

			mapper.Class<Mother>(rc =>
			{
				rc.Id(x => x.Id, m => m.Generator(Generators.Identity));
				rc.Property(x => x.Name);
				rc.Discriminator(x => x.Column("kind"));
			});
			mapper.Subclass<Child1>(rc =>
			{
				rc.Property(x => x.Name);
				rc.ManyToOne(x => x.Thing, m =>
				{
					m.NotFound(NotFoundMode.Ignore);
					m.Column("thingId");
				});
				rc.DiscriminatorValue(1);
			});
			mapper.Subclass<Child2>(rc =>
			{
				rc.Property(x => x.Name);
				rc.ManyToOne(x => x.Thing, m =>
				{
					m.NotFound(NotFoundMode.Ignore);
					m.Column("thingId");
				});
				rc.DiscriminatorValue(2);
			});
			mapper.Class<Thing1>(rc =>
			{
				rc.Id(x => x.Id, m => m.Generator(Generators.Assigned));
				rc.Property(x => x.Name);

			});
			mapper.Class<Thing2>(rc =>
			{
				rc.Id(x => x.Id, m => m.Generator(Generators.Assigned));
				rc.Property(x => x.Name);

			});
			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var t1 = new Thing1() { Name = "don't care", Id = "00001" };
				session.Save(t1);
				var t2 = new Thing2() { Name = "look for this", Id = "00002" };
				session.Save(t2);
				var child1 = new Child1 { Name = "Child1", Thing = t1 };
				session.Save(child1);
				var child2 = new Child2 { Name = "Child1", Thing = t2 };
				session.Save(child2);
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
		public void LookForThingOfTypeThing2()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var result = session.Query<Mother>().Where(k => k is Child2 && (k as Child2).Thing.Id == "00002").ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				transaction.Commit();
			}
		}

		[Test]
		public void LookForThingOfTypeThing1()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var oftype = session.Query<Mother>().Where(k => k is Child1).ToList();
				Assert.That(oftype, Has.Count.EqualTo(1));
				Assert.That((oftype[0] as Child1).Thing, Is.Not.Null);
				/*
				 * wrong statement created
				 * select mother0_.Id as id1_0_, mother0_.Name as name3_0_,
					mother0_.thingId as thingid4_0_, mother0_.kind as kind2_0_ from Mother mother0_ 
					left outer join Thing2 thing2x1_ on 
					mother0_.thingId=thing2x1_.Id where mother0_.kind='1' and thing2x1_.Id=?"
				 * 
				 */

				var result = session.Query<Mother>().Where(k => k is Child1 && (k as Child1).Thing.Id == "00001").ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				transaction.Commit();
			}
		}

		[Test]
		public void LookForManyToOneByDescr()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var result = session.Query<Mother>().Where(k => k is Child2 && (k as Child2).Thing.Name == "look for this").ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				transaction.Commit();
			}
		}
	}
}
