using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ChatApp.Server.DAL
{
    partial class SqlServerDAL
    {
        private static class _impl
        {
            private static TResult ExecCmdImpl<TResult>
            (
                string connectionString,
                CommandType commandType,
                string query,
                Dictionary<string, Object> parameters,
                Func<IDbCommand, TResult> executeAndReaderResult
            )
            {
                return ExecImpl
                (
                    connectionString,
                    execute: con =>
                    (
                    ExecCmdImpl
                    (
                        con: con,
                        commandType: commandType,
                        query: query,
                        parameters: parameters,
                        executeAndReaderResult: executeAndReaderResult
                    )
                    )
                );
            }

            private static TResult ExecImpl<TResult>(string connectionString, Func<SqlConnection, TResult> execute)
            {
                using (var con = new SqlConnection(connectionString: connectionString))
                {
                    con.Open();
                    return execute(con);
                }
            }

            private static TResult ExecCmdImpl<TResult>
            (
                SqlConnection con,
                CommandType commandType,
                string query,
                Dictionary<string, Object> parameters,
                Func<IDbCommand, TResult> executeAndReaderResult
            )
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds;
                    cmd.CommandType = commandType switch
                    {
                        CommandType.Text => System.Data.CommandType.Text,
                        CommandType.StoredProcedure => System.Data.CommandType.StoredProcedure,
                        _ => throw new NotImplementedException()
                    };
                    cmd.CommandText = query;
                    foreach (var parameter in parameters)
                    {
                        if (parameter.Value == null)
                        {

                        }
                        cmd.Parameters.AddWithValue
                        (
                            parameterName: (!parameter.Key.StartsWith("@") ? "@" : "") + parameter.Key,
                            value: parameter.Value != null ? parameter.Value : DBNull.Value
                        );

                    }
                    return executeAndReaderResult(cmd);
                }
            }

            public static TResult ExecCmdWithParamsAndResult<TResult>
            (
                string connectionString,
                string query,
                CommandType commandType,
                Dictionary<string, Object> parameters,
                Func<IDataReader, TResult> readFromReader
            )
            {
                return ExecCmdImpl
                (
                    connectionString: connectionString,
                    commandType: commandType,
                    query: query,
                    parameters: parameters,
                    executeAndReaderResult: cmd =>
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            return readFromReader(reader);
                        }
                    }
                );
            }

            public static void ExecCmd
            (
                string connectionString,
                CommandType commandType,
                string query
            )
            {
                ExecCmdImpl
                (
                    connectionString: connectionString,
                    query: query,
                    commandType: commandType,
                    parameters: new Dictionary<string, Object>(),
                    executeAndReaderResult: cmd =>
                    {
                        cmd.ExecuteNonQuery();
                        return 0;
                    }
                );
            }
            public static void ExecCmdWithParams
            (
                string connectionString,
                CommandType commandType,
                string query,
                Dictionary<string, Object> parameters
            )
            {
                ExecCmdImpl
                (
                    connectionString: connectionString,
                    query: query,
                    commandType: commandType,
                    parameters: parameters,
                    executeAndReaderResult: cmd =>
                    {
                        cmd.ExecuteNonQuery();
                        return 0;
                    }
                );
            }

            public static TResult ExecCmdWithResult<TResult>
            (
                string connectionString,
                string query,
                CommandType commandType,
                Func<IDataReader, TResult> readFromReader
            )
            {
                return ExecCmdImpl
                (
                    connectionString: connectionString,
                    commandType: commandType,
                    query: query,
                    parameters: new Dictionary<string, Object>(),
                    executeAndReaderResult: cmd =>
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            return readFromReader(reader);
                        }
                    }
                );
            }
        }
    }

}