// COMPONENT should be defined in the script_component.hpp and included BEFORE this hpp

#define MAINPREFIX z
#define PREFIX TC

#include "script_version.hpp"

#define VERSION MAJOR.MINOR.PATCHLVL.BUILD
#define VERSION_AR MAJOR,MINOR,PATCHLVL,BUILD

// MINIMAL required version for the Mod. Components can specify others..
#define REQUIRED_VERSION 1.98

#ifdef COMPONENT_BEAUTIFIED
    #define COMPONENT_NAME QUOTE(TacControl - COMPONENT_BEAUTIFIED)
#else
    #define COMPONENT_NAME QUOTE(TacControl - COMPONENT)
#endif
