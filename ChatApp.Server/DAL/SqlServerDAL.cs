using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.ServiceProcess;
using System.Diagnostics;
using System.Reflection;
using System.Data.OleDb;

namespace ChatApp.Server.DAL
{
    public partial class SqlServerDAL
    {
        public enum CommandType { Text, StoredProcedure }
        
        private readonly string _connectionString;
        public SqlServerDAL(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) { throw new ArgumentException(); }
            this._connectionString = connectionString;
        }
        public TResult ExecCmdWithParamsAndResult<TResult>
        (
            CommandType commandType,
            string query,
            Dictionary<string, Object> parameters,
            Func<IDataReader, TResult> readFromReader
        )
        {
            return _impl.ExecCmdWithParamsAndResult
            (
                connectionString: _connectionString,
                query: query,
                commandType: commandType,
                parameters: parameters,
                readFromReader: readFromReader
            );
        }
        public void ExecCmd
        (
            CommandType commandType,
            string query
        )
        {
            _impl.ExecCmd
                (
                    connectionString: _connectionString,
                    query: query,
                    commandType: commandType
                );
        }

        public void ExecCmdWithParams
        (
            CommandType commandType,
            string query,
            Dictionary<string, Object> parameters
        )
        {
            _impl.ExecCmdWithParams
            (
                connectionString: _connectionString,
                query: query,
                commandType: commandType,
                parameters: parameters
            );
        }

        public TResult ExecCmdWithResult<TResult>
        (
            CommandType commandType,
            string query,
            Func<IDataReader, TResult> readFromReader
        )
        {
            return _impl.ExecCmdWithResult
            (
                connectionString: _connectionString,
                query: query,
                commandType: commandType,
                readFromReader: readFromReader
            );
        }

    }

}