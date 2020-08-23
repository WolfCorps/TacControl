using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TacControl.Common.Modules;

namespace TacControl.Common.Maps
{
    public class Helper
    {
        public struct SvgLayer
        {
            public string name;
            public string content;
        }


        private static readonly XNamespace xlink = "http://www.w3.org/1999/xlink";
        private static readonly XNamespace svg = "http://www.w3.org/2000/svg";
        private static readonly XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();



        private static XmlParserContext CreateSvgXmlContext()
        {
            var table = new NameTable();
            var manager = new XmlNamespaceManager(table);
            manager.AddNamespace(string.Empty, svg.NamespaceName);
            manager.AddNamespace("xlink", xlink.NamespaceName);
            return new XmlParserContext(null, manager, null, XmlSpace.None);
        }



        public static Task<List<SvgLayer>> ParseLayers()
        {

            string wantedDirectory = Path.Combine(Directory.GetCurrentDirectory(), "maps");
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                //wantedDirectory = Path.Combine(Directory.GetCurrentDirectory(), "maps");
            }
            else if (System.Environment.OSVersion.Platform == PlatformID.Unix) //Android
            {
                wantedDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            }

            string filePath = "";
            if (File.Exists(Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svgz")))
                filePath = Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svgz");
            else if (File.Exists(Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svg")))
                filePath = Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svg");

            if (string.IsNullOrEmpty(filePath))
            {

                return
                ImageDirectory.Instance.RequestMapfile(GameState.Instance.gameInfo.worldName, wantedDirectory)
                    .ContinueWith(
                        (x) =>
                        {
                            var ret = ParseSvgLayers(x.Result, x.Result.EndsWith("z"));
                            return ret;
                        });
            }

            return Task.FromResult(ParseSvgLayers(filePath, filePath.EndsWith("z")));
        }

        private static List<SvgLayer> ParseSvgLayers(string filePath, bool isCompressed)
        {
            List<SvgLayer> ret = new List<SvgLayer>();

            using (var fileStream = File.OpenRead(filePath))
            {
                GZipStream decompressionStream = new GZipStream(fileStream, CompressionMode.Decompress);

                Stream stream = isCompressed ? decompressionStream : (Stream) fileStream;

                using (var reader = XmlReader.Create(stream, xmlReaderSettings, CreateSvgXmlContext()))
                {
                    var xdoc = XDocument.Load(reader);
                    var svg = xdoc.Root;
                    var ns = svg.Name.Namespace;

                    var mainAttributes = svg.Attributes();
                    var defs = svg.Element(ns + "defs");

                    //Change land color from pure white to a better gray
                    foreach (var def in defs.Descendants())
                    {
                        if (def.Attribute("id")?.Value == "colorLand")
                        {
                            def.Descendants(ns + "stop").First().SetAttributeValue("stop-color", "#DFDFDF");
                        }
                    }


                    List<XElement> layers = new List<XElement>();
                    List<XElement> rootElements = new List<XElement>();

                    foreach (var xElement in svg.Elements())
                    {
                        if (xElement.Name == ns + "g")
                        {
                            layers.Add(xElement);
                        }
                        else
                            rootElements.Add(xElement);
                    }

                    XDocument bareDoc = new XDocument(xdoc);
                    List<XElement> toRemove = new List<XElement>();
                    foreach (var xElement in bareDoc.Root.Elements())
                    {
                        if (xElement.Name == ns + "g")
                            toRemove.Add(xElement);
                    }

                    toRemove.ForEach(x => x.Remove());

                    var test = bareDoc.ToString();
                    foreach (var xElement in layers)
                    {
                        XDocument newDoc = new XDocument(bareDoc);
                        newDoc.Root.Add(xElement);
                        SvgLayer x;
                        x.content = newDoc.ToString();
                        x.name = xElement.Attribute("id").Value;
                        ret.Add(x);
                    }
                }

                decompressionStream.Dispose();
            }
            return ret;
        }
    }
}
