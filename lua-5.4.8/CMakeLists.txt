cmake_minimum_required(VERSION 3.15)
project(lua VERSION 5.4.8 LANGUAGES C)

# Options
option(BUILD_SHARED_LIBS "Build shared libraries" OFF)

# Static linking of C runtime on Windows
if(WIN32 AND MSVC)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

# Add compile definitions
add_definitions(-DLUA_COMPAT_5_3)

# Core Lua source files
set(LUA_CORE_SRCS
    src/lapi.c src/lcode.c src/lctype.c src/ldebug.c src/ldo.c src/ldump.c
    src/lfunc.c src/lgc.c src/llex.c src/lmem.c src/lobject.c src/lopcodes.c
    src/lparser.c src/lstate.c src/lstring.c src/ltable.c src/ltm.c src/lundump.c
    src/lvm.c src/lzio.c
)

# Lua library source files
set(LUA_LIB_SRCS
    src/lauxlib.c src/lbaselib.c src/lcorolib.c src/ldblib.c src/liolib.c
    src/lmathlib.c src/loadlib.c src/loslib.c src/lstrlib.c src/ltablib.c
    src/lutf8lib.c src/linit.c
)

# Create Lua library target
add_library(lua ${LUA_CORE_SRCS} ${LUA_LIB_SRCS})
set_target_properties(lua PROPERTIES 
    VERSION ${PROJECT_VERSION}
    SOVERSION ${PROJECT_VERSION_MAJOR}
    PUBLIC_HEADER "src/lua.h;src/luaconf.h;src/lualib.h;src/lauxlib.h;src/lua.hpp"
)

if(WIN32)
    target_link_libraries(lua PRIVATE advapi32)
else()
    target_link_libraries(lua PRIVATE m)
endif()

# Create lua interpreter executable
add_executable(lua_interpreter src/lua.c)
set_target_properties(lua_interpreter PROPERTIES OUTPUT_NAME lua)
target_link_libraries(lua_interpreter PRIVATE lua)

# Create luac compiler executable
add_executable(luac src/luac.c)
target_link_libraries(luac PRIVATE lua)

# Create luac1 shared library (DLL)
add_library(luac1 SHARED src/luac1.c)
target_link_libraries(luac1 PRIVATE lua)
set_target_properties(luac1 PROPERTIES
    VERSION ${PROJECT_VERSION}
    SOVERSION ${PROJECT_VERSION_MAJOR}
)

# On Windows, ensure proper DLL export/import
if(WIN32)
    set_target_properties(luac1 PROPERTIES
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )
endif()

# Optional: Create test executable for luac1 DLL
add_executable(test_luac1 test_luac1.c)
if(WIN32)
    target_link_libraries(test_luac1 PRIVATE kernel32)
endif()

# Installation rules
include(GNUInstallDirs)

install(TARGETS lua lua_interpreter luac luac1
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR}
    PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}
)
