// /*
// * Copyright (C) Dedmen Miller @ R4P3 - All Rights Reserved
// * Unauthorized copying of this file, via any medium is strictly prohibited
// * Proprietary and confidential
// * Written by Dedmen Miller <dedmen@dedmen.de>, 08 2016
// */

using System.ComponentModel;
using System.Runtime.CompilerServices;
using TacControl.Common.Modules;

namespace TacControl.Common
{
    public class GameState : INotifyPropertyChanged
    {
        public static GameState Instance = new GameState();

        public ModuleRadio radio { get; set; } = new ModuleRadio();
        public ModuleGPS gps { get; set; } = new ModuleGPS();

        public ModuleMarker marker { get; set; } = new ModuleMarker();

        public ModuleGameInfo gameInfo { get; set; } = new ModuleGameInfo();

        public ModuleNote note { get; set; } = new ModuleNote();
        public ModuleVehicle vehicle { get; set; } = new ModuleVehicle();
        public ModuleACE ace { get; set; } = new ModuleACE();

        public void test()
        {
            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
