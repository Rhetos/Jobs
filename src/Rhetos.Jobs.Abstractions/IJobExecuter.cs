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

namespace Rhetos.Jobs
{
	/// <summary>
	/// Implement this interface to add a custom background job type.
	/// Existing implementation, <see cref="ActionJobExecuter"/>, may be used for jobs that are
	/// implemented simply as a DSL Action.
	/// </summary>
	/// <remarks>
	/// The implementation needs to be registered to Rhetos DI container.
	/// It will be resolved from DI container scope when the job is executed,
	/// as a unit of work (a separate database transaction).
	/// </remarks>
	/// <typeparam name="TParameter">Job parameter type.</typeparam>
	public interface IJobExecuter<in TParameter>
	{
		/// <summary>
		/// Executes the job immediately.
		/// </summary>
		void Execute(TParameter job);
	}
}