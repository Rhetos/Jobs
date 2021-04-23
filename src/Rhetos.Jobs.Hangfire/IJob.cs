using System;

namespace Rhetos.Jobs.Hangfire
{
    public interface IJob
    {
        Guid Id { get; }
        string ExecuteAsUser { get; }

        string GetLogInfo(Type executerType);
    }
}