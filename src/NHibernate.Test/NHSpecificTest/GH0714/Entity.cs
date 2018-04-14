using System;

namespace NHibernate.Test.NHSpecificTest.GH0714
{
	[Serializable]
	public class Component
	{
		public override bool Equals(object obj)
		{
			if (!(obj is Component other))
			{
				return false;
			}
			// Since Entity2 does not override Equals this implementation is only valid
			// when comparing components made of entities attached to the same session.
			return other.PK1 == PK1 && other.PK2 == PK2;
		}

		public override int GetHashCode()
		{
			return PK1?.GetHashCode() ?? 0 ^ PK2?.GetHashCode() ?? 0;
		}

		public Entity2 PK1 { get; set; }
		public Entity2 PK2 { get; set; }
	}

	public class Entity1
	{
		public virtual Component ID { get; set; }
	}

	public class Entity2
	{
		public virtual int Id { get; set; }
	}
}
