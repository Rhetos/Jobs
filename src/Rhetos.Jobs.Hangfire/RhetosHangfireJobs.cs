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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;

namespace Rhetos.Jobs.Hangfire
{
    /// <summary>
    /// Helper class for accessing Hangfire jobs information.
    /// </summary>
    public class RhetosHangfireJobs
	{
        private readonly ISqlExecuter _sqlExecuter;
        private readonly ISqlUtility _sqlUtility;

        public RhetosHangfireJobs(ISqlExecuter sqlExecuter, ISqlUtility sqlUtility)
        {
            _sqlExecuter = sqlExecuter;
            _sqlUtility = sqlUtility;
        }

        /// <remarks>
        /// Only recurring jobs have name.
        /// </remarks>
        public void InsertJobConfirmation(Guid id, string name)
        {
            string commmand =
$@"IF NOT EXISTS (SELECT TOP 1 1 FROM Common.HangfireJob WITH (READCOMMITTEDLOCK, ROWLOCK) WHERE ID = '{id}')
    INSERT INTO Common.HangfireJob (ID, Name) VALUES ('{id}', {_sqlUtility.QuoteText(name)})";
            _sqlExecuter.ExecuteSql(commmand);
        }

        public bool JobConfirmationExists(Guid jobId)
		{
			var command = $"SELECT COUNT(1) FROM Common.HangfireJob WITH (READCOMMITTEDLOCK, ROWLOCK) WHERE ID = '{jobId}'";
			var count = 0;

			_sqlExecuter.ExecuteReader(command, reader => count = reader.GetInt32(0));

			//If transaction that created job failed there will be no job scheduled and count will be zero
			return count > 0;
		}

		public void DeleteJobConfirmation(Guid jobId)
		{
			var command = $"DELETE FROM Common.HangfireJob WHERE ID = '{jobId}'";
			_sqlExecuter.ExecuteSql(command);
		}

        /// <remarks>
        /// Only recurring jobs have name.
        /// </remarks>
        public Guid? GetJobId(string name)
        {
            Guid? id = null;
            var command = $"SELECT ID FROM Common.HangfireJob WITH (READCOMMITTEDLOCK) WHERE Name = {_sqlUtility.QuoteText(name)}";
            _sqlExecuter.ExecuteReader(command, reader => id = reader.GetGuid(0));
            return id;
        }

        /// <remarks>
        /// Only recurring jobs have name.
        /// </remarks>
        public IEnumerable<string> GetJobNames()
        {
            var names = new List<string>();
            var command = $"SELECT Name FROM Common.HangfireJob WITH (READCOMMITTEDLOCK) WHERE Name IS NOT NULL";
            _sqlExecuter.ExecuteReader(command, reader => names.Add(reader.GetString(0)));
            return names;
        }
    }
}
