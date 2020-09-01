using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using TacControl.Common.Annotations;

namespace TacControl.Common.Modules
{
    public class ModuleVehicle : INotifyPropertyChanged
    {

        public class VehicleProperties : INotifyPropertyChanged
        {

            public bool EngineOn { get; set; }
            public float AnimAltBaro { get; set; }
            public float AnimAltRadar { get; set; }
            public float AnimAltSurface { get; set; }
            // 1 == in, 0 == out
            public float AnimGear { get; set; }
            public float AnimGearReversed => 1 - AnimGear;
            public float AnimHorizonBank { get; set; }
            public float AnimHorizonDive { get; set; }
            public float AnimVertSpeed { get; set; }
            public float AnimFuel { get; set; }
            public float AnimEngineTemp { get; set; }
            public float AnimRpm { get; set; }
            public float AnimSpeed { get; set; }
            public float AnimRotorHFullyDestroyed { get; set; }
            public float AnimTailRotorHFullyDestroyed { get; set; }
            public float AnimActiveSensorsOn { get; set; }
            public bool AutoHover { get; set; }
            public bool LightOn { get; set; }
            public bool CollisionLight { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged1([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public VehicleProperties props { get; set; } = new VehicleProperties();

        public class VehicleCrewMember
        {
            public string name { get; set; }
            public string role { get; set; }
            public int cargoIndex { get; set; }
        }

        public ObservableCollection<VehicleCrewMember> crew { get; set; } = new ObservableCollection<VehicleCrewMember>();

        public bool IsInVehicle { get; set; }

        private void vehicleSet(string type, bool value)
        {
            Networking.Instance.SendMessage(
                $@"{{
                        ""cmd"": [""Vehicle"", ""Do""],
                        ""args"": {{
                            ""type"": {JsonConvert.ToString(type)},
                            ""state"": {(value ? "true" : "false")}
                        }}
                    }}"
            );
        }

        public void SetActiveSensors(bool value) => vehicleSet("ActiveSensors", value);
        public void SetAutoHover(bool value) => vehicleSet("AutoHover", value);
        public void SetCollisionLight(bool value) => vehicleSet("CollisionLight", value);
        public void SetEngine(bool value) => vehicleSet("Engine", value);
        public void SetGear(bool value) => vehicleSet("Gear", value);
        public void SetHook(bool value) => vehicleSet("Hook", value);
        public void SetLight(bool value) => vehicleSet("Light", value);
        public void SetWheelsBrake(bool value) => vehicleSet("WheelsBrake", value);







        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
