using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Jobs
{
    /// <summary>
    /// Generic job executer for background jobs that are implemented as a DSL Action.
    /// </summary>
    /// <remarks>
    /// Instead of using this class directly, you may add a new job instance by calling
    /// <see cref="BackgroundJobExtensions.EnqueueAction"/>
    /// extension method on the <see cref="IBackgroundJob"/>.
    /// </remarks>
    public class ActionJobExecuter : IJobExecuter<ActionJobParameter>
    {
        private readonly INamedPlugins<IActionRepository> _actions;

        public ActionJobExecuter(INamedPlugins<IActionRepository> actions)
		{
            _actions = actions;
        }

		public void Execute(ActionJobParameter job)
		{
			var actionRepository = _actions.GetPlugin(job.ActionName);
			actionRepository.Execute(job.ActionParameters);
		}
	}
}
