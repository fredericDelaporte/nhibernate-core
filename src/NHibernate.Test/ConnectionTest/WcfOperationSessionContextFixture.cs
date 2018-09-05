#if NETFX
using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using NHibernate.Cfg;
using NHibernate.Context;
using NUnit.Framework;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Test.ConnectionTest
{
	// This fixture requires administrator privileges: it launches a WCF service.
	[TestFixture]
	public class WcfOperationSessionContextFixture : ConnectionManagementTestCase
	{
		private ServiceHost _host;

		protected override ISession GetSessionUnderTest()
		{
			var session = OpenSession();
			session.BeginTransaction();
			return session;
		}

		protected override void Configure(Configuration configuration)
		{
			cfg.SetProperty(Environment.CurrentSessionContextClass, "wcf_operation");
		}

		protected override void OnSetUp()
		{
			var host = new ServiceHost(
				new DummyService(cfg),
				new Uri("http://localhost:8000/WcfOperationSessionContextFixture/"));
			try
			{
				var smb = new ServiceMetadataBehavior
				{
					HttpGetEnabled = true
				};
				host.AddServiceEndpoint(typeof(IDummyService), new WSHttpBinding(), "DummyService");
				host.Description.Behaviors.Add(smb);
				host.Open();
			}
			catch
			{
				host.Abort();
				throw;
			}

			_host = host;
		}

		protected override void OnTearDown()
		{
			try
			{
				_host?.Close();
			}
			catch
			{
				_host?.Abort();
				throw;
			}
		}

		[Test]
		public void MultiFactory()
		{
			using (var client = new DummyServiceClient())
			{
				// The operation context is not available on client side. Un-commenting following line would cause a failure.
				//Assert.That(OperationContext.Current, Is.Not.Null);
				client.MultiFactory();
				client.Close();
			}
		}

		[ServiceContract(Namespace = "http://NHibernate.Test")]
		public interface IDummyService
		{
			[OperationContract]
			void MultiFactory();
		}

		[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
		public class DummyService : IDummyService
		{
			private readonly Configuration _cfg;

			public DummyService(Configuration cfg)
			{
				_cfg = cfg;
			}

			public void MultiFactory()
			{
				using (var factory1 = _cfg.BuildSessionFactory())
				using (var session1 = factory1.OpenSession())
				using (var factory2 = _cfg.BuildSessionFactory())
				using (var session2 = factory2.OpenSession())
				{
					CurrentSessionContext.Bind(session1);
					AssertCurrentSession(factory1, session1, "Unexpected session for factory1 after bind of session1.");
					CurrentSessionContext.Bind(session2);
					AssertCurrentSession(factory2, session2, "Unexpected session for factory2 after bind of session2.");
					AssertCurrentSession(factory1, session1, "Unexpected session for factory1 after bind of session2.");
				}
			}

			private void AssertCurrentSession(ISessionFactory factory, ISession session, string message)
			{
				Assert.That(
					factory.GetCurrentSession(),
					Is.EqualTo(session),
					"{0} {1} instead of {2}.",
					message,
					factory.GetCurrentSession().GetSessionImplementation().SessionId,
					session.GetSessionImplementation().SessionId);
			}
		}
	}
}
#endif
