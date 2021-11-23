using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            string sandboxSavedFile = args[0];

            string folder = Path.GetDirectoryName(sandboxSavedFile);

            Dictionary<string, List<XElement>> replaces = new();

            int cnt = 0;
            string[] lines = File.ReadAllLines(sandboxSavedFile);
            foreach (string line in lines)
            {
                string[] splited = line.Split(',');
                if (splited.Length > 1)
                {
                    string conf = GetWorldSectorConfig("fc5_main", splited[1], splited[2]);

                    long entityID = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + cnt;

                    XElement entity = new("object", new XAttribute("name", "Entity"), new XAttribute("addNode", "1"));
                    entity.Add(new XElement("field", $@"entityarchetypeslibrary\{splited[0]}.ark.fcb", new XAttribute("name", "text_hidArchetypeResId"), new XAttribute("type", "String")));
                    entity.Add(new XElement("field", $@"entityarchetypeslibrary\{splited[0]}.ark.fcb", new XAttribute("name", "hidArchetypeResId"), new XAttribute("type", "ComputeHash64")));
                    entity.Add(new XElement("field", $"World:ArmanIII:Sandbox:Entity{entityID}", new XAttribute("hash", "1DD79FAD"), new XAttribute("type", "String")));
                    entity.Add(new XElement("field", $"SandboxEntity{entityID}", new XAttribute("name", "hidName"), new XAttribute("type", "String")));
                    entity.Add(new XElement("field", entityID.ToString(), new XAttribute("name", "disEntityId"), new XAttribute("type", "Id64")));
                    entity.Add(new XElement("field", $"{splited[1]},{splited[2]},{splited[3]}", new XAttribute("name", "hidPos"), new XAttribute("type", "Vector3")));
                    entity.Add(new XElement("field", $"{splited[4]},{splited[5]},{splited[6]}", new XAttribute("name", "hidAngles"), new XAttribute("type", "Vector3")));
                    entity.Add(new XElement("field", $"{splited[1]},{splited[2]},{splited[3]}", new XAttribute("name", "hidPos_precise"), new XAttribute("type", "Vector3")));
                    entity.Add(new XElement("field", "True", new XAttribute("name", "SpawnThreadSafe"), new XAttribute("type", "Boolean")));
                    entity.Add(new XElement("field", new XAttribute("name", "ArchetypeResDepList"), new XElement("Resource", new XAttribute("ID", $@"entityarchetypeslibrary\{splited[0]}.ark.fcb"))));

                    XElement components = new("object", new XAttribute("name", "Components"));
                    {
                        XElement navtComp = new("object", new XAttribute("name", "CNavLinkComponent"));
                        {
                            navtComp.Add(new XElement("field", "False", new XAttribute("name", "bActivated"), new XAttribute("type", "Boolean")));
                        }
                        components.Add(navtComp);

                        XElement eventComp = new("object", new XAttribute("name", "CEventComponent"));
                        {
                            eventComp.Add(new XElement("field", "False", new XAttribute("name", "hidHasAliasName"), new XAttribute("type", "Boolean")));
                            eventComp.Add(new XElement("object", new XAttribute("name", "hidLinks")));
                        }
                        components.Add(eventComp);
                    }
                    entity.Add(components);

                    if (!replaces.ContainsKey(conf))
                        replaces.Add(conf, new List<XElement>());

                    replaces[conf].Add(entity);

                    cnt++;
                }
            }


            XDocument xInfoReplaceXML = new(new XDeclaration("1.0", "utf-8", "yes"));
            XElement xInfoReplace = new("PackageInfoReplace");
            xInfoReplace.Add(new XElement("Games", new XElement("Game", "FC5")));
            xInfoReplace.Add(new XElement("Name", "Custom Sandbox Creation"));
            xInfoReplace.Add(new XElement("Description", "Custom placed objects using Sandbox."));

            XElement xReplaces = new("Replaces");
            foreach (KeyValuePair<string, List<XElement>> pair in replaces)
            {
                XElement xReplace = new("Replace", new XAttribute("RequiredFile", pair.Key));
                XElement CSectorSpawnCategory = new("object", new XAttribute("hash", "464A7F75"));
                XElement MissionLayer = new("object", new XAttribute("hash", "494C09F2"));
                MissionLayer.Add(new XElement("primaryKey", new XAttribute("hash", "B88F49FD"), "6400000000000000"));

                foreach (XElement entity in pair.Value)
                    MissionLayer.Add(entity);

                CSectorSpawnCategory.Add(MissionLayer);
                xReplace.Add(CSectorSpawnCategory);
                xReplaces.Add(xReplace);
            }

            xInfoReplace.Add(xReplaces);
            xInfoReplaceXML.Add(xInfoReplace);

            MemoryStream ms = new();
            xInfoReplaceXML.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);


            ZipArchive zip = ZipFile.Open(folder + "Custom Sandbox Creation.a3", ZipArchiveMode.Create);
            ZipArchiveEntry zipInfo = zip.CreateEntry("info_replace.xml");
            using (Stream entryStream = zipInfo.Open())
            {
                ms.CopyTo(entryStream);
            };
            zip.Dispose();
        }

        static string GetWorldSectorConfig(string worldName, string x, string y)
        {
            float sourceX = float.Parse(x, CultureInfo.InvariantCulture);
            float sourceY = float.Parse(y, CultureInfo.InvariantCulture);

            float tmpX = sourceX + 5120.0f;
            float tmpY = sourceY + 5120.0f;

            tmpX /= 64.0f;
            tmpY /= 64.0f;

            int sectX = (int)Math.Floor(tmpX);
            int sectY = (int)Math.Floor(tmpY);

            int mainSectChar = (int)Math.Floor(sectY / 16.0f);
            int mainSectNum = (int)Math.Floor(sectX / 16.0f);

            int sectID = (sectY * 160) + sectX;

            char[] sectChar = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };

            return $@"generated\worlds\{worldName}\levels\{sectChar[mainSectChar]}{mainSectNum}\worldsectors\worldsector_{sectID}.data.fcb";
        }
    }
}
