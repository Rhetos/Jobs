using System;

namespace Rhetos.Jobs.Hangfire
{
	public class Job : IEquatable<Job>
	{
		public Guid Id { get; set; }
		public string ActionName { get; set; }
		public string ActionParameters { get; set; }
		public string ExecuteAsUser { get; set; }

		public bool Equals(Job other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return ActionName == other.ActionName && ActionParameters == other.ActionParameters && ExecuteAsUser == other.ExecuteAsUser;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Job) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (ActionName != null ? ActionName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (ActionParameters != null ? ActionParameters.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (ExecuteAsUser != null ? ExecuteAsUser.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}