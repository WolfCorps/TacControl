using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
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
            public float AnimGear { get; set; }
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



        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
