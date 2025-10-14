using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace ChatApp.Server.DAL
{
    public static class IDataReaderExtensions
    {
        public static TResult[] ReadResultset<TResult>(this IDataReader reader, Func<TResult> rowSelector)
        {
            var resultSet = new List<TResult>();
            while (reader.Read()) { resultSet.Add(rowSelector()); }
            return resultSet.ToArray();
        }

        public static TResult ? ReadBestResultset<TResult>(this IDataReader reader, Func<TResult> prioritySetter)
        {
            TResult[] res = ReadResultset(reader, prioritySetter);
            if(res!=null && res.Length>0)
            {
                return res[0];
            }

            return default(TResult);
        }

        public static Guid GetGuid(this IDataReader reader, string name)
        {
            return (Guid)reader[name];
        }
        public static string GetString(this IDataReader reader, string name)
        {
            return (string)reader[name];
        }
        public static float GetFloat(this IDataReader reader, string name)
        {
            return (float)reader[name];
        }
        public static Decimal GetDecimal(this IDataReader reader, string name)
        {
            return (Decimal)reader[name];
        }
        public static Double GetDouble(this IDataReader reader, string name)
        {
            return (Double)reader[name];
        }
        public static Byte GetByte(this IDataReader reader, string name)
        {
            return (Byte)reader[name];
        }
        public static Byte[] GetBytes(this IDataReader reader, string name)
        {
            return (Byte[])reader[name];
        }
        public static Int16 GetInt16(this IDataReader reader, string name)
        {
            return (Int16)reader[name];
        }
        public static Int32 GetInt32(this IDataReader reader, string name)
        {
            return (Int32)reader[name];
        }
        public static Int64 GetInt64(this IDataReader reader, string name)
        {
            return (Int64)reader[name];
        }

        public static Single GetSingle(this IDataReader reader, string name)
        {
            return (Single)reader[name];
        }

        public static Boolean GetBoolean(this IDataReader reader, string name)
        {
            return (Boolean)reader[name];
        }
        public static DateTime GetDateTime(this IDataReader reader, string name)
        {
            return (DateTime)reader[name];
        }
        
        public static T GetEnum<T>(this IDataReader reader, string name)
           where T : struct, IComparable, IFormattable, IConvertible
        {
            return GetEnumCore<T>(reader[name]);
        }

        public static T GetEnum<T>(this IDataReader reader, int columnIndex)
           where T : struct, IComparable, IFormattable, IConvertible
        {
            return GetEnum<T>(reader, columnIndex);
        }

        private static T GetEnumCore<T>(object value)
            where T : struct, IComparable, IFormattable, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("'T' type parameter must be enum, but '" + typeof(T).FullName + "' was used instead.");
            }
            if (value == DBNull.Value || value == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            var enumValue = (T)Enum.ToObject(typeof(T), value);
            if (!Enum.IsDefined(typeof(T), enumValue))
            {
                throw new ArgumentOutOfRangeException();
            }
            return enumValue;
        }


        private static object ConvertDBNullToNull(IDataReader reader, string name)
        {
            object value = reader[name];
            return value == DBNull.Value ? null : value;
        }

        public static Guid? GetGuidNullable(this IDataReader reader, string name)
        {
            return (Guid?)ConvertDBNullToNull(reader, name);
        }
        public static string GetStringNullable(this IDataReader reader, string name)
        {
            return (string)ConvertDBNullToNull(reader, name);
        }
        public static float? GetFloatNullable(this IDataReader reader, string name)
        {
            return (float?)ConvertDBNullToNull(reader, name);
        }
        public static Decimal? GetDecimalNullable(this IDataReader reader, string name)
        {
            return (Decimal?)ConvertDBNullToNull(reader, name);
        }
        public static Double? GetDoubleNullable(this IDataReader reader, string name)
        {
            return (Double?)ConvertDBNullToNull(reader, name);
        }
        public static Byte? GetByteNullable(this IDataReader reader, string name)
        {
            return (Byte?)ConvertDBNullToNull(reader, name);
        }
        public static Byte[] GetBytesNullable(this IDataReader reader, string name)
        {
            return (Byte[])ConvertDBNullToNull(reader, name);
        }
        public static Int16? GetInt16Nullable(this IDataReader reader, string name)
        {
            return (Int16?)ConvertDBNullToNull(reader, name);
        }
        public static Int32? GetInt32Nullable(this IDataReader reader, string name)
        {
            return (Int32?)ConvertDBNullToNull(reader, name);
        }
        public static Int64? GetInt64Nullable(this IDataReader reader, string name)
        {
            return (Int64?)ConvertDBNullToNull(reader, name);
        }

        public static Single? GetSingleNullable(this IDataReader reader, string name)
        {
            return (Single?)ConvertDBNullToNull(reader, name);
        }

        public static Boolean? GetBooleanNullable(this IDataReader reader, string name)
        {
            return (Boolean?)ConvertDBNullToNull(reader, name);
        }
        public static DateTime? GetDateTimeNullable(this IDataReader reader, string name)
        {
            return (DateTime?)ConvertDBNullToNull(reader, name);
        }
        public static T? GetEnumNullable<T>(this IDataReader reader, string name)
           where T : struct, IComparable, IFormattable, IConvertible
        {
            var value = reader[name];
            return
                (value == DBNull.Value || value == null) ?
                    (T?)null :
                    GetEnumCore<T>(ConvertDBNullToNull(reader, name));
        }

    }
}
