﻿using System;
using System.Collections.Generic;
using System.Data;
//ing System.Data.OracleClient;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using System.Text;

namespace VAdvantage.DataBase
{
    class DB_Oracle : ViennaDatabase
    {

        private string connectionString = null;
        private System.Data.IDbConnection con = null;


        public int GetStandardPort()
        {
            return 1541;
        }

        public bool SupportsBLOB()
        {
            return true;
        }

        public string ConvertStatement(string oraStatement)
        {
            return oraStatement;
        }

        public string TO_DATE(DateTime? time, bool dayOnly)
        {
            StringBuilder dateString = new StringBuilder("");
            string myDate = "";

            if (time == null)
            {
                if (dayOnly)
                    return "TRUNC(SysDate)";
                return "SysDate";
            }

            dateString = new StringBuilder("TO_DATE('");
            //  YYYY-MM-DD HH24:MI:SS.mmmm  JDBC Timestamp format
            //String myDate = time.ToString("yyyy-mm-dd");
            //myDate = time.ToString("yyyy-MM-dd HH:mm:ss");
            myDate = time.Value.ToString();//"yyyy-MM-dd");
            if (dayOnly)
            {
                myDate = time.Value.ToString("yyyy-MM-dd");
                dateString.Append(myDate);
                dateString.Append("','YYYY-MM-DD')");
            }
            else
            {
                myDate = time.Value.ToString("yyyy-MM-dd HH:mm:ss");
                dateString.Append(myDate);	//	cut off miliseconds
                dateString.Append("','YYYY-MM-DD HH24:MI:SS')");
            }
            return dateString.ToString();
        }

        public string TO_CHAR(string columnName, int displayType, string AD_Language)
        {
            StringBuilder retValue = new StringBuilder("TRIM(TO_CHAR(");

            retValue.Append(columnName);

            //  Numbers
            if (VAdvantage.Classes.DisplayType.IsNumeric(displayType))
            {
                if (displayType == VAdvantage.Classes.DisplayType.Amount)
                    retValue.Append(",'9G999G999G999G990D00'");
                else
                    retValue.Append(",'TM9'");
                //  TO_CHAR(GrandTotal,'9G999G990D00','NLS_NUMERIC_CHARACTERS='',.''')
                //if (!Language.isDecimalPoint(AD_Language))      //  reversed
                //    retValue.append(",'NLS_NUMERIC_CHARACTERS='',.'''");
            }
            else if (VAdvantage.Classes.DisplayType.IsDate(displayType))
            {
                retValue.Append(",'")
                    .Append("yyyy-MM-dd")
                    .Append("'");
            }
            retValue.Append("))");
            //
            return retValue.ToString();
        }

        public string TO_NUMBER(decimal? number, int displayType)
        {
            if (number == null)
                return "NULL";
            Decimal result = number.Value;
            //int scale = VAdvantage.Classes.DisplayType.GetDefaultPrecision(displayType);
            //if (scale > Decimal.  number.Value.())
            //{
            //    try
            //    {
            //        result = number.setScale(scale, BigDecimal.ROUND_HALF_UP);
            //    }
            //    catch (Exception e)
            //    {
            //        //  log.severe("Number=" + number + ", Scale=" + " - " + e.getMessage());
            //    }
            //}
            return result.ToString();
        }

        //public int GetNextID(string Name)
        //{
        //    throw new NotImplementedException();
        //}

        public bool CreateSequence(string name, int increment, int minvalue, int maxvalue, int start, Trx trxName)
        {

            int no = DB.ExecuteQuery("DROP SEQUENCE " + name.ToUpper() + "_SEQ", null, trxName);
            no = DB.ExecuteQuery("CREATE SEQUENCE " + name.ToUpper() + "_SEQ"
                                + " MINVALUE " + minvalue
                                + " MAXVALUE " + maxvalue
                                + " START WITH " + start
                                + " INCREMENT BY " + increment + " CACHE 20", null, trxName)
                                ;

            if (no == -1)
                return false;
            else
                return true;
            //if (no == -1 && Common.Common.LenghtyTableNames.IndexOf(name.ToUpper()) > -1)
            //{
            //    name = name.Substring(0, 25);

            //    DB.ExecuteQuery("DROP SEQUENCE " + name.ToUpper()+ "_SEQ", null, trxName);

            //    no = DB.ExecuteQuery("CREATE SEQUENCE " + name.ToUpper()+ "_SEQ"
            //                  + " MINVALUE " + minvalue
            //                  + " MAXVALUE " + maxvalue
            //                  + " START WITH " + start
            //                  + " INCREMENT BY " + increment + " CACHE 20", null, trxName);
            //    if (no == -1)
            //        return false;
            //    else
            //        return true;
            //}
            //else
            //    return true;
        }

        public string GetName()
        {
            return DatabaseType.DB_ORACLE;
        }




        public System.Data.IDbConnection GetCachedConnection(bool autoCommit, int transactionIsolation)
        {
            if (con == null)
            {
                con = new OracleConnection(connectionString);
            }
            return con;
        }

        public void SetConnectionString(string conString)
        {
            connectionString = conString;
        }


        public System.Data.DataSet ExecuteDatasetPaging(string sql, int page, int pageSize, int increment)
        {
            DataSet ds = null;
            OracleConnection connection = new OracleConnection(connectionString);
            try
            {

                //                sql = @"SELECT * FROM (
                //                     
                //                         SELECT a.*, rownum row_num
                //                         FROM
                //                              ( " + sql + @" ) a
                //                            
                //                            WHERE rownum < ((" + page + @" * " + pageSize + @") + 1 )
                //                        )
                //                        WHERE row_num >= (((" + page + @"-1) * " + pageSize + @") + 1)";

                // int index = sql.ToLower().IndexOf("select");

                // sql = sql.Insert(index + 6, " rownum as r, ");

                //sql = @"select * from (" + sql + ") t where r between  (((" + page + @" - 1) * " + pageSize + @") + 1) AND ((" + page + @" * " + pageSize + @") + 1 )";


                sql = "select * FROM (SELECT t.*, rownum AS row_num FROM (" + sql + ") t ) WHERE row_num BETWEEN (((" + page + " - 1) * " + pageSize + ") + 1) AND ((" + page + " * " + pageSize + "))";

                connection.Open();
                OracleDataAdapter adapter = new OracleDataAdapter();
                adapter.SelectCommand = new OracleCommand(sql);
                adapter.SelectCommand.Connection = (OracleConnection)connection;
                ds = new DataSet();

                if (page < 1)// Set rowcount =PageNumber * PageSize for best performance
                {
                    page = 1;
                }
                //adapter.Fill(ds, ((page - 1) * pageSize) + increment, pageSize - increment, "Data");

                adapter.Fill(ds);

                if (ds != null && ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Columns.IndexOf("row_num") > -1)
                    {
                        ds.Tables[0].Columns.Remove("row_num");
                    }

                }

                //adapter.FillSchema(ds, SchemaType.Mapped, "DataSchema");

                //if (ds != null && ds.Tables.Count > 1)
                //{
                //    DataTable data = ds.Tables["Data"];
                //    DataTable schema = ds.Tables["DataSchema"];
                //    data.Merge(schema);
                //    ds.Tables.Remove(schema);
                //}
            }
            catch (Exception e)
            {
                //
                VAdvantage.Logging.VLogger.Get().Severe(e.Message + " [Query NQTrx]" + sql);
                ds = null;
            }
            finally
            {
                connection.Close();
            }
            return ds;
        }

        public static int GetNextID(String Name)
        {
            int m_sequence_id = DB.GetSQLValue(null, "SELECT " + Name.ToUpper() + ".nextval FROM DUAL");
            return m_sequence_id;
        }

    }
}
