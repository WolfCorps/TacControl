using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TacControl.Common.Modules.Streams;

namespace TacControl.Common.Modules
{
    public class ModuleStreamManager
    {
        public class CollectionClearingContractResolver : DefaultContractResolver
        {
            protected override JsonArrayContract CreateArrayContract(Type objectType)
            {
                var c = base.CreateArrayContract(objectType);



                c.OnDeserializingCallbacks.Add((obj, streamingContext) =>
                {
                    if (obj is IList {IsReadOnly: false} list)
                        list.Clear();
                });

                c.OnDeserializedCallbacks.Add((obj, streamingContext) =>
                {
                    if (obj is ObservableCollection<float> list)
                        list.Clear();
                });

                return c;
            }
        }



        public StreamAircraftState S_AirState { get; set; } = new StreamAircraftState();


        public void OnStreamUpdate(string streamName, JObject jObject)
        {
            var jsonSerializer = new JsonSerializer();
            //jsonSerializer.ContractResolver = new CollectionClearingContractResolver();

            jsonSerializer.Converters.Add(new KeyedIListMergeConverter(jsonSerializer.ContractResolver));

            jsonSerializer.Populate(new JTokenReader(jObject), S_AirState);
        }
    }
}
