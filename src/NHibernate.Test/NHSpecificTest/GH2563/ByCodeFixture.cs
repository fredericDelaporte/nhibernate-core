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
				});

			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var e1 = new Entity { Id = 1, Name = "Middle", };
				e1.Parent = new Entity { Id = 2, Name = "Parent", Child = e1 };

				session.Save(e1);
				session.Save(e1.Parent);

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
		public void SerializationOfConnectedEntitiesWithLazyProperties()
		{
			using (var session = OpenSession())
			{
				var result = session.Query<Entity>().ToList();
				var ds = SpoofSerialization(session);
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
