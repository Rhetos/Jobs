// using System;
// using System.Data.SqlClient;
// using Rhetos.Logging;
// using Rhetos.Utilities;
// using TableDependency.SqlClient;
// using TableDependency.SqlClient.Base.EventArgs;
//
// namespace Rhetos.Jobs
// {
// 	public class AsyncActionListener
// 	{
// 		private readonly ConnectionString _connectionString;
// 		private readonly ILogger _logger;
//
// 		public AsyncActionListener(ConnectionString connectionString, ILogProvider logProvider)
// 		{
// 			_connectionString = connectionString;
// 			_logger = logProvider.GetLogger("SqlListener");
// 		}
//
// 		public void RegisterNotification()
// 		{
// 			SqlDependency.Start(_connectionString);
//
// 			var commandText = "SELECT ID, Name From Common.Principal";
//
// 			using (SqlConnection connection = new SqlConnection(_connectionString))
// 			{
// 				using (SqlCommand command = new SqlCommand(commandText, connection))
// 				{
// 					connection.Open();
// 					var sqlDependency = new SqlDependency(command);
//
//
// 					sqlDependency.OnChange += SqlDependencyOnOnChange;
//
// 					// NOTE: You have to execute the command, or the notification will never fire.
// 					using (SqlDataReader reader = command.ExecuteReader())
// 					{
// 					}
// 				}
// 			}
//
//
// 			using (var dep = new SqlTableDependency<Principal>(_connectionString, "Principal", "Common"))
// 			{
// 				dep.OnChanged += DepOnOnChanged;
// 				dep.Start();
// 			}
//
// 		}
//
// 		private void DepOnOnChanged(object sender, RecordChangedEventArgs<Principal> e)
// 		{
// 			_logger.Info($"{e.ChangeType}: Name: {e.Entity.Name}");
// 		}
//
// 		class Principal
// 		{
// 			public Guid ID { get; set; }
// 			public string Name { get; set; }
// 		}
//
//
// 		private void SqlDependencyOnOnChange(object sender, SqlNotificationEventArgs e)
// 		{
// 			_logger.Info($"{e.Source}");
// 			RegisterNotification();
// 		}
// 	}
// }