cmake_minimum_required (VERSION 3.15)
if (${CMAKE_HOST_SYSTEM_NAME} STREQUAL "Windows")
	set (CMAKE_SYSTEM_VERSION 8.1 CACHE TYPE INTERNAL FORCE) #Force 8.1 SDK, to keep it compatible with win7
endif()
cmake_policy(SET CMP0091 NEW)
project (TacControl C CXX)
find_package (Threads)


if(MSVC)
	set( CMAKE_CXX_FLAGS  "${CMAKE_CXX_FLAGS} /std:c++latest" )
elseif(MSVC)
	message(FATAL_ERROR "ERROR: You need a C++20 compatible compiler")
endif()

message("GENERATOR USED: '${CMAKE_GENERATOR}'")
message("COMPILER USED: '${CMAKE_CXX_COMPILER_ID}'")

if(USE_64BIT_BUILD)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/release/@TacControl")
else()
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/release/@TacControl")
endif()

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

SET(CMAKE_INCLUDE_CURRENT_DIR ON) 
set_property(GLOBAL PROPERTY USE_FOLDERS ON)

#set(CMAKE_SUPPRESS_REGENERATION true)
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)

set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

add_subdirectory(lib/boost-cmake EXCLUDE_FROM_ALL)
set(FMT_OS OFF) # Don't want windows.h include splattered everywhere
add_subdirectory(lib/fmt EXCLUDE_FROM_ALL)


set(ARMA_PBO_LIB_WRITE OFF)
add_subdirectory(lib/ArmaPboLib EXCLUDE_FROM_ALL)
add_subdirectory(lib/MipMapTool EXCLUDE_FROM_ALL)





add_subdirectory(src)
