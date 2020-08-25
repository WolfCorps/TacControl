// /*
// * Copyright (C) Dedmen Miller @ R4P3 - All Rights Reserved
// * Unauthorized copying of this file, via any medium is strictly prohibited
// * Proprietary and confidential
// * Written by Dedmen Miller <dedmen@dedmen.de>, 08 2016
// */

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace TacControl.Common.Modules
{
    public class ModuleMarker : INotifyPropertyChanged
    {
        public class MarkerType
        {
            public string name { get; set; }
            public string color { get; set; }
            public UInt32 size { get; set; }
            public bool shadow { get; set; }
            public string icon { get; set; }
        }

        public class MarkerColor
        {
            public string name { get; set; }
            public string color { get; set; }

            public SKColor ToSKColor()
            {
                CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                ci.NumberFormat.NumberDecimalSeparator = ".";

                var colorArr = color.Trim('[', ']').Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToList();

                return new SKColor((byte) (colorArr[0] * 255), (byte) (colorArr[1] * 255), (byte) (colorArr[2] * 255), (byte)(colorArr[3] * 255));
            }

            public Mapsui.Styles.Color ToMapsuiColor()
            {
                CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                ci.NumberFormat.NumberDecimalSeparator = ".";

                var colorArr = color.Trim('[', ']').Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToList();

                return new Mapsui.Styles.Color((byte)(colorArr[0] * 255), (byte)(colorArr[1] * 255), (byte)(colorArr[2] * 255), (byte)(colorArr[3] * 255));
            }


        }

        public class MarkerBrush
        {
            public string name { get; set; }
            public string texture { get; set; }
            public bool drawBorder { get; set; }
        }

        public class ActiveMarker : INotifyPropertyChanged
        {
            public string id { get; set; }
            public string type { get; set; }
            public string color { get; set; }
            public float dir { get; set; }
            public ObservableCollection<float> pos { get; set; } = new ObservableCollection<float>();
            public string text { get; set; }
            public string shape { get; set; }
            public float alpha { get; set; }
            public string brush { get; set; }
            public string size { get; set; }
            public int channel { get; set; }

            public ObservableCollection<float[]> polyline { get; set; } = new ObservableCollection<float[]>();


            ActiveMarker()
            {
                pos.CollectionChanged += (a, e) =>
                {
                    if (pos.Count > 3) //#TODO this is stupid, need Vector3 support
                        pos.RemoveAt(0);
                    OnPropertyChanged("pos");
                };
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }




        public ObservableDictionary<string, MarkerType> markerTypes { get; set; } = new ObservableDictionary<string, MarkerType>();
        public ObservableDictionary<string, MarkerColor> markerColors { get; set; } = new ObservableDictionary<string, MarkerColor>();
        public ObservableDictionary<string, MarkerBrush> markerBrushes { get; set; } = new ObservableDictionary<string, MarkerBrush>();
        public ObservableDictionary<string, ActiveMarker> markers { get; set; } = new ObservableDictionary<string, ActiveMarker>();


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
