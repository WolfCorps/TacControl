using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Mapsui.Styles;
using Newtonsoft.Json;
using Sentry;
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

            if (color == "Default")
            {
                // #TODO this is wrong, we need to get the default color from somewhere I think this should lookup in MarkerColors?
                if (Debugger.IsAttached) Debugger.Break();
                return SKColors.Black;
            }

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


            try
            {

                var colorArr = color.Trim('[', ']').Split(',').Select(xy => float.Parse(xy, NumberStyles.Any, ci))
                    .ToList();

                if (colorArr.Count != 4)
                {
                    SentrySdk.CaptureException(new System.ArgumentOutOfRangeException($"Marker color {color} doesn't have 4 elements!"));
                    return SKColors.Black;
                }

                return new SKColor((byte) (colorArr[0] * 255), (byte) (colorArr[1] * 255), (byte) (colorArr[2] * 255),
                    (byte) (colorArr[3] * 255));

            }
            catch (System.FormatException ex)
            {
                SentrySdk.AddBreadcrumb($"color = {color}");
                SentrySdk.CaptureException(ex);
                return SKColors.Black;
            }
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
                if (pos.Count > 2) //#TODO this is stupid, need Vector3 support
                    pos.RemoveAt(0);
                OnPropertyChanged("pos");
            };
        }

        public void SetPos(float X, float Y)
        {
            pos[0] = X;
            pos[1] = Y;
            OnPropertyChanged("pos");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // retrieve direct play ID of the player that placed this marker, if it was player placed.
        public UInt64 GetDPID()
        {
            // $"_USER_DEFINED #{playerDirectPlayID}/TC_{timeStamp}/{(int) channel}";

            if (!id.StartsWith("_USER_DEFINED")) return 0;
            var start = id.IndexOf("#") + 1;
            var length = id.IndexOf("/") - id.IndexOf("#") - 1;
            var dpIdString = id.Substring(start, length);

            return UInt64.Parse(dpIdString);
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
        
        public ObservableDictionary<string, MarkerType> markerTypes { get; set; } = new ObservableDictionary<string, MarkerType>(StringComparer.OrdinalIgnoreCase);
        public ObservableDictionary<string, MarkerColor> markerColors { get; set; } = new ObservableDictionary<string, MarkerColor>(StringComparer.OrdinalIgnoreCase);
        public ObservableDictionary<string, MarkerBrush> markerBrushes { get; set; } = new ObservableDictionary<string, MarkerBrush>(StringComparer.OrdinalIgnoreCase);
        public ObservableDictionary<string, ActiveMarker> markers { get; set; } = new ObservableDictionary<string, ActiveMarker>(StringComparer.OrdinalIgnoreCase);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string GenerateMarkerName(MarkerChannel channel)
        {
            Int32 timeStamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(2020, 08, 01))).TotalSeconds;
            return $"_USER_DEFINED #{GameState.Instance.gameInfo.playerID}/TC_{timeStamp}/{(int) channel}";
        }

        public void CreateMarker(ActiveMarker markerRef)
        {

            if (markerRef.polyline.Count != 0 && markerRef.polyline.Count < 2)
                return;

            //#TODO https://www.wpf-tutorial.com/wpf-application/application-culture-uiculture/
            //keep android in mind? If we set it in common in GameState init that should be fine
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";
            Networking.Instance.SendMessage(
                $@"{{
                        ""cmd"": [""Marker"", ""CreateMarker""],
                        ""args"": {{
                            ""id"": {JsonConvert.ToString(markerRef.id)},
                            ""type"": {JsonConvert.ToString(markerRef.type)},
                            ""color"": {JsonConvert.ToString(markerRef.color)},
                            ""dir"": {markerRef.dir.ToString(ci)},
                            ""pos"": [{markerRef.pos[0].ToString(ci)},{markerRef.pos[1].ToString(ci)}],
                            ""text"": {JsonConvert.ToString(markerRef.text)},
                            ""shape"": {JsonConvert.ToString(markerRef.shape)},
                            ""alpha"": {markerRef.alpha},
                            ""brush"": {JsonConvert.ToString(markerRef.brush)},
                            ""size"": {JsonConvert.ToString($"[{markerRef.size.ToString(ci)}]")},
                            ""channel"": {markerRef.channel},
                            ""polyline"": [{(markerRef.polyline.Count == 0 ? "" : markerRef.polyline.Select(x => $"[{x[0].ToString(ci)},{x[1].ToString(ci)}]").Aggregate((i,j) => i+","+j))}]
                        }}
                    }}"
            );
        }

        public void EditMarker(ActiveMarker markerRef)
        {

            if (markerRef.polyline.Count != 0 && markerRef.polyline.Count < 2)
            {
                // github #8 and #6
                if (Debugger.IsAttached) Debugger.Break();
                return;
            }

            //#TODO https://www.wpf-tutorial.com/wpf-application/application-culture-uiculture/
            //keep android in mind? If we set it in common in GameState init that should be fine
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";
            Networking.Instance.SendMessage(
                $@"{{
                        ""cmd"": [""Marker"", ""EditMarker""],
                        ""args"": {{
                            ""id"": {JsonConvert.ToString(markerRef.id)},
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
                            ""polyline"": [{(markerRef.polyline.Count == 0 ? "" : markerRef.polyline.Select(x => $"[{x[0].ToString(ci)},{x[1].ToString(ci)}]").Aggregate((i, j) => i + "," + j))}]
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
                            ""id"": {JsonConvert.ToString(markerRef.id)}
                        }}
                    }}"
            );
        }
        public void SerializeMarkers(JsonWriter output)
        {
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Serialize(output, this.markers);
        }

        public void DeserializeMarkers(JsonReader input)
        {
            var jsonSerializer = new JsonSerializer();
            var data = jsonSerializer.Deserialize<ObservableDictionary<string, ActiveMarker>>(input);
            foreach (var keyValuePair in data)
            {
                markers[keyValuePair.Key] = keyValuePair.Value;
            }
        }


        public void SerializeTypes(JsonWriter output)
        {
            var jsonSerializer = new JsonSerializer();
            output.WriteStartObject();
            output.WritePropertyName("markerTypes");
            jsonSerializer.Serialize(output, markerTypes);
            output.WritePropertyName("markerColors");
            jsonSerializer.Serialize(output, markerColors);
            output.WritePropertyName("markerBrushes");
            jsonSerializer.Serialize(output, markerBrushes);
            output.WriteEndObject();
        }


        public void DeserializeTypes(JsonTextReader input)
        {
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Populate(input, this);
        }
    }
}
