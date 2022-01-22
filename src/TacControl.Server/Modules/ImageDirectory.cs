using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TacControl.Common.Maps;
using Path = System.IO.Path;

namespace TacControl.Server.Modules
{
    abstract class ITextureFile
    {

        public int width;
        public int height;
        public string path;

        public abstract string ToBase64();
    }


    class TextureFileDirect : ITextureFile
    {
        private readonly string _filePath;
        public TextureFileDirect(string filePath)
        {
            _filePath = filePath;

            using (var fileStream = System.IO.File.Open(_filePath, FileMode.Open))
            using (var reader = new BinaryReader(fileStream))
            {
                width = reader.ReadInt32();
                height = reader.ReadInt32();
                path = reader.ReadString();
            }
        }

        public override string ToBase64()
        {
            using (var fileStream = System.IO.File.Open(_filePath, FileMode.Open))
            using (var reader = new BinaryReader(fileStream))
            {

                reader.ReadInt32(); // width
                reader.ReadInt32(); // height
                reader.ReadString(); // path

                var buffer = new byte[width * height * 4];

                reader.Read(buffer, 0, buffer.Length);

                return Convert.ToBase64String(buffer, 0, buffer.Length);
            }
        }
    }

    class TextureFileZip : ITextureFile, IDisposable
    {
        private ZipArchiveEntry _zipArchiveEntry;
        private SharedDisposable<ZipArchive>.Reference _reference;


        public TextureFileZip(ZipArchiveEntry zipArchiveEntry, SharedDisposable<ZipArchive>.Reference reference)
        {
            _zipArchiveEntry = zipArchiveEntry;
            this._reference = reference;

            using (var fileStream = _zipArchiveEntry.Open())
            using (var reader = new BinaryReader(fileStream))
            {
                width = reader.ReadInt32();
                height = reader.ReadInt32();
                path = reader.ReadString();
            }
        }



        public override string ToBase64()
        {
            using (var fileStream = _zipArchiveEntry.Open())
            using (var reader = new BinaryReader(fileStream))
            {

                reader.ReadInt32(); // width
                reader.ReadInt32(); // height
                reader.ReadString(); // path

                var buffer = new byte[width * height * 4];

                reader.Read(buffer, 0, buffer.Length);

                return Convert.ToBase64String(buffer, 0, buffer.Length);
            }
        }

        public void Dispose()
        {
            _reference?.Dispose();
        }
    }



    class ImageDirectory : IModule, IMessageReceiver
    {
        private Dictionary<string, ITextureFile> ImageCache = new();


        public void ModuleInit()
        {
            LoadImagesFromZip(Path.Combine(Directory.GetCurrentDirectory(), "markers.zip"));
        }

        public string MessageReceiverName => "ImgDir";

        public void OnNetMessage(IEnumerable<string> function, JObject arguments, Action<string> replyFunc)
        {

            if (function.First() == "RequestTexture")
            {
                var path = arguments["path"].Value<string>();



                JObject msg = new JObject();

                msg["cmd"] = new JArray("ImgDir", "TextureFile");
                var args = msg["args"] = new JObject();
                args["path"] = path;
                

                if (ImageCache.ContainsKey(path))
                {
                    args["data"] = ImageCache[path].ToBase64();
                    args["width"] = ImageCache[path].width;
                    args["height"] = ImageCache[path].height;
                }
                else
                {
                    args["data"] = "";
                    args["width"] = 0;
                    args["height"] = 0;
                }

                var reply = msg.ToString();
                replyFunc(reply);


            }
            else if (function.First() == "RequestMapfile")
            {
                //#TODO properly parse args and return the requested mapfile, not the current active one
                JObject msg = new JObject();

                msg["cmd"] = new JArray("ImgDir", "MapFile");
                var args = msg["args"] = new JObject();

                var wantedMap = arguments["name"];


                string wantedDirectory = Path.Combine(Directory.GetCurrentDirectory(), "maps");
                string filePath = "";
                if (File.Exists(Path.Combine(wantedDirectory, wantedMap + ".zip")))
                {
                    filePath = Path.Combine(wantedDirectory, wantedMap + ".zip");
                    args["name"] = $"{wantedMap}.zip";
                }
                else if (File.Exists(Path.Combine(wantedDirectory, wantedMap + ".svgz")))
                {
                    filePath = Path.Combine(wantedDirectory, wantedMap + ".svgz");
                    args["name"] = $"{wantedMap}.svgz";
                }
                else if(File.Exists(Path.Combine(wantedDirectory, wantedMap + ".svg")))
                { 
                    filePath = Path.Combine(wantedDirectory, wantedMap + ".svg");
                    args["name"] = $"{wantedMap}.svg";
                }
                else
                    Debugger.Break();

                args["data"] = Convert.ToBase64String(System.IO.File.ReadAllBytes(filePath));

                var reply = msg.ToString();
                replyFunc(reply);
                
            }
        }

        //public void LoadImagesFromDirectory(DirectoryInfo directorySelected)
        //{
        //    foreach (FileInfo fileToLoad in directorySelected.GetFiles())
        //    {
        //        if (fileToLoad.Extension != ".tcimg") continue;
        //
        //        var newFile = new TextureFileDirect(fileToLoad.FullName);
        //
        //        ImageCache.Add(newFile.path, newFile);
        //    }
        //}

        public void LoadImagesFromZip(string zipFilePath)
        {
            var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Read);
            var zipRef = new SharedDisposable<ZipArchive>(zip); //This ref will later do the disposing

            List<Helper.SvgLayer> ret = new List<Helper.SvgLayer>();

            foreach (var zipArchiveEntry in zip.Entries)
            {

                //if (zipArchiveEntry.FullName != ".tcimg") continue;

                var newFile = new TextureFileZip(zipArchiveEntry, zipRef.Acquire());

                ImageCache.Add(newFile.path, newFile);
            }
        }


    }
}
