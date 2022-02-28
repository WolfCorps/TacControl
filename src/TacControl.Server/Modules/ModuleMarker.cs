using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TacControl.Common.Modules;

namespace TacControl.Server.Modules
{
    class ActiveMarkerChangeRec : ActiveMarker
    {
        [NonSerialized]
        public bool HasChanged = false;

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            HasChanged = true;
        }
    }



    class ModuleMarker : Common.Modules.ModuleMarker, IModule, IMessageReceiver, IStateHolder
    {

        public void ModuleInit()
        {
            using (var sr = new StreamReader("markerTypes.txt"))
            using (var reader = new JsonTextReader(sr))
            {
                GameState.Instance.marker.DeserializeTypes(reader);
            }



        }

        public string MessageReceiverName => "Marker";
        public void OnNetMessage(IEnumerable<string> function, JObject arguments, Action<string> replyFunc)
        {
            if (function.First() == "CreateMarker")
            {
                var newMarker = arguments.ToObject<ActiveMarkerChangeRec>();

                // "[1,1]" -> "1,1", thats how Arma script returns it
                newMarker.size = newMarker.size.First() == '['
                    ? newMarker.size.Substring(1, newMarker.size.Length - 2)
                    : newMarker.size;


                markers.Add(newMarker.id, newMarker);
                GameState.Instance.UpdateState(StateHolderName);
            }
            else if (function.First() == "EditMarker")
            {
                if (!markers.TryGetValue(arguments["id"].Value<string>(), out var marker))
                    return;

                JsonSerializer.Create().Populate(new JTokenReader(arguments), marker);

                // "[1,1]" -> "1,1", thats how Arma script returns it
                marker.size = marker.size.First() == '['
                    ? marker.size.Substring(1, marker.size.Length - 2)
                    : marker.size;


                GameState.Instance.UpdateState(StateHolderName);
            }
            else if (function.First() == "DeleteMarker")
            {
                if (!markers.TryGetValue(arguments["id"].Value<string>(), out var marker))
                    return;
                markers.Remove(arguments["id"].Value<string>());
                GameState.Instance.UpdateState(StateHolderName);
            }
        }

        public string StateHolderName => "Marker";
        public void SerializeState(JObject ar)
        {
            if (!((IDictionary<string, JToken>) ar).ContainsKey("markerTypes"))
            {
                ar["markerTypes"] = JObject.FromObject(markerTypes);
            }

            if (!((IDictionary<string, JToken>) ar).ContainsKey("markerColors"))
            {
                ar["markerColors"] = JObject.FromObject(markerColors);
            }

            if (!((IDictionary<string, JToken>) ar).ContainsKey("markerBrushes"))
            {
                ar["markerBrushes"] = JObject.FromObject(markerBrushes);
            }


            // state update
            if (((IDictionary<string, JToken>) ar).ContainsKey("markers"))
            {
                var markObj = ar["markers"];

                // remove deleted markers
                foreach (var jToken in markObj.Children().Where(child => !markers.ContainsKey((child as JProperty).Name)).ToList())
                {
                    jToken.Remove();
                }

                foreach (var (key, value) in markers)
                {
                    if (value is ActiveMarkerChangeRec markerChangeRec)
                    {
                        if (markerChangeRec.HasChanged)
                            markObj[key] = JObject.FromObject(value as ActiveMarker);
                        markerChangeRec.HasChanged = false;
                    }
                    else
                    {
                        markObj[key] = JObject.FromObject(value as ActiveMarker);
                    }

                   
                }
            }
            else // Full state
            {
               
                JObject markObj = new JObject();
                ar["markers"] = markObj;

                foreach (var (key, value) in markers)
                {
                    markObj[key] = JObject.FromObject(value as ActiveMarker);
                }
            }


        }
    }
}
