﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Transactions;
using log4net;
using log4net.Repository.Hierarchy;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Engine;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH3023
{
	[TestFixture]
	public class DeadlockConnectionPoolIssue : BugTestCase
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(DeadlockConnectionPoolIssue));

		protected virtual bool UseConnectionOnSystemTransactionPrepare => true;

		protected override void Configure(Configuration configuration)
		{
			configuration.SetProperty(
				Cfg.Environment.UseConnectionOnSystemTransactionPrepare,
				UseConnectionOnSystemTransactionPrepare.ToString());
		}

		// Uses directly SqlConnection.
		protected override bool AppliesTo(ISessionFactoryImplementor factory) =>
			factory.ConnectionProvider.Driver is SqlClientDriver &&
			factory.ConnectionProvider.Driver.SupportsSystemTransactions;

		protected override bool AppliesTo(Dialect.Dialect dialect) =>
			dialect is MsSql2000Dialect;

		protected override void OnSetUp()
		{
			RunScript("db-seed.sql");

			((Logger)_log.Logger).Level = log4net.Core.Level.Debug;
		}

		protected override void OnTearDown()
		{
			// Before clearing the pool for dodging pool corruption, we need to wait
			// for late transaction processing not yet ended.
			Thread.Sleep(100);
			//
			// Hopefully this will clean up the pool so that teardown can succeed
			//
			SqlConnection.ClearAllPools();

			RunScript("db-teardown.sql");

			using (var s = OpenSession())
			{
				s.CreateQuery("delete from System.Object").ExecuteUpdate();
			}
		}

		[Theory]
		public void ConnectionPoolCorruptionAfterDeadlock(bool distributed, bool disposeSessionBeforeScope)
		{
			var tryCount = 0;
			var id = 1;
			do
			{
				tryCount++;
				var missingDeadlock = false;

				try
				{
					_log.DebugFormat("Starting loop {0}", tryCount);
					// When the connection is released from transaction completion, the scope disposal after deadlock
					// takes up to 30 seconds (not at first try, but at subsequent tries). With additional logs, it
					// appears this delay occurs at connection closing. Definitely, there is something which can go
					// wrong when disposing a connection from transaction scope completion.
					// Note that the transaction completion event can execute as soon as the deadlock occurs. It does
					// not wait for the scope disposal.
					var session = OpenSession();
					var scope = distributed ? CreateDistributedTransactionScope() : new TransactionScope();
					try
					{
						_log.Debug("Session and scope opened");
						session.GetSessionImplementation().Factory.TransactionFactory
							   .EnlistInSystemTransactionIfNeeded(session.GetSessionImplementation());
						_log.Debug("Session enlisted");
						try
						{
							new DeadlockHelper().ForceDeadlockOnConnection(
								(SqlConnection)session.Connection,
								GetConnectionString());
						}
						catch (SqlException x)
						{
							//
							// Deadlock error code is 1205.
							//
							if (x.Errors.Cast<SqlError>().Any(e => e.Number == 1205))
							{
								//
								// It did what it was supposed to do.
								//
								_log.InfoFormat("Expected deadlock on attempt {0}. {1}", tryCount, x.Message);
								continue;
							}

							//
							// ? This shouldn't happen
							//
							Assert.Fail($"Surprising exception when trying to force a deadlock: {x}");
						}

						_log.WarnFormat("Initial session seemingly not deadlocked at attempt {0}", tryCount);
						missingDeadlock = true;

						try
						{
							session.Save(
								new DomainClass
								{
									Id = id++,
									ByteData = new byte[] {1, 2, 3}
								});

							session.Flush();
							if (tryCount < 10)
							{
								_log.InfoFormat("Initial session still usable, trying again");
								continue;
							}
							_log.InfoFormat("Initial session still usable after {0} attempts, finishing test", tryCount);
						}
						catch (Exception ex)
						{
							_log.Error("Failed to continue using the session after lacking deadlock.", ex);
							// This exception would hide the transaction failure, if any.
							//throw;
						}
						_log.Debug("Completing scope");
						scope.Complete();
						_log.Debug("Scope completed");
					}
					finally
					{
						// Check who takes time in the disposing
						var chrono = new Stopwatch();
						if (disposeSessionBeforeScope)
						{
							try
							{
								chrono.Start();
								session.Dispose();
								_log.Debug("Session disposed");
								Assert.That(chrono.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)), "Abnormal session disposal duration");
							}
							catch (Exception ex)
							{
								// Log in case it gets hidden by the next finally
								_log.Warn("Session disposal failure", ex);
								throw;
							}
							finally
							{
								chrono.Restart();
								scope.Dispose();
								_log.Debug("Scope disposed");
								Assert.That(chrono.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)), "Abnormal scope disposal duration");
							}
						}
						else
						{
							try
							{
								chrono.Start();
								scope.Dispose();
								_log.Debug("Scope disposed");
								Assert.That(chrono.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)), "Abnormal scope disposal duration");
							}
							catch (Exception ex)
							{
								// Log in case it gets hidden by the next finally
								_log.Warn("Scope disposal failure", ex);
								throw;
							}
							finally
							{
								chrono.Restart();
								session.Dispose();
								_log.Debug("Session disposed");
								Assert.That(chrono.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)), "Abnormal session disposal duration");
							}
						}
					}
					_log.Debug("Session and scope disposed");
				}
				catch (AssertionException)
				{
					throw;
				}
				catch (Exception x)
				{
					_log.Error($"Initial session failed at attempt {tryCount}.", x);
				}

				var subsequentFailedRequests = 0;

				for (var i = 1; i <= 10; i++)
				{
					//
					// The error message will vary on subsequent requests, so we'll somewhat
					// arbitrarily try 10
					//

					try
					{
						using (var scope = new TransactionScope())
						{
							using (var session = OpenSession())
							{
								session.Save(
									new DomainClass
									{
										Id = id++,
										ByteData = new byte[] { 1, 2, 3 }
									});

								session.Flush();
							}

							scope.Complete();
						}
					}
					catch (Exception x)
					{
						subsequentFailedRequests++;
						_log.Error($"Subsequent session {i} failed.", x);
					}
				}

				Assert.Fail(
					missingDeadlock
						? $"Deadlock not reported on initial request, and initial request failed; {subsequentFailedRequests} subsequent requests failed."
						: $"Initial request failed; {subsequentFailedRequests} subsequent requests failed.");
				
			} while (tryCount < 3);
			//
			// I'll change this to while(true) sometimes so I don't have to keep running the test
			//
		}

		private static TransactionScope CreateDistributedTransactionScope()
		{
			var scope = new TransactionScope();
			//
			// Forces promotion to distributed transaction
			//
			TransactionInterop.GetTransmitterPropagationToken(System.Transactions.Transaction.Current);
			return scope;
		}

		private void RunScript(string script)
		{
			var cxnString = GetConnectionString() + "; Pooling=No";
			// Disable connection pooling so this won't be hindered by
			// problems encountered during the actual test

			string sql;
			using (var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream(GetType().Namespace + "." + script)))
			{
				sql = reader.ReadToEnd();
			}

			using (var cxn = new SqlConnection(cxnString))
			{
				cxn.Open();

				foreach (var batch in Regex.Split(sql, @"^go\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)
					.Where(b => !string.IsNullOrEmpty(b)))
				{
					using (var cmd = new System.Data.SqlClient.SqlCommand(batch, cxn))
					{
						cmd.ExecuteNonQuery();
					}
				}
			}
		}

		private string GetConnectionString()
		{
			return cfg.Properties["connection.connection_string"];
		}
	}

	[TestFixture]
	public class DeadlockConnectionPoolIssueWithoutConnectionFromPrepare : DeadlockConnectionPoolIssue
	{
		protected override bool UseConnectionOnSystemTransactionPrepare => false;
	}
}
