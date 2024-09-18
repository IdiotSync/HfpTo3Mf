using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Data.SqlTypes;
using System.Xml;
using System.Net.Mime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;


namespace WpfApp1
{

    public sealed partial class Tools
    {

        private static string getModelInfo(string rawStl)
        {
            string verticeTriangles = importBinary(rawStl);
            // return package;
            return verticeTriangles;
        }
        private static byte[] ReadAllBytes(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
        private static string importBinary(string fileName)
        {

            int vectorCount = 0;
            Dictionary<Vector3, int> Uniques = new Dictionary<Vector3, int>();

            XmlDocument docElem = new XmlDocument();
            XmlDeclaration xmldecl = docElem.CreateXmlDeclaration("1.0", "utf-8", null);
            XmlElement rootElem = docElem.DocumentElement;
            docElem.InsertBefore(xmldecl, rootElem);
            XmlElement modelElem = docElem.CreateElement("model", "http://schemas.microsoft.com/3dmanufacturing/core/2015/02");
            modelElem.SetAttribute("unit", "millimeter");
            modelElem.SetAttribute("xml:lang", "en-US");
            modelElem.SetAttribute("xmlns:BambuStudio", "http://schemas.bambulab.com/package/2021");
            modelElem.SetAttribute("xmlns:p", "http://schemas.microsoft.com/3dmanufacturing/production/2015/06");
            modelElem.SetAttribute("requiredextensions", "p");
            docElem.AppendChild(modelElem);

            XmlElement metadataElem = docElem.CreateElement("metadata");
            metadataElem.SetAttribute("name", "BambuStudio:3mfVersion");
            metadataElem.InnerText = "1";
            modelElem.AppendChild(metadataElem);

            XmlElement resourceElem = docElem.CreateElement("resources", docElem.DocumentElement.NamespaceURI);
            modelElem.AppendChild(resourceElem);

            XmlElement objectElem = docElem.CreateElement("object");
            objectElem.SetAttribute("id", "1");
            objectElem.SetAttribute("p:UUID", "00010000-81cb-4c03-9d28-80fed5dfa1dc");
            objectElem.SetAttribute("type", "model");
            resourceElem.AppendChild(objectElem);

            XmlElement meshElem = docElem.CreateElement("mesh", docElem.DocumentElement.NamespaceURI);
            objectElem.AppendChild(meshElem);
            XmlElement verticesElem = docElem.CreateElement("vertices", docElem.DocumentElement.NamespaceURI);
            objectElem.AppendChild(verticesElem);
            XmlElement trianglesElem = docElem.CreateElement("triangles", docElem.DocumentElement.NamespaceURI);
            objectElem.AppendChild(trianglesElem);

            try
            {
                Object locker = new Object();
                lock (locker)
                {
                    using (BinaryReader br = new BinaryReader(File.Open(fileName, FileMode.Open)))
                    {
                        byte[] header = br.ReadBytes(80);
                        byte[] length = br.ReadBytes(4);
                        int numberOfSurfaces = BitConverter.ToInt32(length, 0);
                        string headerInfo = Encoding.UTF8.GetString(header, 0, header.Length).Trim();
                        System.Diagnostics.Debug.WriteLine(String.Format("Number of faces:{0}", numberOfSurfaces));

                        byte[] full = ReadAllBytes(br.BaseStream);
                        byte[] block = new byte[50];
                        int surfCount = 0;
                        while (surfCount < numberOfSurfaces)
                        {
                            byte[] xComp = new byte[4];
                            byte[] yComp = new byte[4];
                            byte[] zComp = new byte[4];
                            int offset = surfCount * 50;
                            List<int> MyFace = new List<int>();
                            for (int i = 1; i < 4; i++)
                            {
                                for (int k = 0; k < 12; k++)
                                {
                                    int index = (k + i * 12) + offset;

                                    if (k < 4)
                                    {
                                        xComp[k] = full[index];
                                    }
                                    else if (k < 8)
                                    {
                                        yComp[k - 4] = full[index];
                                    }
                                    else
                                    {
                                        zComp[k - 8] = full[index];
                                    }
                                }
                                Vector3 vert = new Vector3();
                                vert.X = BitConverter.ToSingle(xComp, 0);
                                vert.Y = BitConverter.ToSingle(yComp, 0);
                                vert.Z = BitConverter.ToSingle(zComp, 0);

                                if (!Uniques.ContainsKey(vert))
                                {
                                    XmlElement vertexElem = docElem.CreateElement("vertex");
                                    vertexElem.SetAttribute("x", vert.X.ToString());
                                    vertexElem.SetAttribute("y", vert.Y.ToString());
                                    vertexElem.SetAttribute("z", vert.Z.ToString());
                                    verticesElem.AppendChild(vertexElem);
                                    Uniques.Add(vert, vectorCount);
                                    vectorCount++;
                                }

                                MyFace.Add(Uniques[vert]);
                                if (i == 3)
                                {
                                    XmlElement triangleElem = docElem.CreateElement("triangle");
                                    var v1 = MyFace[0];
                                    var v2 = MyFace[1];
                                    var v3 = MyFace[2];
                                    triangleElem.SetAttribute("v1", v1.ToString());
                                    triangleElem.SetAttribute("v2", v2.ToString());
                                    triangleElem.SetAttribute("v3", v3.ToString());
                                    trianglesElem.AppendChild(triangleElem);
                                }
                            }
                            surfCount++;
                        }
                    }


                    docElem.PreserveWhitespace = true;
                    return docElem.OuterXml;
                }

            }
            catch (Exception e)  // This is too general to be the only catch statement.
            {
                System.Diagnostics.Debug.WriteLine("The file could not be read:");
                System.Diagnostics.Debug.WriteLine(e.Message);
                return "";

            }
        }
        private static XmlElement createMeta(XmlDocument docElem, string name, string text)
        {
            XmlElement TitleElem = docElem.CreateElement("metadata");
            TitleElem.SetAttribute("name", name);
            TitleElem.InnerText = text;
            TitleElem.RemoveAttribute("xmlns");
            return TitleElem;
        }

        private static void AddToArchive(ZipArchive archive, string archivePath, string xml)
        {
            ZipArchiveEntry modelEntry = archive.CreateEntry(archivePath);
            using (StreamWriter writer = new StreamWriter(modelEntry.Open()))
            {
                writer.Write(xml);
            }
        }
        private static string getCuttingInfo(JObject HfpData, JArray Colors)
        {
            var layer_height = HfpData["layer_height"];
            var base_layer_height = HfpData["base_layer_height"];

            string outputCut = "";
            int extruderCount = 2;
            var maxColor = Colors.Count;
            var swapCount = 0;
            var maxSwaps = Colors.Count - 1;
            foreach (JValue value in HfpData["slider_values"])
            {
                if (swapCount < maxSwaps)
                { // weird color swap storage in hueforge
                    var zHeight = (value.Value<double>()) * layer_height.Value<double>() + base_layer_height.Value<double>();
                    var currentExtruder = maxColor - extruderCount - 1;
                    var colorhere = Colors[extruderCount - 1];
                    outputCut += "<layer top_z=\"" + zHeight + "\" type=\"2\" extruder=\"" + extruderCount + "\" color=\"" + colorhere.Value<string>() + "\" extra=\"\" gcode=\"tool_change\"/>\r\n";
                    extruderCount++;
                }
                swapCount++;
            }

            var cutXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
            "<custom_gcodes_per_layer>\r\n" +
            "<plate>\r\n" +
            "<plate_info id=\"1\"/>\r\n" +
            outputCut +
            "<mode value=\"MultiAsSingle\"/>\r\n" +
            "</plate>\r\n" +
            "</custom_gcodes_per_layer>\r\n";

            return cutXml;
        }

        public static async void CreatePackage(string inputFile)
        {
            JObject HfpData = JObject.Parse(File.ReadAllText(inputFile));
            string Folder = inputFile;
            if (inputFile.LastIndexOf("\\") > 0)
                Folder = inputFile.Substring(0,inputFile.LastIndexOf("\\")+1);
            else if (inputFile.LastIndexOf("/") > 0)
                Folder = inputFile.Substring(0, inputFile.LastIndexOf("/") + 1);
            string stlOrig = HfpData["stl"].Value<string>();
            if (stlOrig.LastIndexOf("\\") > 0)
                stlOrig = stlOrig.Substring(stlOrig.LastIndexOf("\\") + 1);
            else if (stlOrig.LastIndexOf("/") > 0)
                stlOrig = stlOrig.Substring(stlOrig.LastIndexOf("/") + 1);
            string outputPath = inputFile.Replace("hfp", "3mf");
            var stlName = Folder + stlOrig;
            var bblXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                "<model unit=\"millimeter\" xml:lang=\"en-US\" xmlns=\"http://schemas.microsoft.com/3dmanufacturing/core/2015/02\" xmlns:BambuStudio=\"http://schemas.bambulab.com/package/2021\" xmlns:p=\"http://schemas.microsoft.com/3dmanufacturing/production/2015/06\" requiredextensions=\"p\">\r\n" +
                "<metadata name=\"Application\">BambuStudio-01.09.05.51</metadata>\r\n" +
                "<metadata name=\"BambuStudio:3mfVersion\">1</metadata>\r\n" +
                "<metadata name=\"Copyright\"></metadata>\r\n" +
                "<metadata name=\"CreationDate\">2024-09-17</metadata>\r\n" +
                "<metadata name=\"Description\"></metadata>\r\n" +
                "<metadata name=\"Designer\"></metadata>\r\n" +
                "<metadata name=\"DesignerCover\"></metadata>\r\n" +
                "<metadata name=\"DesignerUserId\">1403644216</metadata>\r\n" +
                "<metadata name=\"License\"></metadata>\r\n" +
                "<metadata name=\"ModificationDate\">2024-09-17</metadata>\r\n" +
                "<metadata name=\"Origin\"></metadata>\r\n" +
                "<metadata name=\"Title\"></metadata>\r\n" +
                "<resources>\r\n" +
                "<object id=\"2\" p:UUID=\"00000001-61cb-4c03-9d28-80fed5dfa1dc\" type=\"model\">\r\n" +
                "<components>\r\n" +
                "<component p:path=\"/3D/Objects/object_1.model\" objectid=\"1\" p:UUID=\"00010000-b206-40ff-9872-83e8017abed1\" transform=\"1 0 0 0 1 0 0 0 1 0 0 0\"/>\"\r\n" +
                "</components>\r\n" +
                "</object>\r\n" +
                "</resources>\r\n" +
                " <build p:UUID=\"2c7c17d8-22b5-4d84-8835-1976022ea369\">\r\n" +
                " <item objectid=\"2\" p:UUID=\"00000002-b1ec-4553-aec9-835e5b724bb4\" transform=\"1 0 0 0 1 0 0 0 1 128 128 12\" printable=\"1\"/>\r\n" +
                "</build>\r\n" +
                "</model>";

            var globalRel = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Target=\"/3D/3dmodel.model\" Id=\"rel0\" Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\" />" +
            "</Relationships>";

            var modelRelXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            " <Relationship Target=\"/3D/Objects/object_1.model\" Id=\"rel-1\" Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\"/>" +
            "</Relationships>";


            var stringMeshXml = getModelInfo(stlName);

            var sliceInfoXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<config>" +
                "  <header>" +
                "  <header_item key=\"X-BBL-Client-Type\" value=\"slicer\"/>" +
                "  <header_item key=\"X-BBL-Client-Version\" value=\"01.09.05.51\"/>" +
                "  </header>" +
                " </config>";
            JObject projectSettings = ProjectSettings.projectJson;


            JArray filament_set = new JArray();
            JArray filament_settings_id = new JArray();
            JArray filament_type = new JArray();
            JArray filament_vendor = new JArray();
            foreach (JObject value in (JArray)HfpData["filament_set"])
            {
                foreach (var property in value.Properties())
                {
                    if (property.Name == "Color")
                    {
                        filament_settings_id.Add("Bambu PLA Basic @BBL X1C");
                        filament_type.Add("PLA");
                        filament_vendor.Add("Bambu Lab");
                        filament_set.AddFirst(property.Value);
                    }
                }
            }
            projectSettings["filament_colour"] = filament_set;
            projectSettings["filament_settings_id"] = filament_settings_id;
            projectSettings["filament_type"] = filament_type;
            projectSettings["filament_vendor"] = filament_vendor;

            string cutXml = getCuttingInfo(HfpData, filament_set);

            using (FileStream zipToOpen = new FileStream(outputPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    AddToArchive(archive, "3D/3dmodel.model", bblXml);
                    AddToArchive(archive, "3D/_rels/3dmodel.model.rels", modelRelXml);
                    AddToArchive(archive, "3D/Objects/object_1.model", stringMeshXml);
                    AddToArchive(archive, "_rels/.rels", globalRel);
                    AddToArchive(archive, "Metadata/slice_info.config", sliceInfoXml);
                    AddToArchive(archive, "Metadata/project_settings.config", projectSettings.ToString());
                    AddToArchive(archive, "Metadata/custom_gcode_per_layer.xml", cutXml);
                    //AddToArchive(archive, "[Content_Types].xml", cutXml);
                }
            }
        }

    }
}