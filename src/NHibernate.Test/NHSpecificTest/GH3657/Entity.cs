using System;

namespace NHibernate.Test.NHSpecificTest.GH3657
{
	class Entity
	{
		public virtual Guid Id { get; set; }
		public virtual string Name { get; set; }
	}
}
