project(Viewer)

file(GLOB INCLUDES Include/*.hpp)
file(GLOB SOURCES Source/*.cpp)

include_directories(../extern/glm)

set(ALL_LIBS
	glfw
	dol_detour
)

add_definitions(
	-DTW_STATIC
	-DTW_NO_LIB_PRAGMA
	-DTW_NO_DIRECT3D
	-D_CRT_SECURE_NO_WARNINGS
)

add_executable(Viewer ${SOURCES} "Include/gles2.h")
target_link_libraries(Viewer ${ALL_LIBS})

set_target_properties(Viewer PROPERTIES DEBUG_POSTFIX -d)

set(Viewer_INCLUDE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/Include")

target_include_directories(Viewer PUBLIC
	"$<BUILD_INTERFACE:${Viewer_INCLUDE_DIR}>"
)

set_target_properties(Viewer PROPERTIES
	SOVERSION ${SOVERSION}
	VERSION ${LIB_VERSION}
	COMPILE_PDB_OUTPUT_DIRECTORY .
	COMPILE_PDB_NAME "Viewer-d"
)

install(TARGETS Viewer
	RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
	ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR}
	LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
)
