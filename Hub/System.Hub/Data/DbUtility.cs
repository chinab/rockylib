using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Data;
using System.Data.OleDb;

namespace System.Data
{
    public static class DbUtility
    {
        #region Fields
        internal const string Separator = ",";
        internal const string Special = "'";
        #endregion

        #region Methods
        public static bool IsNullOrDBNull(object value)
        {
            return value == null || value == DBNull.Value;
        }

        public static bool TryToParams(string paramsString, out int[] array)
        {
            if (string.IsNullOrEmpty(paramsString))
            {
                array = new int[0];
                return false;
            }

            string[] sArray = paramsString.Split(Separator[0]);
            array = new int[sArray.Length];
            int value;
            for (int i = 0; i < sArray.Length; i++)
            {
                if (!int.TryParse(sArray[i], out value))
                {
                    return false;
                }
                array[i] = value;
            }
            return true;
        }
        public static bool TryToParams(string paramsString, out Guid[] array)
        {
            if (string.IsNullOrEmpty(paramsString))
            {
                array = new Guid[0];
                return false;
            }

            string[] sArray = paramsString.Split(Separator[0]);
            array = new Guid[sArray.Length];
            Guid value;
            for (int i = 0; i < sArray.Length; i++)
            {
                if (!Guid.TryParse(sArray[i], out value))
                {
                    return false;
                }
                array[i] = value;
            }
            return true;
        }

        /// <summary>
        /// 返回 GUID 用于数据库操作，特定的时间代码可以提高检索效率
        /// </summary>
        /// <returns>COMB (GUID 与时间混合型) 类型 GUID 数据</returns>
        public static Guid NewComb()
        {
            byte[] guidArray = Guid.NewGuid().ToByteArray();
            DateTime baseDate = new DateTime(1900, 1, 1);
            DateTime now = DateTime.Now;
            // Get the days and milliseconds which will be used to build the byte string 
            TimeSpan days = new TimeSpan(now.Ticks - baseDate.Ticks);
            TimeSpan msecs = new TimeSpan(now.Ticks - (new DateTime(now.Year, now.Month, now.Day).Ticks));
            // Convert to a byte array 
            // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333 
            byte[] daysArray = BitConverter.GetBytes(days.Days);
            byte[] msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));
            // Reverse the bytes to match SQL Servers ordering 
            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);
            // Copy the bytes into the guid 
            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);
            return new Guid(guidArray);
        }

        /// <summary>
        /// 从 SQL SERVER 返回的 GUID 中生成时间信息
        /// </summary>
        /// <param name="guid">包含时间信息的 COMB</param>
        /// <returns>时间</returns>
        public static DateTime GetDateFromComb(Guid guid)
        {
            DateTime baseDate = new DateTime(1900, 1, 1);
            byte[] daysArray = new byte[4];
            byte[] msecsArray = new byte[4];
            byte[] guidArray = guid.ToByteArray();
            // Copy the date parts of the guid to the respective byte arrays. 
            Array.Copy(guidArray, guidArray.Length - 6, daysArray, 2, 2);
            Array.Copy(guidArray, guidArray.Length - 4, msecsArray, 0, 4);
            // Reverse the arrays to put them into the appropriate order 
            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);
            // Convert the bytes to ints
            return baseDate.AddDays(BitConverter.ToInt32(daysArray, 0)).AddMilliseconds(BitConverter.ToInt32(msecsArray, 0) * 3.333333);
        }

        public static DataTable ReadExcel(string filePath, ExcelVersion excelType)
        {
            string connStr;
            switch (excelType)
            {
                case ExcelVersion.Excel2007:
                    connStr = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 12.0;HDR=YES;'", filePath);
                    break;
                default:
                    connStr = string.Format("Provider=Microsoft.Jet.OleDb.4.0;Data Source={0};Extended Properties='Excel 8.0;HDR=Yes;IMEX=1'", filePath);
                    break;
            }
            OleDbConnection conn = new OleDbConnection(connStr);
            conn.Open();
            DataTable table = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            conn.Close();
            DataTable dt = new DataTable();
            if (table.Rows.Count > 0)
            {
                OleDbDataAdapter da = new OleDbDataAdapter(string.Empty, conn);
                foreach (DataRow dr in table.Rows)
                {
                    string sheetName = dr[2].ToString();
                    da.SelectCommand.CommandText = "Select * From [" + sheetName + "]";
                    da.Fill(dt);
                }
            }
            return dt;
        }

        public enum ExcelVersion
        {
            Excel2003, Excel2007
        }

        internal static string GetFormat(string formatSql, object[] paramValues)
        {
            var sb = new StringBuilder();
            AppendParamsValue(sb, paramValues);
            string[] args = sb.ToString().Split(',');
            sb.Length = 0;
            sb.AppendFormat(formatSql, args);
            return sb.ToString();
        }

        private static void AppendParamsValue(StringBuilder buffer, IEnumerable enumerable)
        {
            IEnumerator tor = enumerable.GetEnumerator();
            if (!tor.MoveNext())
            {
                throw new InvalidOperationException("The enumerator's empty.");
            }

            buffer.AppendValue(tor.Current);
            while (tor.MoveNext())
            {
                buffer.Append(Separator).AppendValue(tor.Current);
            }
        }
        #endregion

        #region Extensions
        internal static StringBuilder AppendValue(this StringBuilder buffer, object value)
        {
            if (DbUtility.IsNullOrDBNull(value))
            {
                buffer.Append("NULL");
            }
            else
            {
                Type type = value.GetType();
                type = Nullable.GetUnderlyingType(type) ?? type;
                string sValue = value.ToString();
                if (type.IsPrimitive)
                {
                    if (type == typeof(bool))
                    {
                        buffer.Append(sValue == bool.FalseString ? "0" : "1");
                    }
                    else
                    {
                        buffer.Append(sValue);
                    }
                }
                else
                {
                    if (type == typeof(DateTime) || type == typeof(Guid))
                    {
                        buffer.Append(DbUtility.Special).Append(sValue).Append(DbUtility.Special);
                    }
                    else
                    {
                        buffer.Append(DbUtility.Special).Append(sValue.Replace(DbUtility.Special, string.Empty)).Append(DbUtility.Special);
                    }
                }
            }
            return buffer;
        }
        #endregion
    }
}