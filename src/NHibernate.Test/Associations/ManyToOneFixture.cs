using System.Linq;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Test.Associations.ManyToOneFixtureEntities;
using NUnit.Framework;

namespace NHibernate.Test.Associations
{
	[TestFixture]
	public class ManyToOneFixture : TestCaseMappingByCode
	{
		[Test]
		public void DupColManyToOneQueryAndUpdate()
		{
			using (var session = OpenSession())
			{
				var e = session.Query<Parent>().FirstOrDefault();

				Assert.That(e, Is.Not.Null);
				Assert.That(e.ManyToOne1, Is.Not.EqualTo(e.ManyToOne2));

				e.ManyToOne1 = session.Load<EntityWithCompositeId>(_key3);
				e.ManyToOne2 = session.Load<EntityWithCompositeId>(_key4);
				session.Flush();
			}

			using (var session = OpenSession())
			{
				var e = session.Query<Parent>().FirstOrDefault();

				Assert.That(e, Is.Not.Null);
				Assert.That(e.ManyToOne1, Is.Not.EqualTo(e.ManyToOne2));

				Assert.That(e.ManyToOne1.Key, Is.EqualTo(_key3));
				Assert.That(e.ManyToOne2.Key, Is.EqualTo(_key4));
			}
		}

		#region Test Setup

		private CompositeKey _key3;
		private CompositeKey _key4;

		protected override string CacheConcurrencyStrategy => null;

		protected override HbmMapping GetMappings()
		{
			var mapper = new ModelMapper();

			mapper.Class<EntityWithCompositeId>(
				rc =>
				{
					rc.ComponentAsId(
						e => e.Key,
						ekm =>
						{
							ekm.Property(ek => ek.Id1);
							ekm.Property(ek => ek.Id2);
						});
					rc.Property(e => e.Name);
				});

			mapper.Class<Parent>(
				rc =>
				{
					rc.Id(
						e => e.Id,
						ekm => { ekm.Generator(Generators.Native); });

					rc.ManyToOne(
						e => e.ManyToOne1,
						m => { m.Columns(c => c.Name("Id1"), c => c.Name("Id2")); });
					rc.ManyToOne(
						e => e.ManyToOne2,
						m => { m.ColumnsAndFormulas(cf => cf.Formula("Id1"), c => c.Name("Id3")); });
				});

			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}

		protected override void OnTearDown()
		{
			using (ISession session = OpenSession())
			using (ITransaction transaction = session.BeginTransaction())
			{
				session.Delete("from System.Object");

				session.Flush();
				transaction.Commit();
			}
		}

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var key1 = new CompositeKey { Id1 = 4, Id2 = 3, };
				var key2 = new CompositeKey { Id1 = 4, Id2 = 2, };
				var manyToOneParent = new Parent()
				{
					ManyToOne1 = new EntityWithCompositeId { Key = key1, Name = "Composite1" },
					ManyToOne2 = new EntityWithCompositeId { Key = key2, Name = "Composite2" },
				};

				session.Save(manyToOneParent.ManyToOne1);
				session.Save(manyToOneParent.ManyToOne2);
				session.Save(manyToOneParent);

				_key3 = new CompositeKey { Id1 = 5, Id2 = 3, };
				_key4 = new CompositeKey { Id1 = 5, Id2 = 2, };
				session.Save(new EntityWithCompositeId { Key = _key3, Name = "Composite3" });
				session.Save(new EntityWithCompositeId { Key = _key4, Name = "Composite4" });

				session.Flush();
				transaction.Commit();
			}
		}

		#endregion Test Setup
	}

	namespace ManyToOneFixtureEntities
	{
		public class CompositeKey
		{
			public int Id1 { get; set; }
			public int Id2 { get; set; }

			public override bool Equals(object obj)
			{
				return obj is CompositeKey key
					&& Id1 == key.Id1
					&& Id2 == key.Id2;
			}

			public override int GetHashCode()
			{
				var hashCode = -1596524975;
				hashCode = hashCode * -1521134295 + Id1.GetHashCode();
				hashCode = hashCode * -1521134295 + Id2.GetHashCode();
				return hashCode;
			}
		}

		public class EntityWithCompositeId
		{
			public virtual CompositeKey Key { get; set; }
			public virtual string Name { get; set; }
		}

		public class Parent
		{
			public virtual int Id { get; set; }

			public virtual EntityWithCompositeId ManyToOne1 { get; set; }

			public virtual EntityWithCompositeId ManyToOne2 { get; set; }
		}
	}
}
