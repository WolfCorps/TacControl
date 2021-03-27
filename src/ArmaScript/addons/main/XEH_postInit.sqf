#include "script_component.hpp"


"TacControl" callExtension "postInit";

["GameInfo.PlayerId", [getPlayerId player]] call TC_main_fnc_sendMessage;

{
    ["GameInfo.PlayerJoined", [name player, getPlayerId player]] call TC_main_fnc_sendMessage;
} forEach allPlayers;

// Needs to run serverside :/
// stupid hack


{
    if (!isNil "TC_ServersideEHSet") exitWith {};

    addMissionEventHandler ["PlayerConnected",
    {
        params ["", "", "_name", "", "", "_idstr"];
        [QGVAR(PlayerConnected), [_name, _idstr]] call CBA_fnc_remoteEvent;
    }];

    addMissionEventHandler ["PlayerDisconnected",
    {
        params ["", "", "_name", "", "", "_idstr"];
        [QGVAR(PlayerDisconnected), _idstr] call CBA_fnc_remoteEvent;
    }];
} remoteExec ["call", 2];

[QGVAR(PlayerConnected), {
    // _this == [name player, player DPID]
    ["GameInfo.PlayerJoined", _this] call TC_main_fnc_sendMessage;
}] call CBA_fnc_addEventHandler;

[QGVAR(PlayerConnected), {
    // _this == player DPID
    ["GameInfo.PlayerLeft", _this] call TC_main_fnc_sendMessage;
}] call CBA_fnc_addEventHandler;


call TC_main_fnc_Marker_postInit;
