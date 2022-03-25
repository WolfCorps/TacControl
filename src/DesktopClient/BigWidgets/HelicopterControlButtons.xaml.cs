using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TacControl.Common;
using TacControl.Common.Modules;

namespace TacControl
{

    public class BoolToButtonColorRedGreen : IValueConverter
    {
        static SolidColorBrush Green = new SolidColorBrush(Colors.Green);
        static SolidColorBrush Red = new SolidColorBrush(Colors.Red);


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool state)
                return state ? Green : Red;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public partial class HelicopterControlButtons : UserControl
    {
        public ModuleVehicle VecRef { get; } = GameState.Instance.vehicle;

        public HelicopterControlButtons()
        {
            InitializeComponent();

            GameState.Instance.core.UpdateInterest("VehAnim", true);
        }

        ~HelicopterControlButtons()
        {
            GameState.Instance.core.UpdateInterest("VehAnim", false);
        }

        private void GearButton_OnClick(object sender, RoutedEventArgs e)
        {
            var gearState = VecRef.props.AnimGear <= 0.05;
            VecRef.SetGear(!gearState);
        }

        private void EngineButton_OnClick(object sender, RoutedEventArgs e)
        {
            VecRef.SetEngine(!VecRef.props.EngineOn);
        }

        private void AutoHoverButton_OnClick(object sender, RoutedEventArgs e)
        {
            VecRef.SetAutoHover(!VecRef.props.AutoHover);
        }

        private void DoHookButton_OnClick(object sender, RoutedEventArgs e)
        {
            VecRef.SetHook(true);
        }

        private void LightButton_OnClick(object sender, RoutedEventArgs e)
        {
            VecRef.SetLight(!VecRef.props.LightOn);
        }

        private void CollisionLight_OnClick(object sender, RoutedEventArgs e)
        {
            VecRef.SetCollisionLight(!VecRef.props.CollisionLight);
        }
    }
}
