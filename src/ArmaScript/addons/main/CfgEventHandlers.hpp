class Extended_PreStart_EventHandlers {
    class ADDON {
        init = QUOTE(call COMPILE_FILE(XEH_preStart));
    };
};

class Extended_PreInit_EventHandlers {
    class ADDON {
        init = QUOTE(call COMPILE_FILE(XEH_preInit));
    };
};

class Extended_PostInit_EventHandlers {
    class ADDON {
        init = QUOTE(call COMPILE_FILE(XEH_postInit));
    };
};

class Extended_Init_EventHandlers {
    class TC_ACE_Attach_GPSTracker {
        class TC_InitGPSTracker {
            init = "[{[0,_this select 0,0] call TC_main_fnc_GPS_activateTracker}, _this] call CBA_fnc_execNextFrame;";
        };
    };
};