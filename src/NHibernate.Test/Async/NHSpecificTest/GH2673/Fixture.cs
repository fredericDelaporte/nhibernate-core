﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.GH2673
{
	using System.Threading.Tasks;
	[TestFixture(true)]
	[TestFixture(false)]
	public class FixtureAsync : TestCaseMappingByCode
	{
		private readonly bool _withLazyProperties;

		public FixtureAsync(bool withLazyProperties)
		{
			_withLazyProperties = withLazyProperties;
		}

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var role1 = new Role {Id = 1, Name = "role1"};
				session.Save(role1);
				var role2 = new Role {Id = 2, Name = "role2"};
				session.Save(role2);

				var r1 = new Resource() {Id = 1, Name = "r1", ResourceRole = role1};
				session.Save(r1);

				var r2 = new Resource() {Id = 2, Name = "r2", ResourceRole = role2};
				session.Save(r2);

				var r3 = new Resource() {Id = 3, Name = "r3", ResourceRole = role2};
				session.Save(r3);

				r1.Manager = r2;
				r2.Manager = r3;
				r3.Manager = r1;
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
		public async Task DeserializeSameTypeAssociationWithInitializedProxyAndCircularReferencesAsync()
		{
			using (var session = OpenSession())
			{
				var r1 = await (session.LoadAsync<Resource>(1));
				var r2 = await (session.LoadAsync<Resource>(2));
				var r3 = await (session.LoadAsync<Resource>(3));

				var list = await (session.QueryOver<Resource>()
								.Fetch(SelectMode.Fetch, res => res.Manager)
								.ListAsync());

				try
				{
					var serialised = SpoofSerialization(list[0]);
					SpoofSerialization(session);
				}
				catch (SerializationException)
				{
					//Lazy properties case throws due to circular references. See GH-2563
					if (!_withLazyProperties)
						throw;
				}
			}
		}

		[Test]
		public async Task DeserializeSameTypeAssociationWithInitializedAndNotInitializedProxyAsync()
		{
			using (var session = OpenSession())
			{
				var r1 = await (session.GetAsync<Resource>(1));
				var r2 = await (session.GetAsync<Resource>(2));
				var r1Name = r1.Name;
				var serialised = SpoofSerialization(r1);
				Assert.That(serialised.Name, Is.EqualTo("r1"));
			}
		}

		private T SpoofSerialization<T>(T obj)
		{
			var formatter = new BinaryFormatter
			{
#if !NETFX
				SurrogateSelector = new NHibernate.Util.SerializationHelper.SurrogateSelector()
#endif
			};
			var stream = new MemoryStream();
			formatter.Serialize(stream, obj);

			stream.Position = 0;

			return (T) formatter.Deserialize(stream);
		}

		protected override HbmMapping GetMappings()
		{
			var mapper = new ModelMapper();
			mapper.Class<Resource>(
				m =>
				{
					m.Table("ResTable");
					m.Id(x => x.Id, (i) => i.Generator(Generators.Assigned));
					m.Property(x => x.Name, x => x.Lazy(_withLazyProperties));
					m.ManyToOne(x => x.Manager, x => x.ForeignKey("none"));
					m.ManyToOne(x => x.ResourceRole, x => x.ForeignKey("none"));
				});
			mapper.Class<Role>(
				m =>
				{
					m.Table("RoleTable");
					m.Id(x => x.Id, (i) => i.Generator(Generators.Assigned));
					m.Property(x => x.Name);
				});
			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}
	}
}
