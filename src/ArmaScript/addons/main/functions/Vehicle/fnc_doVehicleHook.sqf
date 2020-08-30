params ["_vehicle", "_hook"];


if (_hook) then {
    _vehicle action ["HookCargo", _vehicle];
    //#TODO param, cargo object
/*

	class CfgSlingLoading
	{
		ropeLength = 10;
		hookMinRange = 4;
		abortHeight = 20;
		abortRange = 15;
		unwindSpeed = 2;
		slmMaxAltitude = 40;
	};

        const Vector3 slpos = GetSlingLoadingPosition();

        //! We want to look only in max default range around position where should be cargo under heli
        auto *cargo = NearestObject(
          GetSlingLoadingPosition(),
           RopeObject::SlingLoadingAbortRange(), //Game config see above
          &StaticIsSlingLoadCargo, this, true));
        if(cargo && GetPosition().Y() - cargo->GetPosition().Y() < RopeObject::SlingLoadingRopeLength() - 1) //< Are we down enough to pick in up?

 */


} else {
    _vehicle action ["UnhookCargo", _vehicle];
};
_vehicle call TC_main_fnc_Vehicle_updateAnimSources;
