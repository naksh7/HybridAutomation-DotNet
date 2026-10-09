using Microsoft.Data.SqlClient;

namespace HybridAutomation.Helpers
{
    public class SQL
    {
        /// <summary>
        /// Executes provided SQL Command for the database
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="databaseName"></param>
        /// <param name="userID"></param>
        /// <param name="password"></param>
        /// <param name="sqlCommand"></param>
        /// <returns>True if rows afftected are greater than 0</returns>
        /// <exception cref="Exception"></exception>
        public bool ExecuteSQL(string serverName, string databaseName, string userID, string password, string sqlCommand)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = serverName;
                builder.UserID = userID;
                builder.Password = password;
                builder.InitialCatalog = databaseName;
                SqlConnection connection = new SqlConnection(builder.ConnectionString);
                connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = sqlCommand;
                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    Utilities.Logger.Log(Logger.LogType.Info, $"{rowsAffected} rows affected with the provided script on Server : {serverName} | Database :{databaseName}");
                    return true;
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Warning, $"No row affetcted with the provided query on Server : {serverName} | Database :{databaseName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nExecuteSQL failed for : {sqlCommand}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Executes provided SQL Command for the database using Windows Authentication
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="databaseName"></param>
        /// <param name="sqlCommand"></param>
        /// <returns>True if rows afftected are greater than 0</returns>
        /// <exception cref="Exception"></exception>
        public bool ExecuteSQLWithWindowsAuth(string serverName, string databaseName, string sqlCommand)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = serverName,
                    InitialCatalog = databaseName,
                    IntegratedSecurity = true,
                    Encrypt = true, // Explicitly set encryption
                    TrustServerCertificate = true // Bypass certificate validation for test environments
                };
                using var connection = new SqlConnection(builder.ConnectionString);
                connection.Open();
                using var cmd = new SqlCommand(sqlCommand, connection);
                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected >= 0)
                {
                    Utilities.Logger.Log(Logger.LogType.Info, $"{rowsAffected} rows affected with the provided script on Server : {serverName} | Database :{databaseName}");
                    return true;
                }
                else
                {
                    Utilities.Logger.Log(Logger.LogType.Warning, $"No row affected with the provided query on Server : {serverName} | Database :{databaseName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nExecuteSQLWithWindowsAuth failed for : {sqlCommand}\n{ex.StackTrace}", ex);
            }
        }
    }
}