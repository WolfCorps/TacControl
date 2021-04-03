using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TacControl.Server.Modules;

namespace TacControl.Server
{
    class GameState : Common.GameState
    {
        public new static GameState Instance = new();

        private List<IModule> modules = new();
        private Dictionary<string, IMessageReceiver> messageReceivers = new();
        private Dictionary<string, IStateHolder> stateHolders = new();

        private JObject jsonState = new();


        public new ModuleGameInfo gameInfo { get; set; }


        GameState()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p =>
                    {
                        if (!p.FullName.Contains("TacControl"))
                            return false;


                        return p.GetInterfaces().Any(i => i == typeof(IModule));


                    }
               
                    );

            foreach (var type in types)
            {
                var newModule = AppDomain.CurrentDomain.CreateInstance(type.Assembly.FullName, type.FullName)?.Unwrap();

                modules.Add(newModule as IModule);

                if (newModule is IMessageReceiver receiver)
                    messageReceivers.Add(receiver.MessageReceiverName, receiver);
                if (newModule is IStateHolder stateHolder)
                {
                    stateHolders.Add(stateHolder.StateHolderName, stateHolder);
                    jsonState[stateHolder.StateHolderName] = new JObject();
                }



                // replace our modules if there is a replacement available
                var myFields = typeof(Common.GameState).GetProperties();

                foreach (var fieldInfo in myFields.Where(x => type.IsSubclassOf(x.PropertyType)))
                {
                    fieldInfo.SetValue(this, newModule);
                }

                myFields = typeof(GameState).GetProperties();

                foreach (var fieldInfo in myFields.Where(x => type == x.PropertyType && x.DeclaringType == typeof(GameState)))
                {
                    fieldInfo.SetValue(this, newModule);
                }


            }
        }

        public void Init()
        {
            modules.ForEach(x => x.ModulePreInit());
            modules.ForEach(x => x.ModuleInit());
            UpdateState();
        }


        public void UpdateState()
        {
            foreach (var (key, holder) in stateHolders)
            {
                holder.SerializeState(jsonState[holder.StateHolderName] as JObject);
            }
            

            Networking.Instance.UpdateState(jsonState);

        }

        public void UpdateState(string stateHolderName)
        {
            if (!stateHolders.ContainsKey(stateHolderName))
                Debugger.Break();

            var holder = stateHolders[stateHolderName];


            holder.SerializeState(jsonState[holder.StateHolderName] as JObject);

            Networking.Instance.UpdateState(jsonState);

        }

        public void TransferNetworkMessage(IEnumerable<string> cmd, JObject args, Action<string> replyFunc)
        {
            if (!messageReceivers.ContainsKey(cmd.First()))
                Debugger.Break();

            var receiver = messageReceivers[cmd.First()];

            receiver.OnNetMessage(cmd.Skip(1), args, replyFunc);


        }
    }
}
