#include "\z\TC\addons\main\script_component.hpp"

#define TRANSFORM_LR_RADIO_TO_EXT(radio) if (radio isEqualType []) then { format["%1 %3", netId radio select 0, radio select 1] } else {radio}
