

PREP(sendMessage);
PREP_SUB(Radio,preInit); //TC_main_fnc_Radio_preInit
PREP_SUB(Radio,onMessage); //TC_main_fnc_Radio_onMessage
PREP_SUB(Radio,TFARTransmit); //TC_main_fnc_Radio_TFARTransmit
PREP_SUB(Radio,doUpdateRadio); //TC_main_fnc_Radio_doUpdateRadio
PREP_SUB(Radio,checkDeletedRadios); //TC_main_fnc_Radio_checkDeletedRadios
PREP_SUB(Radio,initTFAREvents); //TC_main_fnc_Radio_initTFAREvents
PREP_SUB(Radio,getLRRadioByNetId); //TC_main_fnc_Radio_getLRRadioByNetId



PREP_SUB(GPS,preInit); //TC_main_fnc_GPS_preInit
PREP_SUB(GPS,onMessage); //TC_main_fnc_GPS_onMessage
PREP_SUB(GPS,activateTracker); //TC_main_fnc_GPS_activateTracker
PREP_SUB(GPS,sendTrackerUpdate); //TC_main_fnc_GPS_sendTrackerUpdate
PREP_SUB(GPS,onServerTracker); //TC_main_fnc_GPS_onServerTracker


PREP_SUB(Marker,preInit); //TC_main_fnc_Marker_preInit
PREP_SUB(Marker,onMessage); //TC_main_fnc_Marker_onMessage
PREP_SUB(Marker,sendMarkerUpdate); //TC_main_fnc_Marker_sendMarkerUpdate
PREP_SUB(Marker,onMarkerCreated); //TC_main_fnc_Marker_onMarkerCreated
PREP_SUB(Marker,onMarkerDeleted); //TC_main_fnc_Marker_onMarkerDeleted
PREP_SUB(Marker,onMarkerPosChanged); //TC_main_fnc_Marker_onMarkerPosChanged
PREP_SUB(Marker,assembleMarkerInfo); //TC_main_fnc_Marker_assembleMarkerInfo
