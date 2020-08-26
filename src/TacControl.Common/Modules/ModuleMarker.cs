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
using Mapsui.Styles;
using Newtonsoft.Json;
using SkiaSharp;

namespace TacControl.Common.Modules
{
    public class MarkerType
    {
        public string name { get; set; }
        public string color { get; set; }
        public UInt32 size { get; set; }
        public bool shadow { get; set; }
        public string icon { get; set; }

        [JsonIgnore]
        public SKImage iconImage { get; set; }

        [JsonIgnore]
        public SKColor Color => ToSKColor();

        public SKColor ToSKColor()
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";

            var colorArr = color.Trim('[', ']').Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToList();
            if (colorArr.Count != 4) return SKColors.White; //wat.. See AiO config "color_Civilian"
            return new SKColor((byte)(colorArr[0] * 255), (byte)(colorArr[1] * 255), (byte)(colorArr[2] * 255), (byte)(colorArr[3] * 255));
        }

        public Mapsui.Styles.Color ToMapsuiColor()
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";

            var colorArr = color.Trim('[', ']').Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToList();

            return new Mapsui.Styles.Color((byte)(colorArr[0] * 255), (byte)(colorArr[1] * 255), (byte)(colorArr[2] * 255), (byte)(colorArr[3] * 255));
        }


    }

    public class MarkerColor
    {
        public string name { get; set; }
        public string color { get; set; }

        [JsonIgnore]
        public SKColor Color => ToSKColor();

        public SKColor ToSKColor()
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";

            var colorArr = color.Trim('[', ']').Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci)).ToList();

            return new SKColor((byte)(colorArr[0] * 255), (byte)(colorArr[1] * 255), (byte)(colorArr[2] * 255), (byte)(colorArr[3] * 255));
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


        public ActiveMarker()
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

    public enum MarkerChannel
    {
        None = -1,
        Global = 0,
        Side = 1,
        Command = 2,
        Group = 3,
        Vehicle = 4,
        Dirct = 5
        //Custom channels 6-15
    }


    public class ModuleMarker : INotifyPropertyChanged
    {
        
        public ObservableDictionary<string, MarkerType> markerTypes { get; set; } = new ObservableDictionary<string, MarkerType>();
        public ObservableDictionary<string, MarkerColor> markerColors { get; set; } = new ObservableDictionary<string, MarkerColor>();
        public ObservableDictionary<string, MarkerBrush> markerBrushes { get; set; } = new ObservableDictionary<string, MarkerBrush>();
        public ObservableDictionary<string, ActiveMarker> markers { get; set; } = new ObservableDictionary<string, ActiveMarker>();

        public string playerDirectPlayID { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string GenerateMarkerName(MarkerChannel channel)
        {
            Int32 timeStamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(2020, 08, 01))).TotalSeconds;
            return $"_USER_DEFINED #{playerDirectPlayID}/TC_{timeStamp}/{(int) channel}";
        }

        public void CreateMarker(ActiveMarker markerRef)
        {
            //#TODO https://www.wpf-tutorial.com/wpf-application/application-culture-uiculture/
            //keep android in mind? If we set it in common in GameState init that should be fine
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";
            Networking.Instance.SendMessage(
                $@"{{
                        ""cmd"": [""Marker"", ""CreateMarker""],
                        ""args"": {{
                            ""name"": {JsonConvert.ToString(markerRef.id)},
                            ""type"": {JsonConvert.ToString(markerRef.type)},
                            ""color"": {JsonConvert.ToString(markerRef.color)},
                            ""dir"": {markerRef.dir},
                            ""pos"": [{markerRef.pos[0].ToString(ci)},{markerRef.pos[1].ToString(ci)}],
                            ""text"": {JsonConvert.ToString(markerRef.text)},
                            ""shape"": {JsonConvert.ToString(markerRef.shape)},
                            ""alpha"": {markerRef.alpha},
                            ""brush"": {JsonConvert.ToString(markerRef.brush)},
                            ""size"": {JsonConvert.ToString($"[{markerRef.size}]")},
                            ""channel"": {markerRef.channel},
                            ""polyline"": [{(markerRef.polyline.Count == 0 ? "" : markerRef.polyline.Select(x => $"[{x[0]},{x[0]}]").Aggregate((i,j) => i+","+j))}]
                        }}
                    }}"
            );
        }
        public void DeleteMarker(ActiveMarker markerRef)
        {
            Networking.Instance.SendMessage(
                $@"{{
                        ""cmd"": [""Marker"", ""DeleteMarker""],
                        ""args"": {{
                            ""mame"": ""{JsonConvert.ToString(markerRef.id)}""
                        }}
                    }}"
            );
        }
    }
}
