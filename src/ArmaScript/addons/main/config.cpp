#include "script_component.hpp"

class CfgPatches {
    class ADDON {
        name = COMPONENT_NAME;
        units[] = {};
        weapons[] = {};
        requiredVersion = REQUIRED_VERSION;
        requiredAddons[] = {"ace_common", "ace_explosives"};
        author = "";
        authors[] = {"Wolf Corps - Dedmen"};
        authorUrl = "https://wolfcorps.de";
        VERSION_CONFIG;
    };
};

#include "CfgEventHandlers.hpp"


class ACE_Triggers{

    class TC_Activate_Tracker {
        isAttachable = 1;
        displayName = "Activate GPS Tracker";
        picture = "\z\TC\addons\main\data\tk-108_preview.paa";
        onPlace = QUOTE(_this call TC_main_fnc_GPS_activateTracker;false);
    };
};

class CfgMagazines {
    class ATMine_Range_Mag;
    class TC_GPSTracker_Mag : ATMine_Range_Mag {
        ace_explosives_SetupObject = "ACE_Explosives_Place_TC_GPSTracker"; // CfgVehicle class for setup object.
        model =  "\z\TC\addons\main\data\tk-108.p3d";
        picture = "\z\TC\addons\main\data\tk-108_preview.paa";
        displayName = "TK-108 GPS Tracker";
        ammo = "TC_GPSTracker_Ammo";
        mass = 5;
        allowedSlots[] = {901, 801, 701};

descriptionShort = "Type: TK-108 GPS Tracker, Attach on vehicles";
descriptionUse = "<t color='#9cf953'>Use: </t>Attach";



        class ACE_Triggers {
            SupportedTriggers[] = {"TC_Activate_Tracker"};
            class TC_Activate_Tracker {
                ammo = "TC_GPSTracker_Ammo";
            };
        };
    };
};

class CfgAmmo {
    class PipeBombBase;
    class TC_GPSTracker_Ammo: PipeBombBase {
        ace_explosives_magazine = "TC_GPSTracker_Mag";
        ace_explosives_size = 0;
        ace_explosives_defuseObjectPosition[] = {0.07, 0, 0.055};
        soundActivation[] = {"", 0, 0, 0};
        soundDeactivation[] = {"", 0, 0, 0};
        hit = 0;
        indirectHit = 0;
        indirectHitRange = 0;
        model = "\z\TC\addons\main\data\tk-108.p3d";
        ace_minedetector_detectable = 0;

        mineTrigger = "RemoteTrigger";
        simulation = "shotMine";
        //mineTrigger = ""; //We have no trigger, can't do this, game will crash. Seems like it was supported once, but then forgotten
        mineInconspicuousness = 0; // Chance of mine being not-detected just by looking at it. Higher number means you need to look more directly, closer at the mine to detect it
        defaultMagazine = ""; //Mag that gets dropped after defuse, if empty the mine will stay in place, as deactivated
// _mineMaxRndDetectedDistance detector
//mineTrigger = ""; ?? _mineTrigger


    };
};

class CfgVehicles {
    class ACE_Explosives_Place;
    class ACE_Explosives_Place_TC_GPSTracker: ACE_Explosives_Place {
        displayName = "TK-108 GPS Tracker";
        model = "\z\TC\addons\main\data\tk-108.p3d";
    };
};

class CfgWeapons {
    class Default;
    class Put: Default {
        muzzles[] += {QGVAR(muzzle)};
        class PutMuzzle: Default{};
        class GVAR(muzzle): PutMuzzle {
            magazines[] = {"TC_GPSTracker_Mag"};
        };
    };
};
