cmake_minimum_required (VERSION 3.13)


set(PLUGIN_NAME "TacControl_x64")

add_definitions(/DNOMINMAX)
add_definitions(/DINTERCEPT_SQF_STRTYPE_RSTRING)
add_definitions(/D_SILENCE_ALL_CXX17_DEPRECATION_WARNINGS) #No I don't cheat!

file(GLOB_RECURSE TacControlSources *.hpp *.cpp *.asm *.s)
SOURCE_GROUP("src" FILES ${TacControlSources})

file(GLOB TCSources_Networking "Networking/*.cpp" "Networking/*.hpp")
SOURCE_GROUP("src/Networking" FILES ${TCSources_Networking})



#file(GLOB FMT_SOURCES "${PROJECT_SOURCE_DIR}/lib/fmt/src/*.cc")
#SOURCE_GROUP("intercept" FILES ${INTERCEPT_SOURCES})

#if(CMAKE_COMPILER_IS_GNUCXX)
#	list(REMOVE_ITEM TacControlSources "${CMAKE_SOURCE_DIR}/src/hooks.asm")
#else()
#	list(REMOVE_ITEM TacControlSources "${CMAKE_SOURCE_DIR}/src/hooks.s")
#endif()

set(library_sources ${TacControlSources})

add_library(${PLUGIN_NAME} SHARED
    ${library_sources}
    "${PROJECT_SOURCE_DIR}/lib/cpp-base64/base64.cpp"
    "${PROJECT_SOURCE_DIR}/lib/s3tc-dxt-decompression/s3tc.cpp"
    "${PROJECT_SOURCE_DIR}/lib/minilzo/minilzo.c"
)

target_include_directories(${PLUGIN_NAME} PRIVATE
    "${PROJECT_SOURCE_DIR}/lib/json/single_include/"
    "${PROJECT_SOURCE_DIR}/lib/ArmaPboLib/src/"
    "${PROJECT_SOURCE_DIR}/lib/MipMapTool/"
    "${PROJECT_SOURCE_DIR}/lib/cpp-base64/"
    "${PROJECT_SOURCE_DIR}/lib/s3tc-dxt-decompression/"
    "${PROJECT_SOURCE_DIR}/lib/minilzo/"
)

target_link_libraries(${PLUGIN_NAME} ${CMAKE_THREAD_LIBS_INIT})
target_link_libraries(${PLUGIN_NAME} PUBLIC Boost::boost)
target_link_libraries(${PLUGIN_NAME} PUBLIC fmt::fmt-header-only)
target_link_libraries(${PLUGIN_NAME} PUBLIC ArmaPboLib)
target_link_libraries(${PLUGIN_NAME} PUBLIC MipMapToolLib)

#target_link_libraries(${PLUGIN_NAME} ${LINK_LIBS})

set_target_properties(${PLUGIN_NAME} PROPERTIES PREFIX "")
set_target_properties(${PLUGIN_NAME} PROPERTIES FOLDER TacControl)

if(CMAKE_COMPILER_IS_GNUCXX)
	#set_source_files_properties(hooks.s PROPERTIES COMPILE_FLAGS "-x assembler-with-cpp")
	add_compile_definitions(__linux__)
	#list(REMOVE_ITEM TacControlSources "${CMAKE_SOURCE_DIR}/src/hooks.asm")

	target_compile_options(
		${PLUGIN_NAME} PRIVATE 
		"-std=c++1z"
		"$<$<CONFIG:Release>:-O3>"
		"-s"
		"-fPIC"
		"-fpermissive"
		"-static-libgcc"
		"-static-libstdc++"
		"-march=i686"
		"-m32"
		"-Wno-ignored-attributes"
		"-static"
		"$<$<CONFIG:Release>:-ffunction-sections>"
		"$<$<CONFIG:Release>:-fdata-sections>"

	)
	target_link_options(${PLUGIN_NAME} PRIVATE "-m32" "-fPIC" "-static" "-static-libgcc" "-static-libstdc++"
	"$<$<CONFIG:Release>:-Wl,--gc-sections>"
	"$<$<CONFIG:Release>:-Wl,--strip-all>"
	) 
	set(CMAKE_FIND_LIBRARY_SUFFIXES ".a")

	target_link_libraries(${PLUGIN_NAME} "libstdc++.a")

else()
	target_compile_options(
		${PLUGIN_NAME} PRIVATE 
		"/MP" "/Zi" "/bigobj"
		"$<$<CONFIG:Release>:/MT>"
		"$<$<CONFIG:Release>:/Ox>"
		"$<$<CONFIG:Release>:/Ob2>"
		"$<$<CONFIG:Release>:/Oi>"
		"$<$<CONFIG:Release>:/Ot>"
        "/Zc:__cplusplus"
	)
	target_link_options(${PLUGIN_NAME} PRIVATE "/OPT:REF" "/OPT:ICF" "/DEBUG:FULL") 
endif()


if(CMAKE_COMPILER_IS_GNUCXX)
	set_target_properties(${INTERCEPT_NETWORK_NAME} PROPERTIES LINK_SEARCH_START_STATIC 1)
	set_target_properties(${INTERCEPT_NETWORK_NAME} PROPERTIES LINK_SEARCH_END_STATIC 1)
endif()

#Binary signing
#if(EXISTS "${CMAKE_RUNTIME_OUTPUT_DIRECTORY}/sign.bat" AND MSVC)
#	add_custom_command(TARGET ${PLUGIN_NAME}
#		POST_BUILD
#		COMMAND ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}/sign.bat
#		WORKING_DIRECTORY ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}
#	)
#endif()
