/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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
