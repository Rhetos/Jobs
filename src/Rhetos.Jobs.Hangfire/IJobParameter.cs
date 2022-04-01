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
    internal interface IJobParameter
    {
        Guid Id { get; }

        /// <summary>
        /// Null for immediate asynchronous jobs, not null for recurring jobs.
        /// </summary>
        string RecurringJobName { get; }

        string ExecuteAsUser { get; }

        /// <summary>
        /// The <see langword="null"/> value is considered <see langword="false"/>, to simplify job parameter serialization.
        /// </summary>
        bool? ExecuteAsAnonymous { get; set; }

        string GetLogInfo(Type executerType);
    }
}