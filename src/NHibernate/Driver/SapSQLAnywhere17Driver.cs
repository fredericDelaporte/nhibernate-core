namespace NHibernate.Driver
{
	/// <summary>
	/// The SybaseSQLAnywhereDriver Driver provides a database driver for Sap SQL Anywhere 17 and above
	/// </summary>
	public class SapSQLAnywhere17Driver : ReflectionBasedDriver
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SapSQLAnywhere17Driver"/> class.
		/// </summary>
		/// <exception cref="HibernateException">
		/// Thrown when the Sap.Data.SQLAnywhere assembly is not found or can not be loaded.
		/// </exception>
		public SapSQLAnywhere17Driver()
			: base("Sap.Data.SQLAnywhere.v4.5", "Sap.Data.SQLAnywhere.SAConnection", "Sap.Data.SQLAnywhere.SACommand")
		{
		}

		public override bool UseNamedPrefixInSql => true;

		public override bool UseNamedPrefixInParameter => true;

		public override string NamedPrefix => ":";

		public override bool RequiresTimeSpanForTime => true;
	}
}