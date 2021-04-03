using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TacControl.Server.Modules
{
    interface IModule
    {
        virtual void ModulePreInit() { }
        void ModuleInit();
        virtual void ModulePostInit() { }
    }

    interface IStateHolder
    {
        string StateHolderName { get; }
        abstract void SerializeState(JObject ar);

        public void SendStateUpdate()
        {
            GameState.Instance.UpdateState(StateHolderName);
        }
    };

    interface IMessageReceiver
    {
        string MessageReceiverName { get; }
        abstract void OnNetMessage(IEnumerable<string> function, JObject arguments, Action<string> replyFunc);
    }




}
