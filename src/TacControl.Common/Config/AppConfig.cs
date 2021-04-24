using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PropertyChanged;
using TacControl.Common.Annotations;
using TacControl.Common.Config.Converter;

namespace TacControl.Common.Config
{



    public interface IConfigSection
    {
        IConfigSection GetSubSection(IEnumerable<string> path);
        object GetEntry(string entryName);

        event PropertyChangedEventHandler PropertyChanged;
    }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class ConfigSectionAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class ConfigEntryAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class ConfigNoSerializeAttribute : System.Attribute
    {
    }

    public class ConfigSectionBase : IConfigSection, INotifyPropertyChanged
    {
        public IConfigSection GetSubSection(IEnumerable<string> path)
        {
            var foundSubsection = this.GetType().GetProperty(path.First());

            if (foundSubsection == null)
            {
                // #TODO error?
                if (Debugger.IsAttached) Debugger.Break();
                return null;
            }

            if (!foundSubsection.GetCustomAttributes(typeof(ConfigSectionAttribute), false).Any())
            {
                // #TODO error?
                if (Debugger.IsAttached) Debugger.Break();
                return null;
            }


            //var subSections = this.GetType().FindMembers(MemberTypes.Property, BindingFlags.Default, (x,_) => x.CustomAttributes.Any(y => y.AttributeType == typeof(ConfigSectionAttribute)), null) as PropertyInfo[];
            //
            //var foundSubsection = subSections.FirstOrDefault(x => x.Name.Equals(path.First(), StringComparison.CurrentCultureIgnoreCase));
            //
            //if (foundSubsection == null)
            //{
            //    // #TODO error?
            //    if (Debugger.IsAttached) Debugger.Break();
            //    return null;
            //}

            // iterate further if we can
            path = path.Skip(1);

            if (!path.Any()) // end
                return foundSubsection.GetValue(this) as IConfigSection;

            return ((IConfigSection)foundSubsection.GetValue(this)).GetSubSection(path);
        }

        public object GetEntry(string entryName)
        {
            var property = this.GetType().GetProperty(entryName);

            if (property == null)
            {
                // #TODO error?
                if (Debugger.IsAttached) Debugger.Break();
                return null;
            }

            return property.GetValue(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        internal void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyName)
        {
            PropertyChanged?.Invoke(sender, propertyName);
        }
    }



    public class RootConfig : ConfigSectionBase, INotifyPropertyChanged
    {

        [ConfigSection]
        public Section.Networking Networking { get; set; }

        [ConfigSection]
        public Section.Map Map { get; set; }
    }


    public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member,
            MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (member.GetCustomAttribute<ConfigNoSerializeAttribute>() != null)
            {
                property.ShouldSerialize = instance =>
                {
                    return false;
                };
            }

            return property;
        }
    }


    public class AppConfig : IDisposable
    {
        public static AppConfig Instance = new AppConfig();




        public RootConfig Root { get; set; } = new RootConfig();
        public string ConfigDirectory { get; private set; }

        private string _configFile;

        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings();

        private AppConfig()
        {
            // Use Arma 3 appdata, except on Android
            ConfigDirectory = Path.Combine(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Arma 3"), "TacControl");

            if (System.Environment.OSVersion.Platform == PlatformID.Unix) //Android
            {
                ConfigDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            }

            System.IO.Directory.CreateDirectory(ConfigDirectory);

            // Initialize Tree Sections

            void InitSection(ConfigSectionBase section)
            {
                var subSections = section.GetType().GetProperties().Where(x => x.GetCustomAttributes(typeof(ConfigSectionAttribute)).Any());

                foreach (var propertyInfo in subSections)
                {

                    if (!typeof(ConfigSectionBase).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        // Property has ConfigSection attribute but doesn't implement IConfigSection interface
                        if (Debugger.IsAttached) Debugger.Break();
                    }

                    var newValue = Activator.CreateInstance(propertyInfo.PropertyType) as ConfigSectionBase;
                    propertyInfo.SetValue(section, newValue);

                    // cascade property changed events upwards
                    newValue.PropertyChanged += (x,y) => section.OnPropertyChanged(x, new PropertyChangedEventArgs($"{propertyInfo.Name}.{y.PropertyName}"));

                    InitSection(newValue);
                }
            }

            InitSection(Root);


            // Find json converters
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.IsClass && t.Namespace == "TacControl.Common.Config.Converter" && t.BaseType == typeof(JsonConverter)
                select t;

            foreach (var type in q)
            {
                _jsonSerializerSettings.Converters.Add(Activator.CreateInstance(type) as JsonConverter);
            }

            _jsonSerializerSettings.Formatting = Formatting.Indented;
            _jsonSerializerSettings.ContractResolver = new ShouldSerializeContractResolver();

            _configFile = Path.Combine(ConfigDirectory, "main.json");
            LoadConfig();

            Root.PropertyChanged += (x, y) =>
            {
                SaveConfig();
            };



        }


        private void LoadConfig()
        {
            if (!File.Exists(_configFile))
                return;


            var jsonFile = File.ReadAllText(_configFile);

            JsonConvert.PopulateObject(jsonFile, Root, _jsonSerializerSettings);
        }

        private void SaveConfig()
        {
            File.WriteAllText(_configFile, JsonConvert.SerializeObject(Root, _jsonSerializerSettings));
        }
        
        public IConfigSection GetSection(string sectionPath)
        {
            return Root.GetSubSection(sectionPath.Split('.').AsEnumerable());
        }

        public T GetSection<T>(string sectionPath) where T: IConfigSection
        {
            var section = Root.GetSubSection(sectionPath.Split('.').AsEnumerable());

            if (section == null)
            {
                // #TODO error?
                if (Debugger.IsAttached) Debugger.Break();
                return default(T);
            }

            if (!(section is T))
            {
                // #TODO error?
                if (Debugger.IsAttached) Debugger.Break();
                return default(T);
            }

            return (T)section;
        }


        public T GetEntry<T>(string sectionPath)
        {
            var path = sectionPath.Split('.').AsEnumerable();
            var subSection = Root.GetSubSection(path.SkipLast());
            if (subSection == null)
            {
                // #TODO error?
                if (Debugger.IsAttached) Debugger.Break();
                return default(T);
            }

            var entry = subSection.GetEntry(path.Last());

            if (!(entry is T))
            {
                // #TODO error?
                if (Debugger.IsAttached) Debugger.Break();
                return default(T);
            }

            return (T)entry;
        }

        public void Dispose()
        {
            SaveConfig();
        }
    }
}
