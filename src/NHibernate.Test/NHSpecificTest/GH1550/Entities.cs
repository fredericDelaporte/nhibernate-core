using System;

namespace NHibernate.Test.NHSpecificTest.GH1550
{
	public abstract class AbstractClass : IEquatable<AbstractClass>
	{
		public virtual KeyClass IdOne { get; set; }
		public virtual KeyClass IdTwo { get; set; }

		public virtual bool Equals(AbstractClass other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(IdOne, other.IdOne)
				&& Equals(IdTwo, other.IdTwo);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((AbstractClass) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((IdOne != null ? IdOne.GetHashCode() : 0) * 397) ^ (IdTwo != null ? IdTwo.GetHashCode() : 0);
			}
		}

		public static bool operator ==(AbstractClass left, AbstractClass right) => Equals(left, right);

		public static bool operator !=(AbstractClass left, AbstractClass right) => !Equals(left, right);
	}

	public class DerivativeOne : AbstractClass
	{ }

	public class DerivativeTwo : AbstractClass
	{ }

	public class KeyClass
	{
		public virtual Guid Id { get; set; }
	}
}
