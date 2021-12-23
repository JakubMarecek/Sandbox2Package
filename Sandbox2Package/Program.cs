using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace ConsoleApp1
{
    class Program
    {
        public static string version = "20211223-1500";
        public static string outFile = "Custom Sandbox Creation.a3";

        static void Main(string[] args)
        {
            Console.Title = "Sandbox2Package";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("*******************************************************************************************");
            Console.WriteLine("**** Sandbox2Package v" + version);
            Console.WriteLine("****   Author: ArmanIII");
            Console.WriteLine("*******************************************************************************************");
            Console.ResetColor();
            Console.WriteLine("");

            if (args.Length == 0)
            {
                Console.WriteLine("Converts output from Sandbox to own package.");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("==========================================================================");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("[Usage]");
                Console.WriteLine("    Sandbox2Package <file>");
                Console.WriteLine("    file - output file with saved objects from Sandbox");
                Console.WriteLine("         - EditorConvert.txt - convert saved to group, so you can use your saved objects to group");
                Console.WriteLine("         - you can specify txt file with custom editor groups, see https://fallen.ninja/farcry5/items");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("==========================================================================");
                Console.WriteLine("");
                Console.ResetColor();
                return;
            }

            string file = args[0];

            Console.Title = "Sandbox2Package - " + file;

            string folder = Path.GetDirectoryName(file) + "\\";

            string firstLine = File.ReadAllLines(file).First();
            if (firstLine.StartsWith("<IGE.ObjectSelection"))
            {
                ConvertIGESelection(file, folder);
            }
            else if (file.EndsWith("EditorConvert.txt"))
            {
                ConvertSavedToGroup(file, folder);
            }
            else
            {
                Convert2Package(file, folder);
            }

            Console.WriteLine("FIN!");
        }

        static void ConvertIGESelection(string file, string outDir)
        {
            List<string> sandboxGroup = new List<string>();

            XDocument doc = XDocument.Load(file);

            string[] center = doc.Root.Attribute("SelCenter").Value.Split(',');

            IEnumerable<XElement> objects = doc.Root.Elements("Object");
            foreach (XElement obj in objects)
            {
                XElement subObj = obj.Element("Object");

                string arkID = subObj.Attribute("LibId").Value;
                string[] pos = subObj.Attribute("Pos").Value.Split(',');
                string rot = subObj.Attribute("Angles").Value;

                float posX = float.Parse(pos[0], CultureInfo.InvariantCulture) - float.Parse(center[0], CultureInfo.InvariantCulture);
                float posY = float.Parse(pos[1], CultureInfo.InvariantCulture) - float.Parse(center[1], CultureInfo.InvariantCulture);
                float posZ = float.Parse(pos[2], CultureInfo.InvariantCulture) - float.Parse(center[2], CultureInfo.InvariantCulture);

                sandboxGroup.Add($"{arkID},{posX.ToString(CultureInfo.InvariantCulture)},{posY.ToString(CultureInfo.InvariantCulture)},{posZ.ToString(CultureInfo.InvariantCulture)},{rot}");
            }

            File.WriteAllLines(outDir + "EditorGroup.txt", sandboxGroup.ToArray());
        }

        static void ConvertSavedToGroup(string file, string outDir)
        {
            string[] lines = File.ReadAllLines(file);

            string[] refObj = lines[0].Split(',');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] != "")
                {
                    string[] split = lines[i].Split(',');

                    float posX = float.Parse(split[1], CultureInfo.InvariantCulture) - float.Parse(refObj[1], CultureInfo.InvariantCulture);
                    float posY = float.Parse(split[2], CultureInfo.InvariantCulture) - float.Parse(refObj[2], CultureInfo.InvariantCulture);
                    float posZ = float.Parse(split[3], CultureInfo.InvariantCulture) - float.Parse(refObj[3], CultureInfo.InvariantCulture);

                    split[1] = posX.ToString(CultureInfo.InvariantCulture);
                    split[2] = posY.ToString(CultureInfo.InvariantCulture);
                    split[3] = posZ.ToString(CultureInfo.InvariantCulture);

                    lines[i] = string.Join(",", split);
                }
            }

            File.WriteAllLines(outDir + "EditorGroup.txt", lines.ToArray());
        }

        static void Convert2Package(string sandboxSavedFile, string outDir)
        {
            Dictionary<string, List<XElement>> replaces = new();

            XElement xReplaces = new("Replaces");
            void WriteReplaces()
            {
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
            }

            long cnt = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            string[] lines = File.ReadAllLines(sandboxSavedFile);
            foreach (string line in lines)
            {
                if (line != "" && !line.StartsWith(";"))
                {
                    string[] splited = line.Split(',');
                    if (splited.Length > 1)
                    {
                        string conf = GetWorldSectorConfig("fc5_main", splited[1], splited[2]);

                        long entityID = cnt;

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

                        /*XElement components = new("object", new XAttribute("name", "Components"));
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
                        entity.Add(components);*/

                        if (!replaces.ContainsKey(conf))
                            replaces.Add(conf, new List<XElement>());

                        replaces[conf].Add(entity);

                        cnt++;
                    }
                }

                if (line.StartsWith(";"))
                {
                    WriteReplaces();
                    xReplaces.Add(new XComment(line.Replace(";", "")));
                    replaces = new();
                }
            }

            WriteReplaces();


            XDocument xInfoReplaceXML = new(new XDeclaration("1.0", "utf-8", "yes"));
            XElement xInfoReplace = new("PackageInfoReplace");
            xInfoReplace.Add(new XElement("Games", new XElement("Game", "FC5")));
            xInfoReplace.Add(new XElement("Name", "Custom Sandbox Creation"));
            xInfoReplace.Add(new XElement("Description", "Custom placed objects using Sandbox."));

            xInfoReplace.Add(xReplaces);
            xInfoReplaceXML.Add(xInfoReplace);

            MemoryStream ms = new();
            xInfoReplaceXML.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);


            if (File.Exists(outDir + outFile))
                File.Delete(outDir + outFile);

            ZipArchive zip = ZipFile.Open(outDir + outFile, ZipArchiveMode.Create);
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
