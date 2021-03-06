﻿using SQLMigration.Data;
using SQLMigrationConverter.MapAttribut;
using SQLMigrationConverter.Template;
using SQLMigrationInterface;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace SQLMigrationManager
{

    public class UDTManager : IManager
    {
        private cUDT cudt;
        readonly DataAccess dataAccess = new DataAccess();
       
        public void GetSchema() 
        {
            var ds = new DataSet();
            var infoQuery = new InfoQuery();
            var configdata = dataAccess.ReadXML();

            var dt = dataAccess.GetDataTable(infoQuery.GetUdts());
            ds.Tables.Add(dt);
            ds.WriteXml(configdata.Path + "UDTSchema.xml");

            MessageBox.Show("UDT sql Schema created " + configdata.Path + "UDTSchema.xml");
           }
     

        public void Convert()
        {
            var configdata = dataAccess.ReadXML();
            IInfoQuery infoQuery = new InfoQuery();
            IInfo info = new Info(infoQuery);
            cudt = new cUDT(info, infoQuery);
            var result = cudt.CreateScript();

            var file = configdata.Path + configdata.Destination;
           
            if (Directory.Exists(Path.GetDirectoryName(file)))
            {
                File.Delete(file);
            }
            using (var sw = File.CreateText(file))
            {
                sw.Write(result);
            }
            MessageBox.Show("UDT_pgSQL created " + configdata.Path + configdata.Destination);

        }

        public void SetConfig(SetData setdata)
        {
            var configdata = new ConfigData();
            var param = new DBData()
            {
                dbName = setdata.dbName,
                serverName = setdata.serverName,
                password = setdata.passSQL,
                userName = setdata.usernameSQL,

            };
            configdata.Destination = setdata.destination;
            configdata.Path = setdata.path;
            configdata.Id = setdata.id;
            configdata.Source = param;

            WriteConfig(configdata);
        }

        private static void WriteConfig(ConfigData configdata)
        {
            var getdata = new ConfigData
            {
                Destination = configdata.Destination,
                Id = configdata.Id,
                Source = configdata.Source,
                Path = configdata.Path
            };

            var writer = new System.Xml.Serialization.XmlSerializer(typeof(ConfigData));
            var file = File.Create(getdata.Path + "UDTConfig.xml");

            writer.Serialize(file, getdata);
            MessageBox.Show("config Data created on " + getdata.Path + "UDTConfig.xml");

            file.Close();
        }
    }
}
