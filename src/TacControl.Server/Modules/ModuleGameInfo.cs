using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TacControl.Server.Modules
{
    class ModuleGameInfo : Common.Modules.ModuleGameInfo, IModule, IStateHolder, IMessageReceiver
    {

        public void ModuleInit()
        {
            worldName = "regero"; //#TODO parameter
        }

        public string StateHolderName => "GameInfo";
        public void SerializeState(JObject ar)
        {
            ar["worldName"] = worldName;
            //playerid is handled in Networking client
            ar["players"] = JObject.FromObject(players);
        }

        public string MessageReceiverName => "GameInfo";
        public void OnNetMessage(IEnumerable<string> function, JObject arguments, Action<string> replyFunc)
        {

        }


        public void OnPlayerJoined(UInt64 playerId, string playerName)
        {
            players.Add(playerId, playerName);
            GameState.Instance.UpdateState(StateHolderName);
        }
        public void OnPlayerLeft(UInt64 playerId, string playerName)
        {
            players.Remove(playerId);
            GameState.Instance.UpdateState(StateHolderName);
        }
    }
}
