using NetTopologySuite.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using PropertyChanged;
using TacControl.Common.Annotations;

namespace TacControl.Common.Modules.Streams
{
    public class StreamAircraftState : INotifyPropertyChanged
    {
        public float rpm { get; set; }
        public float altBaro { get; set; }
        public float altRadar { get; set; }
        public float aoa { get; set; }
        public float fuel { get; set; }
        public float speed { get; set; }
        public float vertSpeed { get; set; }
        public float horizonBank { get; set; }

        public float horizonBankDeg => -RadToDeg(horizonBank);

        public float horizonDive { get; set; }

        public float horizonDiveDeg => RadToDeg(horizonDive);

        public ObservableCollection<float> velocityMS { get; set; } = new ObservableCollection<float>(new float[] { 0, 0, 0 });
        public ObservableCollection<float> vecDir { get; set; } = new ObservableCollection<float>(new float[]{0,0,0});
        public float dir { get; set; }

        public double RollAngle => horizonBankDeg;
        public double PitchAngle => horizonDiveDeg;
        public double YawAngle => dir;
        public double ClimbRateArrowMag => 20;

        float RadToDeg(float rad)
        {
            return (rad * 180f / (float) Math.PI);
        }
        double RadToDeg(double rad)
        {
            return (rad * 180f / Math.PI);
        }

        // https://ntrs.nasa.gov/api/citations/20180001968/downloads/20180001968.pdf
        // 𝑢, 𝑣, 𝑤 x, y, and z body relative velocity vector components

        [DependsOn("velocityMS")]
        public double Alpha => Math.Abs(speed) < 0.1 ? 0 : -RadToDeg(Math.Atan(velocityMS[2]/velocityMS[1]));
        [DependsOn("velocityMS")]
        public double Beta => Math.Abs(speed) < 0.1 ? 0 : RadToDeg(Math.Asin(velocityMS[0] / Math.Sqrt(Math.Pow(velocityMS[0], 2) + Math.Pow(velocityMS[1], 2) + Math.Pow(velocityMS[2], 2))));


        public float SlipBallX => Math.Clamp(velocityMS[0], -10, 10);
        public float SlipBallY => Math.Clamp(velocityMS[1], -10, 10);



        //RollAngle="{Binding AirStateRef., ElementName=HelicopterControlButtonsCtrl, Mode=OneWay}"
        //PitchAngle="{Binding AirStateRef., ElementName=HelicopterControlButtonsCtrl, Mode=OneWay}"
        //YawAngle="{Binding AirStateRef., ElementName=HelicopterControlButtonsCtrl, Mode=OneWay}"


        public StreamAircraftState()
        {
            velocityMS.CollectionChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(Alpha));
                OnPropertyChanged(nameof(Beta));
                OnPropertyChanged(nameof(SlipBallX));
                OnPropertyChanged(nameof(SlipBallY));
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
