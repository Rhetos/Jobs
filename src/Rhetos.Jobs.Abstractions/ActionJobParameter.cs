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