using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using TacControl.Common.Config;
using Formatting = Newtonsoft.Json.Formatting;

namespace TacControl.Misc
{
    public class JsonLayoutSerializer : LayoutSerializer
    {
        private class ShouldSerializeContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member,
                MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                //if (member.Name == "SelectedContent")
                //    Debugger.Break();
                

                if (
                    member.GetCustomAttribute<XmlIgnoreAttribute>() != null ||
                    member.GetCustomAttribute<NonSerializedAttribute>() != null ||
                    (member.MemberType == MemberTypes.Property && !((PropertyInfo)member).CanWrite)
                )
                {
                    property.ShouldSerialize = instance =>
                    {
                        return false;
                    };
                }

                return property;
            }
        }
        
        private JsonSerializer _serializer = new JsonSerializer();

        public JsonLayoutSerializer(DockingManager manager)
          : base(manager)
        {
            _serializer.ContractResolver = new ShouldSerializeContractResolver();
            _serializer.Formatting = Formatting.Indented;


            


        }
        
        public void Serialize(JsonWriter writer)
        {
            XmlDocument doc = new XmlDocument();
            using (XmlWriter xmlWriter = doc.CreateNavigator().AppendChild())
            {
                new System.Xml.Serialization.XmlSerializer(typeof(LayoutRoot)).Serialize(xmlWriter, (object)this.Manager.Layout);
                XmlNodeConverter xmlNodeConverter = new XmlNodeConverter();
                xmlNodeConverter.WriteJson(writer, doc, _serializer);
            }

            //_serializer.Serialize(writer, (object) this.Manager.Layout);
        }

        public void Serialize(TextWriter writer)
        {
            XmlDocument doc = new XmlDocument();
            using (XmlWriter xmlWriter = doc.CreateNavigator().AppendChild())
            {
                new System.Xml.Serialization.XmlSerializer(typeof(LayoutRoot)).Serialize(xmlWriter, (object)this.Manager.Layout);
            }

            XmlNodeConverter xmlNodeConverter = new XmlNodeConverter();
            xmlNodeConverter.WriteJson(new JsonTextWriter(writer), (XmlNode)doc.FirstChild, _serializer);

            //_serializer.Serialize(writer, (object) this.Manager.Layout);
        }

        public void Serialize(string filepath)
        {
            using (StreamWriter streamWriter = new StreamWriter(filepath))
                this.Serialize((TextWriter)streamWriter);
        }

        public void Deserialize(TextReader reader)
        {
            try
            {
                this.StartDeserialization();

                XmlNodeConverter xmlNodeConverter = new XmlNodeConverter();

                //JsonConvert.DeserializeXNode
                //var test = reader.ReadLine();

                //var jsonReader = new JsonTextReader(reader);
                //jsonReader.Read();


                //var readXml = (XmlDocument)xmlNodeConverter.ReadJson(jsonReader, typeof(XmlDocument), null, _serializer);

                var readXml = JsonConvert.DeserializeXNode(reader.ReadToEnd());
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(readXml.ToString());

                //#TODO this be broke

                LayoutRoot layout = new XmlSerializer(typeof(LayoutRoot)).Deserialize(new XmlNodeReader(doc)) as LayoutRoot;
                this.FixupLayout(layout);
                this.Manager.Layout = layout;
            }
            finally
            {
                this.EndDeserialization();
            }
        }

        public void Deserialize(string filepath)
        {
            using (StreamReader streamReader = new StreamReader(filepath))
                this.Deserialize((TextReader)streamReader);
        }
    }
}
