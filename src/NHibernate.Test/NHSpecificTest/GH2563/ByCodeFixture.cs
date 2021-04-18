using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.GH2563
{
	[TestFixture]
	public class ByCodeFixture : TestCaseMappingByCode
	{
		protected override HbmMapping GetMappings()
		{
			var mapper = new ModelMapper();
			mapper.Class<Entity>(
				rc =>
				{
					rc.Id(x => x.Id, m => m.Generator(Generators.Assigned));
					rc.Property(x => x.Name, m => m.Lazy(true));
					rc.ManyToOne(x => x.Child);
					rc.ManyToOne(x => x.Parent);
					rc.Set(
						e => e.Children,
						m =>
						{
							m.Key(c => c.Column("OtherParent"));
							m.Inverse(true);
						},
						r => r.OneToMany());
					rc.ManyToOne(x => x.OtherParent);
					rc.Set(
						e => e.Related,
						m =>
						{
							m.Table("Related");
							m.Key(c => c.Column("Related"));
						},
						r => r.ManyToMany(
							m => m.Column("InverseRelated")));
					rc.Set(
						e => e.InverseRelated,
						m =>
						{
							m.Table("Related");
							m.Key(c => c.Column("InverseRelated"));
							m.Inverse((true));
						},
						r => r.ManyToMany(
							m => m.Column("Related")));
				});

			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var e1 = new Entity { Id = 1, Name = "Child", };
				e1.Parent = new Entity { Id = 2, Name = "Parent", Child = e1 };
				session.Save(e1);
				session.Save(e1.Parent);

				var e3 = new Entity
				{
					Id = 3,
					Name = "Child",
					OtherParent = new Entity { Id = 4, Name = "OtherParent" },
				};
				session.Save(e3);
				session.Save(e3.OtherParent);

				var e5 = new Entity { Id = 5, Name = "Related", Related = new HashSet<Entity>()};
				var e6 = new Entity { Id = 6, Name = "InverseRelated"};
				e5.Related.Add(e6);
				session.Save(e5);
				session.Save(e6);

				var e7 = new Entity { Id = 7, Name = "CrossRelated1", Related = new HashSet<Entity>()};
				var e8 = new Entity { Id = 8, Name = "CrossRelated2", Related = new HashSet<Entity>()};
				e7.Related.Add(e8);
				e8.Related.Add(e7);
				session.Save(e7);
				session.Save(e8);

				transaction.Commit();
			}
		}

		protected override void OnTearDown()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				session.Delete("from System.Object");
				transaction.Commit();
			}
		}

		[Test]
		public void SerializationOfConnectedEntitiesWithLazyProperties()
		{
			using (var session = OpenSession())
			{
				var result = session.Query<Entity>().Where(e => e.Id < 3).ToList();
				var e1 = result.FirstOrDefault((e => e.Id == 1));
				Assert.That(e1, Is.Not.Null);
				Assert.That(e1.Parent, Is.Not.Null);
				Assert.That(e1, Is.EqualTo(e1.Parent.Child));
				SpoofSerialization(session);
			}
		}

		[Test]
		public void SerializationOfCollectionConnectedEntitiesWithLazyProperties()
		{
			using (var session = OpenSession())
			{
				var result = session.Query<Entity>().Where(e => e.Id > 2 && e.Id < 5).ToList();
				var e3 = result.FirstOrDefault((e => e.Id == 3));
				Assert.That(e3, Is.Not.Null);
				Assert.That(e3.OtherParent, Is.Not.Null);
				Assert.That(e3.OtherParent.Children, Contains.Item(e3));
				SpoofSerialization(session);
			}
		}

		[Test]
		public void SerializationOfManyToManyConnectedEntitiesWithLazyProperties()
		{
			using (var session = OpenSession())
			{
				var result = session.Query<Entity>().Where(e => e.Id > 4 && e.Id < 7).ToList();
				var e5 = result.FirstOrDefault((e => e.Id == 5));
				var e6 = result.FirstOrDefault((e => e.Id == 6));
				Assert.That(e5, Is.Not.Null);
				Assert.That(e5.Related, Contains.Item(e6));
				Assert.That(e6, Is.Not.Null);
				Assert.That(e6.InverseRelated, Contains.Item(e5));
				SpoofSerialization(session);
			}
		}

		[Test]
		public void SerializationOfCrossManyToManyConnectedEntitiesWithLazyProperties()
		{
			using (var session = OpenSession())
			{
				var result = session.Query<Entity>().Where(e => e.Id > 6).ToList();
				var e7 = result.FirstOrDefault((e => e.Id == 7));
				var e8 = result.FirstOrDefault((e => e.Id == 8));
				Assert.That(e7, Is.Not.Null);
				Assert.That(e7.Related, Contains.Item(e8));
				Assert.That(e8, Is.Not.Null);
				Assert.That(e8.Related, Contains.Item(e7));
				SpoofSerialization(session);
			}
		}

		private T SpoofSerialization<T>(T session)
		{
			var formatter = new BinaryFormatter();
			var stream = new MemoryStream();
			formatter.Serialize(stream, session);

			stream.Position = 0;
			return (T) new BinaryFormatter().Deserialize(stream);
		}
	}
}
