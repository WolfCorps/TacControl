using System.ComponentModel;
using System.Runtime.CompilerServices;
using TacControl.Common.Modules;

namespace TacControl.Common
{
    public class GameState : INotifyPropertyChanged
    {
        public static GameState Instance = new GameState();

        public ModuleRadio radio { get; private set; } = new ModuleRadio();
        public ModuleGPS gps { get; private set; } = new ModuleGPS();

        public ModuleMarker marker { get; private set; } = new ModuleMarker();

        public ModuleGameInfo gameInfo { get; private set; } = new ModuleGameInfo();

        public ModuleNote note { get; private set; } = new ModuleNote();
        public ModuleVehicle vehicle { get; private set; } = new ModuleVehicle();
        public ModuleACE ace { get; private set; } = new ModuleACE();
        public ModuleCore core { get; private set; } = new ModuleCore();

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
