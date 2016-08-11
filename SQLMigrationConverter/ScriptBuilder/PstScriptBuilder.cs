﻿
using SQLMigration.Converter.ScriptBuilder;
using SQLMigration.Data.SchemaInfo;
using SQLMigrationInterface.Interface.ScriptBuilder;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SQLMigration.Converter.ScriptBuilder
{
    public class PstScriptBuilder : IScriptBuilder
    {

        class TablesFieldDataType
        {
            public string DataType { get; set; }
            public string ConvertedDataType { get; set; }
        }

        
        public string CreateScriptUDT(UDTSchemaInfoData schemaInfo)
        {
            
            string result;

            var convertedDataType = GetDataTypeMap(schemaInfo.DataType);
            if (schemaInfo.Scale > 0)
            {
                result = string.Format("CREATE DOMAIN {0} AS {1}({2},{3}){4};\r\n",
                                schemaInfo.name, convertedDataType, schemaInfo.Precision,
                                schemaInfo.Scale, (schemaInfo.IsNullable ? "" : " NOT NULL"));

            }
            else if (schemaInfo.Precision == 0)
            {
                result = string.Format("CREATE DOMAIN {0} AS {1}({2}){3};\r\n",
                        schemaInfo.name, convertedDataType, schemaInfo.MaxLength,
                        (schemaInfo.IsNullable ? "" : " NOT NULL"));

            }
            else
            {
                result = string.Format("CREATE DOMAIN {0} AS {1}{2};\r\n",
                        schemaInfo.name, convertedDataType,
                        (schemaInfo.IsNullable ? "" : " NOT NULL"));
            }


            Console.WriteLine("PstScriptBuilder.CreateScriptUDT : " + schemaInfo.name + ", Done");

            return result;

        }

        public string CreateScriptTable(TableSchemaInfoData schemaInfo)
        {
            string result;
            var usedColum = "";
            var n = 0;
        
            foreach (var eachColumn in schemaInfo.usedColumnList)
            {
                n = schemaInfo.usedColumnList.IndexOf(eachColumn);

                if ((n + 1) == schemaInfo.usedColumnList.Count)
                {
                    usedColum += eachColumn.ColumnName + "  " +  cekSuffix(eachColumn) + "\r\n";
                }
                else
                {
                    usedColum += eachColumn.ColumnName + "  " +  cekSuffix(eachColumn) + ",\r\n";
                }
            }
            result = string.Format("CREATE TABLE {0} (\r\n {1} );\r\n",
                                schemaInfo.TableName, usedColum);
         
           

            Console.WriteLine("PstScriptBuilder.CreateScriptTable : " + schemaInfo.name + ", Done");
            return result;
        }

        public string CreateScriptPK(PKSchemaInfoData schemaInfo)
        {
            string result;
            var usedColum = "";
            var n = 0;

            foreach (var eachColumn in schemaInfo.usedColumnList)
            {
                n = schemaInfo.usedColumnList.IndexOf(eachColumn);

                if ((n + 1) == schemaInfo.usedColumnList.Count)
                {
                    usedColum += eachColumn.ColumnName;
                }
                else
                {
                    usedColum += eachColumn.ColumnName + ", ";
                }
            }

            result = string.Format("ALTER TABLE {0} ADD CONSTRAINT {1} PRIMARY KEY ({2});\r\n",
                                   schemaInfo.TableName, schemaInfo.PkName, usedColum);
                     

            Console.WriteLine("PstScriptBuilder.CreateScriptUDT : " + schemaInfo.name + ", Done");

            return result;

        }

        public string CreateScriptIndex(IndexSchemaInfoData schemaInfo)
        {
            string result;
            var usedColum = "";
            var n = 0;

            foreach (var eachColumn in schemaInfo.usedColumnList)
            {
                n = schemaInfo.usedColumnList.IndexOf(eachColumn);

                if ((n + 1) == schemaInfo.usedColumnList.Count)
                {
                    usedColum += eachColumn.ColumnName;
                }
                else
                {
                    usedColum += eachColumn.ColumnName + ", ";
                }
            }


            result = string.Format("CREATE INDEX {0} ON {1} ({2});\r\n",
                         schemaInfo.IndexName, schemaInfo.TableName, usedColum);              
           

            Console.WriteLine("PstScriptBuilder.CreateScriptIndex : " + schemaInfo.name + ", Done");

            return result;

        }

        public string CreateScriptSP2(SPSchemaInfoData schemaInfo)
        {
            string result;
            var usedParameter = "";
            var n = 0;

            foreach (var eachParameter in schemaInfo.usedParameterList)
            {
                n = schemaInfo.usedParameterList.IndexOf(eachParameter);

                if ((n + 1) == schemaInfo.usedParameterList.Count)
                {
                    usedParameter += eachParameter.ParameterName + "  " + cekParameter(eachParameter) + "\r\n";
                }
                else
                {
                    usedParameter += eachParameter.ParameterName + "  " + cekParameter(eachParameter) + ",\r\n";
                }
            }
            result = string.Format("CREATE OR REPLACE FUNCTION {0} (\r\n {1} )\r\n BEGIN\r\n END;\r\n $$ LANGUAGE plpgsql;\r\n",
                                schemaInfo.SPName, usedParameter);

    

            Console.WriteLine("PstScriptBuilder.CreateScriptTable : " + schemaInfo.name + ", Done");
            return result;
        }

        public string CreateScriptSP(SPSchemaInfoData schemaInfo)
        {
            var result = "";
           
            var Tools = new PoorMansUtils();
                var comments = schemaInfo.SqlCode;
                
                comments = Tools.beautifySQL(comments);
                var resultsetOutputs = schemaInfo.SPOutputList;
                var singleOutputs = schemaInfo.usedParameterList;
                var xml = Tools.getXml(comments).DocumentElement;
                var keyword = new Keyword(xml);

                var parameter = "";
                var outputString = "";
                if (singleOutputs != null)
                {
                    outputString = " " + cekParameter(singleOutputs[0]) + " ";
                    keyword.ReturnKind =  Keyword.enProcReturnKind.Single;
                }
                else if (resultsetOutputs.Count() != 0)
                {
                    outputString = " SETOF " + schemaInfo.SPName + "_rs ";
                    keyword.ReturnKind = Keyword.enProcReturnKind.Resultset;
                }
                else
                {
                    outputString = " VOID ";
                    keyword.ReturnKind = Keyword.enProcReturnKind.Void;
                }
           

               keyword.convertSqlStatement();
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(keyword.Element.ToString());
                var script = Tools.getString(xmlDoc);
                script = script.Replace(@"@", "m_");
                var splittedText = Regex.Split(script, "\r\nas\r\n", RegexOptions.IgnoreCase).ToList().Skip(1).ToArray();

             //   var parameters = getAllParameters().Where(x => x.IsResult == false && x.Name == proc);
             //   var parameter = parameters.ToList().Aggregate(new StringBuilder(), (sb, i) => sb.Append("m_" + i.ParamName.Substring(1, i.ParamName.Length - 1) + " " + i.getConvertedDataType() + "\r\n,")).ToString();
             //   parameter = parameter.Substring(0, parameter.Length - 1);
                foreach (var allparam in singleOutputs)
                 {
                        parameter += allparam.ParameterName + "\r\n";
                 }
                parameter = parameter.Replace(@"@", "m_");
                result = "CREATE OR REPLACE FUNCTION " + schemaInfo.SPName + "\r\n(" +
                    parameter +
                     ")\r\n RETURNS " + outputString + " \r\n LANGUAGE plpgsql \r\n AS $$ \r\n " +
                     splittedText.Aggregate(new StringBuilder(), (sb, i) => sb.Append(i)).ToString() + "  \r\n  \r\n ";

          //  MessageBox.Show(result);
            return result;
        }

        public string CreateScriptRecord(RecordSchemaInfoData schemaInfo)
        {
            string result = "";
            string[] rColumnDT = null;
            string[] rColumnName = null;
            var r = 0;
          //  var getType = "";
            var columnName = "";
            var nValues = "";
            var dtSet = new DataSet();
            foreach (DataTable dataTable in dtSet.Tables)
                dataTable.BeginLoadData(); 

       

            var path = "D:\\tempMigration\\" + schemaInfo.TableName + ".xml";
           var SchemaPath = "D:\\tempMigration\\" + schemaInfo.TableName + ".xsd";
            var pathResult = "D:\\tempMigration\\result\\" + schemaInfo.TableName + "_Result.sql";

            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException();

                using (StreamWriter sw = new StreamWriter(pathResult))
                {
               
                    XmlDocument nData = new XmlDocument();
                    XmlSchemaSet schemaSet = new XmlSchemaSet();
                    schemaSet.Add("http://www.w3.org/2001/XMLSchema", SchemaPath);
                    schemaSet.Compile();
                    XmlSchema xmlSchema = null;
                    foreach (XmlSchema schema in schemaSet.Schemas())
                    {
                        xmlSchema = schema;
                    }
                    foreach (object item in xmlSchema.Items)
                    {
                        XmlSchemaElement schemaElement = item as XmlSchemaElement;
                        XmlSchemaComplexType complexType = item as XmlSchemaComplexType;
                        if (schemaElement != null)
                        {
                           //MessageBox.Show(schemaElement.Name);

                            XmlSchemaType schemaType = schemaElement.SchemaType;
                            XmlSchemaComplexType schemaComplexType = schemaType as XmlSchemaComplexType;
                           
                            if (schemaComplexType != null)
                            {
                                XmlSchemaParticle particle = schemaComplexType.Particle;
                                XmlSchemaChoice schemaChoice = particle as XmlSchemaChoice;
                              
                                var item2 = schemaChoice.Items[0];
                                    XmlSchemaElement schemaElement2 = item2 as XmlSchemaElement;
                                if (schemaElement2 != null)
                                {
                                //    MessageBox.Show(schemaElement2.Name);
                                    XmlSchemaType schemaType2 = schemaElement2.SchemaType;
                                    XmlSchemaComplexType schemaComplexType2 = schemaType2 as XmlSchemaComplexType;
                                     if (schemaComplexType2 != null)
                                    {
                                        XmlSchemaParticle particle2 = schemaComplexType2.Particle;
                                        XmlSchemaSequence sequence2 = particle2 as XmlSchemaSequence;
                                        if (sequence2 != null)
                                        {
                                          //  r = 0;
                                            rColumnName = new string[sequence2.Items.Count];
                                            rColumnDT = new string[sequence2.Items.Count];
                                            foreach (XmlSchemaElement childElement in sequence2.Items)
                                            {
                                                rColumnName[r] = childElement.Name;
                                                rColumnDT[r] = childElement.SchemaTypeName.Name;
                                                //   MessageBox.Show(childElement.Name + " " + childElement.SchemaTypeName.Name);
                                                r++;
                                            }

                                        }
                                    }
                                   
                                }
                               
                               
                            }

                        }
                    }

                        nData.Load(path);

                   
                    XmlNodeList list = nData.DocumentElement.GetElementsByTagName("Table");
                   
                    if (list.Count != 0)
                    {
                        for (int i = 0; i < list[0].ChildNodes.Count; i++)
                        {
                            if (i == (list[0].ChildNodes.Count - 1))
                            {
                                columnName += list[0].ChildNodes[i].Name;
                            }
                            else
                            {
                                columnName += list[0].ChildNodes[i].Name + ",";
                            }

                        }
                      sw.WriteLine("INSERT INTO " + schemaInfo.name + "(" + columnName + ") " + "VALUES");

                        for (int p = 0; p < list.Count; p++)
                        {
                              
                            nValues += "(";
                            //==== mulai value kolom
                            for (int i = 0; i < list[p].ChildNodes.Count; i++)
                            {
                                var nColumn = list[p].ChildNodes[i].InnerText;
                                //   nValues += nColumn;
                                if (i == (list[p].ChildNodes.Count - 1))
                                {
                                   
                                    if (rColumnDT[i] != "string" && rColumnDT[i] != "dateTime")
                                        nValues += nColumn;
                                    else
                                        nValues += "'" + nColumn + "'";

                                }
                                else
                                {
                                
                                //    getType = getType.GetMember(list[p].ChildNodes[i].Name).GetType();
                                    if ( rColumnDT[i] != "string" && rColumnDT[i] != "dateTime")  
                                        nValues += nColumn + ",";
                                    else
                                        nValues += "'" + nColumn + "',";
                                }
                            }
                            //========last value
                            if (p == (list.Count - 1))
                            {
                                nValues += ");\r\n";
                            }
                            else
                            {
                                nValues += "),";
                            }
                            sw.WriteLine(nValues);
                            nValues = "";
                        }


                        //   result = "INSERT INTO " + schemaInfo.name + "(" + columnName + ") " + "VALUES\r\n";
                        //   result += nValues;
                        result = pathResult;
                        sw.Dispose();
                    }
                    else
                    {
                        result = "";
                    }
                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("The " + schemaInfo.TableName + ".xml, is missing..!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        

           
            Console.WriteLine("PstScriptBuilder.CreateScriptRecord : " + schemaInfo.name + ", Done");
            
            return result;

        }

        public string CreateScriptFunction(FunctionSchemaInfoData schemaInfo)
        {
            var result = "";
            var hasDeclare = false;
            var hasBody = false;
            var beginBody = "";

            var FnName = schemaInfo.FnName;
            var nReturn = "";
            var parName = "";
            var declareName = "";
            var declareDT = "";
            StringReader strReader = new StringReader(schemaInfo.SqlCode);


            using (StringReader reader = new StringReader(schemaInfo.SqlCode))
            {
                string line = string.Empty;
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        var lines = line.TrimStart();
                        if (lines.Contains("declare"))
                        {
                            hasDeclare = true;
                            string[] words = lines.Split(' ');
                            declareName = words[1].Replace(@"@", "d_");
                            declareDT = words[2];
                        }

                        
                        else if (lines.Contains("END"))
                            hasBody = false;

                        if (hasBody == true)
                            beginBody += "--" + lines + "\r\n";
                        if (lines.Contains("BEGIN"))
                            hasBody = true;

                    }

                } while (line != null);
            }
           

            for (var i = 0; i < schemaInfo.usedParameterList.Count; i++)
            {
                if (schemaInfo.usedParameterList[i].ParameterName != "")
                {
                    parName += schemaInfo.usedParameterList[i].ParameterName.Replace(@"@", "p_");
                    if (schemaInfo.usedParameterList[i].DomainType != "")
                    {
                        parName += " " + schemaInfo.usedParameterList[i].DomainType;
                    }
                    else
                    {
                        parName += " " + GetDataTypeMap(schemaInfo.usedParameterList[i].DataType);
                    }
                   

                    if (i < (schemaInfo.usedParameterList.Count - 1))
                        parName += ",";
                }
                else
                {
                    if (schemaInfo.usedParameterList[i].DomainType != "")
                    {
                        nReturn = schemaInfo.usedParameterList[i].DomainType;
                    }
                    else if (schemaInfo.usedParameterList[i].DataType == "datetime" || schemaInfo.usedParameterList[i].DataType == "bit")
                    {
                        nReturn = schemaInfo.usedParameterList[i].DataType;
                    }
                    else if (schemaInfo.usedParameterList[i].ParameterMaxBytes != 0)
                    {
                        nReturn = GetDataTypeMap(schemaInfo.usedParameterList[i].DataType) + "(" + schemaInfo.usedParameterList[i].ParameterMaxBytes + ")";
                    }
                    else
                    {
                        nReturn = GetDataTypeMap(schemaInfo.usedParameterList[i].DataType) + "(" + schemaInfo.usedParameterList[i].NumericPrecision + ")";
                    }

                       
                }
                
            }

            result = "CREATE OR REPLACE FUNCTION " + FnName + "(" + parName + ")\r\n" +
                     "RETURNS " + nReturn + " AS $$\r\n";
            if (hasDeclare == true)
                result += "DECLARE " + " " + declareName + " " + declareDT + ";\r\n";

            result += "BEGIN\r\n" +
                      beginBody +
                  
            "RETURN " + declareName + ";\r\n" + 
            "END;\r\n" +
            "$$  LANGUAGE plpgsql;\r\n"+
            "\r\n";
            return result;
        }

       

        private string cekParameter(UsedParameter data)
        {
            var cekResult = "";
            if (data.DomainType != "")
            {
                cekResult = data.DomainType;
            }
            else
            {
                cekResult = GetDataTypeMap(data.DataType);
            }

           
            return cekResult;
        }

        private readonly List<TablesFieldDataType> mapDataTypes = new List<TablesFieldDataType>
        {
            new TablesFieldDataType { DataType="datetime", ConvertedDataType="timestamp" },
            new TablesFieldDataType { DataType="bigint", ConvertedDataType="bigint" },
            new TablesFieldDataType { DataType="binary", ConvertedDataType="bytea" },
            new TablesFieldDataType { DataType="boolean", ConvertedDataType="boolean" },
            new TablesFieldDataType { DataType="char", ConvertedDataType="char" },
            new TablesFieldDataType { DataType="currency", ConvertedDataType="money" },
            new TablesFieldDataType { DataType="dbdate", ConvertedDataType="date" },
            new TablesFieldDataType { DataType="dbtime", ConvertedDataType="time" },
            new TablesFieldDataType { DataType="dbtimestamp", ConvertedDataType="timestamp" },
            new TablesFieldDataType { DataType="date", ConvertedDataType="date" },
            new TablesFieldDataType { DataType="decimal", ConvertedDataType="numeric" },
            new TablesFieldDataType { DataType="double", ConvertedDataType="double" },
            new TablesFieldDataType { DataType="filetime", ConvertedDataType="timestamp" },
            new TablesFieldDataType { DataType="guid", ConvertedDataType="uuid" },
            new TablesFieldDataType { DataType="idispatch", ConvertedDataType="oid" },
            new TablesFieldDataType { DataType="iunknown", ConvertedDataType="oid" },
            new TablesFieldDataType { DataType="integer", ConvertedDataType="integer" },
            new TablesFieldDataType { DataType="int", ConvertedDataType="integer" },
            new TablesFieldDataType { DataType="longvarbinary", ConvertedDataType="bytea" },
            new TablesFieldDataType { DataType="longvarchar", ConvertedDataType="text" },
            new TablesFieldDataType { DataType="longvarwchar", ConvertedDataType="text" },
            new TablesFieldDataType { DataType="numeric", ConvertedDataType="numeric" },
            new TablesFieldDataType { DataType="bit", ConvertedDataType="bit" },
            new TablesFieldDataType { DataType="propvariant", ConvertedDataType="text" },
            new TablesFieldDataType { DataType="single", ConvertedDataType="real" },
            new TablesFieldDataType { DataType="smallint", ConvertedDataType="smallint" },
            new TablesFieldDataType { DataType="tinyint", ConvertedDataType="smallint" },
            new TablesFieldDataType { DataType="unsignedtinyint", ConvertedDataType="tinyint" },
            new TablesFieldDataType { DataType="nvarchar", ConvertedDataType="varchar" },
            new TablesFieldDataType { DataType="nchar", ConvertedDataType="varchar" },
            new TablesFieldDataType { DataType="varchar", ConvertedDataType="varchar" },
            new TablesFieldDataType { DataType="varbinary", ConvertedDataType="bytea" },
            new TablesFieldDataType { DataType="varnumeric", ConvertedDataType="numeric" },
            new TablesFieldDataType { DataType="varwchar", ConvertedDataType="varchar" },
            new TablesFieldDataType { DataType="variant", ConvertedDataType="oid" },
            new TablesFieldDataType { DataType="wchar", ConvertedDataType="char" },
            new TablesFieldDataType { DataType="image", ConvertedDataType="bytea" },
            new TablesFieldDataType { DataType="bstr", ConvertedDataType="--" },
            new TablesFieldDataType { DataType="empty", ConvertedDataType="--" },
            new TablesFieldDataType { DataType="error", ConvertedDataType="--" },
            new TablesFieldDataType { DataType="unsignedbigint", ConvertedDataType="--" },
            new TablesFieldDataType { DataType="unsignedint", ConvertedDataType="--" },
            new TablesFieldDataType { DataType="unsignedsmallint", ConvertedDataType="--" },
        };

        public string GetDataTypeMap(string dataType)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException("dataType");
            }


            var findingResult = mapDataTypes.SingleOrDefault(x => string.Equals(x.DataType, dataType, StringComparison.CurrentCultureIgnoreCase));

            return findingResult == null ? "" : findingResult.ConvertedDataType;
        }

        private string cekSuffix(UsedColumn data)
        {
            var cekResult = "";
           

            if (data.CharMaxLength != 0 && data.CharMaxLength != -1 && data.CharMaxLength < 10485760 )
            {
                cekResult = " (" + data.CharMaxLength + ")";
            }
            else if (data.Scale != 0)
            {
                cekResult = " (" + data.Precision + "," + data.Scale + ")";
            }

            if (data.isNullable == false)
            {
                cekResult += "  NOT NULL";
            }

            if (data.Domain != "")
            {
                cekResult = data.Domain;
            }
            else
            {
                cekResult = GetDataTypeMap(data.DataType) + cekResult;
            }
            return cekResult;

        }

        private class nTable
        {
            public string[] nColumn { get; set; }
        }
    }
}
