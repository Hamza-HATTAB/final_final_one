using System;
using System.Diagnostics;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using DataGridNamespace; // Add if not present

namespace DataGrid.Models
{
	public class DatabaseConnection
	{
		// Updated to use AppConfig.CloudSqlConnectionString
		private static string connectionString = AppConfig.CloudSqlConnectionString;
		private static int commandTimeout = 60;       // Command timeout in seconds
		private static int maxRetries = 3;            // Maximum number of connection retry attempts
		private static int retryDelayMs = 1000;       // Delay between retries in milliseconds

		/// <summary>
		/// Gets the connection string used for database operations
		/// </summary>
		public static string GetConnectionString() 
		{
			return AppConfig.CloudSqlConnectionString;
		}

		/// <summary>
		/// Sets the connection string used for database operations
		/// </summary>
		public static void SetConnectionString(string newConnectionString)
		{
			if (string.IsNullOrEmpty(newConnectionString))
			{
				throw new ArgumentException("Connection string cannot be empty");
			}
			
			connectionString = newConnectionString;
		}

		/// <summary>
		/// Tests the connection to the database
		/// </summary>
		/// <returns>True if connection successful, false otherwise</returns>
		public static bool TestConnection()
		{
			try
			{
				using (MySqlConnection conn = new MySqlConnection(connectionString))
				{
					conn.Open();
					Debug.WriteLine("Database connection successful!");
					return true;
				}
			}
			catch (MySqlException sqlEx)
			{
				Debug.WriteLine($"MySQL database connection error: {sqlEx.Message}");
				Debug.WriteLine($"MySQL Error Code: {sqlEx.Number}");
				
				if (sqlEx.InnerException != null)
				{
					Debug.WriteLine($"Inner exception: {sqlEx.InnerException.Message}");
				}
				return false;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"General database connection error: {ex.Message}");
				if (ex.InnerException != null)
				{
					Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
				}
				return false;
			}
		}
		
		/// <summary>
		/// Creates and returns a new database connection
		/// </summary>
		/// <returns>Open MySqlConnection</returns>
		public static MySqlConnection CreateConnection()
		{
			var connection = new MySqlConnection(connectionString);
			// ConnectionTimeout is read-only and set through the connection string
			
			for (int attempt = 1; attempt <= maxRetries; attempt++)
			{
				try
				{
					connection.Open();
					return connection;
				}
				catch (MySqlException ex) when (ex.Number == 1040 || ex.Number == 2002 || ex.Number == 2003 || ex.Number == 2006 || ex.Number == 2013)
				{
					// These error codes typically indicate connection issues that might resolve with a retry
					if (attempt < maxRetries)
					{
						Debug.WriteLine($"Connection attempt {attempt} failed: {ex.Message}. Retrying...");
						Thread.Sleep(retryDelayMs);
					}
					else
					{
						Debug.WriteLine($"Connection failed after {maxRetries} attempts: {ex.Message}");
						throw;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error creating database connection: {ex.Message}");
					throw; // Rethrow to be handled by caller
				}
			}
			
			// This should never be reached due to the loop structure, but the compiler needs it
			throw new Exception($"Failed to establish database connection after {maxRetries} attempts");
		}
		
		/// <summary>
		/// Creates a command with proper timeout settings and parameters
		/// </summary>
		public static MySqlCommand CreateCommand(string commandText, MySqlConnection connection, params MySqlParameter[] parameters)
		{
			var cmd = new MySqlCommand(commandText, connection);
			cmd.CommandTimeout = commandTimeout;
			
			if (parameters != null && parameters.Length > 0)
			{
				cmd.Parameters.AddRange(parameters);
			}
			
			return cmd;
		}
		
		/// <summary>
		/// Executes a SQL query and returns a dictionary list of results
		/// </summary>
		public static List<Dictionary<string, object>> ExecuteQuery(string query, params MySqlParameter[] parameters)
		{
			var results = new List<Dictionary<string, object>>();
			
			using (var connection = CreateConnection())
			{
				using (var command = CreateCommand(query, connection, parameters))
				{
					try
					{
						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								var row = new Dictionary<string, object>();
								
								for (int i = 0; i < reader.FieldCount; i++)
								{
									string columnName = reader.GetName(i);
									object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
									row[columnName] = value;
								}
								
								results.Add(row);
							}
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Error executing query: {ex.Message}");
						throw;
					}
				}
			}
			
			return results;
		}

		/// <summary>
		/// Executes a SQL query that returns a single result
		/// </summary>
		public static object ExecuteScalar(string query, params MySqlParameter[] parameters)
		{
			using (var connection = CreateConnection())
			{
				using (var command = CreateCommand(query, connection, parameters))
				{
					try
					{
						return command.ExecuteScalar();
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Error executing scalar query: {ex.Message}");
						throw;
					}
				}
			}
		}
		
		/// <summary>
		/// Executes a non-query SQL command (INSERT, UPDATE, DELETE)
		/// </summary>
		public static int ExecuteNonQuery(string query, params MySqlParameter[] parameters)
		{
			using (var connection = CreateConnection())
			{
				using (var command = CreateCommand(query, connection, parameters))
				{
					try
					{
						return command.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Error executing non-query: {ex.Message}");
						throw;
					}
				}
			}
		}
	}
}