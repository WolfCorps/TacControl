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

        public interface IMapLayerData
        {
            Stream GetStream();
            long GetSize();
        }

        public class MapLayerDataMemory : IMapLayerData
        {
            private byte[] _data;

            public MapLayerDataMemory(string data)
            {
                _data = Encoding.UTF8.GetBytes(data);
            }
            public Stream GetStream()
            {
                return new MemoryStream(_data);
            }

            public long GetSize()
            {
                return _data.Length;
            }
        }

   

        public class MapLayerDataZipFile : IMapLayerData, IDisposable
        {
            private byte[] _data;
            private ZipArchiveEntry zipArchiveEntry;
            private SharedDisposable<ZipArchive>.Reference reference;
            public MapLayerDataZipFile(ZipArchiveEntry zipArchiveEntry, SharedDisposable<ZipArchive>.Reference reference)
            {
                this.zipArchiveEntry = zipArchiveEntry;
                this.reference = reference;
            }

            public Stream GetStream()
            {
                //Cannot read from zip in multiple threads..
                var retStream = new MemoryStream();
                lock (reference.Value)
                {
                    using (var str = zipArchiveEntry.Open())
                        str.CopyTo(retStream);
                }

                retStream.Position = 0;
                return retStream;
            }

            public long GetSize()
            {
                return zipArchiveEntry.Length;
            }

            //#TODO is this a memory leak if we don't call dispose?
            public void Dispose()
            {
                reference?.Dispose();
            }
        }


        public class SvgLayer
        {
            public string name;
            public IMapLayerData content;
            public int width;
        }


        private static readonly XNamespace xlink = "http://www.w3.org/1999/xlink";
        private static readonly XNamespace svg = "http://www.w3.org/2000/svg";
        private static readonly XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();

        public static event EventHandler<bool> WaitingForTerrain;

        private static XmlParserContext CreateSvgXmlContext()
        {
            var table = new NameTable();
            var manager = new XmlNamespaceManager(table);
            manager.AddNamespace(string.Empty, svg.NamespaceName);
            manager.AddNamespace("xlink", xlink.NamespaceName);
            return new XmlParserContext(null, manager, null, XmlSpace.None);
        }



        public static async Task<List<SvgLayer>> ParseLayers()
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
            if (File.Exists(Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".zip")))
                filePath = Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".zip");
            else if (File.Exists(Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svgz")))
                filePath = Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svgz");
            else if (File.Exists(Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svg")))
                filePath = Path.Combine(wantedDirectory, GameState.Instance.gameInfo.worldName + ".svg");

            if (string.IsNullOrEmpty(filePath))
            {
                WaitingForTerrain?.Invoke(null, true);
                var mapFile = await ImageDirectory.Instance.RequestMapfile(GameState.Instance.gameInfo.worldName, wantedDirectory);
                var result = await ParseSvgLayers(mapFile);
                
                WaitingForTerrain?.Invoke(null, false);
                return result;
            }

            return await ParseSvgLayers(filePath);
        }

        /// <summary>
        /// Split and compress raw SVG
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task ProcessRawSVG(string filePath)
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                var split = await Task.Run(() => SplitSVG(fileStream));

                //#TODO SVG clipping and subdivision
                // https://en.wikipedia.org/wiki/Weiler%E2%80%93Atherton_clipping_algorithm
                // https://en.wikipedia.org/wiki/Vatti_clipping_algorithm



                var targetFile = Path.ChangeExtension(filePath, "zip");

                using (var zip = ZipFile.Open(targetFile, ZipArchiveMode.Create))
                {
                    foreach (var svgLayer in split)
                    {
                        var entry = zip.CreateEntry(svgLayer.name, CompressionLevel.Optimal);
                        var offset = DateTimeOffset.FromUnixTimeSeconds(svgLayer.width).AddYears(30); //Date needs to be min 1980
                        entry.LastWriteTime = offset; //I'm cheating and storing width as date, muhahaha

                        using (Stream destination1 = entry.Open())
                        using (Stream source = svgLayer.content.GetStream())
                            source.CopyTo(destination1);
                    }
                }
            }
        }

        /// <summary>
        /// Split and recompress raw SVGZ into new zip format
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task ProcessRawSVGZ(string filePath)
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                using (GZipStream decompressionStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    var split = await Task.Run(() => SplitSVG(decompressionStream));

                    var targetFile = Path.ChangeExtension(filePath, "zip");

                    using (var zip = ZipFile.Open(targetFile, ZipArchiveMode.Create))
                    {
                        foreach (var svgLayer in split)
                        {
                            var entry = zip.CreateEntry(svgLayer.name, CompressionLevel.Optimal);
                            var offset = DateTimeOffset.FromUnixTimeSeconds(svgLayer.width).AddYears(30); //Date needs to be min 1980
                            entry.LastWriteTime = offset; //I'm cheating and storing width as date, muhahaha

                            using (Stream destination1 = entry.Open())
                            using (Stream source = svgLayer.content.GetStream())
                                source.CopyTo(destination1);
                        }
                    }
                }
            }
        }


        private static async Task<List<SvgLayer>> ParseSvgLayers(string filePath)
        {
            if (Path.GetExtension(filePath) == ".zip")
            {

                var zip = ZipFile.Open(filePath, ZipArchiveMode.Read);
                var zipRef = new SharedDisposable<ZipArchive>(zip); //This ref will later do the disposing

                List<SvgLayer> ret = new List<SvgLayer>();

                foreach (var zipArchiveEntry in zip.Entries)
                {
                    var layer = new SvgLayer();
                    layer.name = zipArchiveEntry.FullName;
                    layer.content = new MapLayerDataZipFile(zipArchiveEntry, zipRef.Acquire());
                    layer.width = (int)zipArchiveEntry.LastWriteTime.Add(zipArchiveEntry.LastWriteTime.Offset).AddYears(-30).ToUnixTimeSeconds();
                    ret.Add(layer);
                }

                return ret;
            }

            if (Path.GetExtension(filePath) == ".svgz")
            {
                //process raw first
                await Task.Run(() => ProcessRawSVGZ(filePath));
                filePath = Path.ChangeExtension(filePath, "zip");
                return await Task.Run(() => ParseSvgLayers(filePath)); //Force seperate thread, as this is slow
            }

            if (Path.GetExtension(filePath) == ".svg")
            {
                //process raw first
                await Task.Run(() => ProcessRawSVG(filePath));
                filePath = Path.ChangeExtension(filePath, "zip");
                return await Task.Run(() => ParseSvgLayers(filePath)); //Force seperate thread, as this is slow
            }

            return null;
        }

        private static List<SvgLayer> SplitSVG(Stream input)
        {
            List<SvgLayer> ret = new List<SvgLayer>();
            using (var reader = XmlReader.Create(input, xmlReaderSettings, CreateSvgXmlContext()))
            {
                var xdoc = XDocument.Load(reader);
                var svg = xdoc.Root;
                var ns = svg.Name.Namespace;

                var mainAttributes = svg.Attributes();
                var defs = svg.Element(ns + "defs");


                var widthAttr = svg.Attribute("width");

                var terrainWidth = int.Parse(widthAttr.Value);


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

                //var test = bareDoc.ToString();
                foreach (var xElement in layers)
                {
                    if (xElement.Attribute("id").Value == "grid") continue;
                    XDocument newDoc = new XDocument(bareDoc);
                    newDoc.Root.Add(xElement);
                    SvgLayer x = new SvgLayer
                    {
                        name = xElement.Attribute("id").Value,
                        content = new MapLayerDataMemory(newDoc.ToString()),
                        width = terrainWidth
                    };
                    ret.Add(x);
                }
            }

            return ret;
        }




    }
}
