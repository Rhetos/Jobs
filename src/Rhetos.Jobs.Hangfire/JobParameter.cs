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

using System;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Job parameters required for job execution.
    /// It is serialized to the Hangfire job queue storage before executing it.
    /// </summary>
    internal sealed class JobParameter<TParameter> : IJobParameter
	{
		public Guid Id { get; set; }

        /// <summary>
        /// Null for immediate asynchronous jobs, not null for recurring jobs.
        /// </summary>
        public string RecurringJobName { get; set; }

		public string ExecuteAsUser { get; set; }

        /// <summary>
        /// The <see langword="null"/> value is considered <see langword="false"/>, to simplify job parameter serialization.
        /// </summary>
        public bool? ExecuteAsAnonymous { get; set; }

        public TParameter Parameter { get; set; }

		public string GetLogInfo(Type executerType)
		{
			var userInfo =
                !string.IsNullOrWhiteSpace(ExecuteAsUser) ? $"ExecuteAsUser: {ExecuteAsUser}"
                : ExecuteAsAnonymous == true ? "ExecuteAsAnonymous"
                : "User not specified";
			return $"JobId: {Id}|{userInfo}|{executerType}|{Parameter}";
		}
	}
}