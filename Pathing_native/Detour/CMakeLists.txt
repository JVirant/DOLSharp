project(dol_detour)

file(GLOB INCLUDES Include/*.h)
file(GLOB SOURCES Source/*.cpp)

add_library(dol_detour SHARED ${SOURCES})

set_target_properties(dol_detour PROPERTIES DEBUG_POSTFIX -d)

set(dol_detour_INCLUDE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/Include")

target_include_directories(dol_detour PUBLIC
	"$<BUILD_INTERFACE:${dol_detour_INCLUDE_DIR}>"
)

set_target_properties(dol_detour PROPERTIES
	SOVERSION ${SOVERSION}
	VERSION ${LIB_VERSION}
	COMPILE_PDB_OUTPUT_DIRECTORY .
	COMPILE_PDB_NAME "dol_detour-d"
)

install(TARGETS dol_detour
	RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
	ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR}
	LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
	COMPONENT library
)
