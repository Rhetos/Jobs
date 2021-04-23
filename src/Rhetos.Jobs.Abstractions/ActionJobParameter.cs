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

using Newtonsoft.Json;

namespace Rhetos.Jobs
{
    /// <summary>
    /// Job parameters for <see cref="ActionJobExecuter"/>.
    /// </summary>
    public class ActionJobParameter
    {
        public ActionJobParameter()
        {
            // Default constructor for job queue serialization.
        }

        public ActionJobParameter(object action)
        {
            ActionName = action.GetType().FullName;
            ActionParameters = action;
        }

        /// <summary>
        /// Full name of the DSL Action, format "ModuleName.ActionName".
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Action parameter is an instance of the action C# type (same name as action).
        /// It can be null.
        /// </summary>
        public object ActionParameters { get; set; }

        /// <summary>
        /// Used for logging.
        /// </summary>
        public override string ToString()
        {
            return $"Action: {ActionName}|Parameters: {JsonConvert.SerializeObject(ActionParameters)}";
        }
    }
}