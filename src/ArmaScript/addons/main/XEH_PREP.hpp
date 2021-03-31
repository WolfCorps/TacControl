

PREP(sendMessage); //TC_main_fnc_sendMessage
PREP(exportTerrain); //TC_main_fnc_exportTerrain
PREP_SUB(Radio,preInit); //TC_main_fnc_Radio_preInit
PREP_SUB(Radio,onMessage); //TC_main_fnc_Radio_onMessage
PREP_SUB(Radio,TFARTransmit); //TC_main_fnc_Radio_TFARTransmit
PREP_SUB(Radio,doUpdateRadio); //TC_main_fnc_Radio_doUpdateRadio
PREP_SUB(Radio,checkDeletedRadios); //TC_main_fnc_Radio_checkDeletedRadios
PREP_SUB(Radio,initTFAREvents); //TC_main_fnc_Radio_initTFAREvents
PREP_SUB(Radio,getLRRadioByNetId); //TC_main_fnc_Radio_getLRRadioByNetId



PREP_SUB(GPS,preInit); //TC_main_fnc_GPS_preInit
PREP_SUB(GPS,postInit); //TC_main_fnc_GPS_postInit
PREP_SUB(GPS,onMessage); //TC_main_fnc_GPS_onMessage
PREP_SUB(GPS,activateTracker); //TC_main_fnc_GPS_activateTracker
PREP_SUB(GPS,sendTrackerUpdate); //TC_main_fnc_GPS_sendTrackerUpdate
PREP_SUB(GPS,onServerTracker); //TC_main_fnc_GPS_onServerTracker
PREP_SUB(GPS,renderPickUpInteraction); //TC_main_fnc_GPS_renderPickUpInteraction
PREP_SUB(GPS,pickUpTracker); //TC_main_fnc_GPS_pickUpTracker
PREP_SUB(GPS,getTrackerByNetId); //TC_main_fnc_GPS_getTrackerByNetId


PREP_SUB(Marker,preStart); //TC_main_fnc_Marker_preStart
PREP_SUB(Marker,preInit); //TC_main_fnc_Marker_preInit
PREP_SUB(Marker,postInit); //TC_main_fnc_Marker_postInit
PREP_SUB(Marker,onMessage); //TC_main_fnc_Marker_onMessage
PREP_SUB(Marker,sendMarkerUpdate); //TC_main_fnc_Marker_sendMarkerUpdate
PREP_SUB(Marker,onMarkerCreated); //TC_main_fnc_Marker_onMarkerCreated
PREP_SUB(Marker,onMarkerDeleted); //TC_main_fnc_Marker_onMarkerDeleted
PREP_SUB(Marker,onMarkerPosChanged); //TC_main_fnc_Marker_onMarkerPosChanged
PREP_SUB(Marker,assembleMarkerInfo); //TC_main_fnc_Marker_assembleMarkerInfo
PREP_SUB(Marker,doCreateMarker); //TC_main_fnc_Marker_doCreateMarker



PREP_SUB(Vehicle,preInit); //TC_main_fnc_Vehicle_preInit
PREP_SUB(Vehicle,postInit); //TC_main_fnc_Vehicle_postInit
PREP_SUB(Vehicle,onMessage); //TC_main_fnc_Vehicle_onMessage
PREP_SUB(Vehicle,onVehicleChanged); //TC_main_fnc_Vehicle_onVehicleChanged
PREP_SUB(Vehicle,updatePassengers); //TC_main_fnc_Vehicle_updatePassengers
PREP_SUB(Vehicle,updateAnimSources); //TC_main_fnc_Vehicle_updateAnimSources
PREP_SUB(Vehicle,onEngineChanged); //TC_main_fnc_Vehicle_onEngineChanged
PREP_SUB(Vehicle,onSlingloadChanged); //TC_main_fnc_Vehicle_onSlingloadChanged
PREP_SUB(Vehicle,doVehicleActiveSensors); //TC_main_fnc_Vehicle_doVehicleActiveSensors
PREP_SUB(Vehicle,doVehicleAutoHover); //TC_main_fnc_Vehicle_doVehicleAutoHover
PREP_SUB(Vehicle,doVehicleCollisionLight); //TC_main_fnc_Vehicle_doVehicleCollisionLight
PREP_SUB(Vehicle,doVehicleEngine); //TC_main_fnc_Vehicle_doVehicleEngine
PREP_SUB(Vehicle,doVehicleGear); //TC_main_fnc_Vehicle_doVehicleGear
PREP_SUB(Vehicle,doVehicleHook); //TC_main_fnc_Vehicle_doVehicleHook
PREP_SUB(Vehicle,doVehicleLight); //TC_main_fnc_Vehicle_doVehicleLight
PREP_SUB(Vehicle,doVehicleWheelsBrake); //TC_main_fnc_Vehicle_doVehicleWheelsBrake



PREP_SUB(ACE,preInit); //TC_main_fnc_ACE_preInit
PREP_SUB(ACE,onMessage); //TC_main_fnc_ACE_onMessage
PREP_SUB(ACE,updateAvailableExplosives); //TC_main_fnc_ACE_updateAvailableExplosives
