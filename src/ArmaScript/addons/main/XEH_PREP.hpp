

PREP(sendMessage);
PREP_SUB(Radio,preInit); //TC_main_fnc_Radio_preInit
PREP_SUB(Radio,onMessage); //TC_main_fnc_Radio_onMessage
PREP_SUB(Radio,TFARTransmit); //TC_main_fnc_Radio_TFARTransmit



PREP_SUB(GPS,preInit); //TC_main_fnc_GPS_preInit
PREP_SUB(GPS,onMessage); //TC_main_fnc_GPS_onMessage
PREP_SUB(GPS,activateTracker); //TC_main_fnc_GPS_activateTracker
PREP_SUB(GPS,sendTrackerUpdate); //TC_main_fnc_GPS_sendTrackerUpdate
