﻿using System;
using System.Data;
using System.Data.SqlClient;
using SQLMigrationConstants;
using System.Windows.Forms;

namespace SQLMigration.Data
{
    public interface IDataAccess
    {
        DataTable GetDataTable(string sql);
        ConfigData ReadXML();
    }

    public class DataAccess : IDataAccess
    {
        public DataTable GetDataTable(string sql)
        {
            ConfigData configdata = new ConfigData();
            configdata = ReadXML();
            DBData param = new DBData();
            param = configdata.Source;
            Validate(param);
                        
            string strConnection = @"Server=" + param.serverName + ";Database=" + param.dbName + ";User Id=" + param.userName + ";Password=" + param.password + ";";
            SqlConnection con = new SqlConnection(strConnection);
            con.Open();

            SqlCommand sqlcmd = new SqlCommand();
            sqlcmd.Connection = con;
            sqlcmd.CommandText = sql;
         
            var dataReader = sqlcmd.ExecuteReader();
            var dataTable = new DataTable();
            if (dataReader.HasRows)
            {
               
                DataColumn column = new DataColumn("SchemaID", typeof(string));
                dataTable.Load(dataReader);
                
                column.AllowDBNull = false;
                dataTable.Columns.Add(column);
             
                foreach (DataRow row in dataTable.Rows)
                {
                    row["SchemaID"] = row["PK_Name"].GetHashCode().ToString().Replace("-","");
                 //   MessageBox.Show(row["SchemaID"].ToString());
                }
                

            }
            return dataTable;
        }

        public ConfigData ReadXML()
        {
            var reader = new System.Xml.Serialization.XmlSerializer(typeof(ConfigData));
            var file = new System.IO.StreamReader(GlobalConstant.configPath + @"\Config.xml");
            var overview = (ConfigData)reader.Deserialize(file);
            file.Close();

            return overview;
        }

        private static void Validate(DBData param)
        {
            if (param == null)
                throw new ArgumentNullException("param");

            if (String.IsNullOrWhiteSpace(param.dbName))
                throw new ArgumentNullException("param.dbName");

            if (String.IsNullOrWhiteSpace(param.password))
                throw new ArgumentNullException("param.password");

            if (String.IsNullOrWhiteSpace(param.serverName))
                throw new ArgumentNullException("param.serverName");

            if (String.IsNullOrWhiteSpace(param.userName))
                throw new ArgumentNullException("param.userName");
        }

    }


}
