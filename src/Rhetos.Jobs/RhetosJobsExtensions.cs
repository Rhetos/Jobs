using Newtonsoft.Json;
using Rhetos.Jobs;

namespace Rhetos.Dom.DefaultConcepts
{
	public static class RhetosJobsExtensions
	{
		public static ITask FromAction(this ITask task, object action)
		{
			task.Name = action.GetType().FullName;
			task.Parameters = JsonConvert.SerializeObject(action);

			return task;
		}
	}
}