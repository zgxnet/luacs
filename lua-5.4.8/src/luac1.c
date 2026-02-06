#define BUILDING_LUAC1_DLL
#include "luac1.h"
#include "lua.h"
#include "lauxlib.h"
#include "lundump.h"
#include "lstate.h"

// Structure to hold callback function for writing bytecode
typedef struct {
    void (*func_write)(const char *data, int length, void* user_data);
    void* user_data;
} WriteData;

// Custom writer function for lua_dump
static int custom_writer(lua_State* L, const void* p, size_t size, void* ud) {
    WriteData* wd = (WriteData*)ud;
    if (wd && wd->func_write) {
        wd->func_write((const char*)p, (int)size, wd->user_data);
    }
    return 0; // success
}

//source: the lua source code
//func_write: a function to write the compiled bytecode
//user_data: user-defined data passed to func_write
void compile_lua(const char *source, void (*func_write)(const char *data, int length, void* user_data), void* user_data) {
    lua_State* L = NULL;
    WriteData wd;
    
    // Initialize callback data
    wd.func_write = func_write;
    wd.user_data = user_data;
    
    // Create Lua state
    L = luaL_newstate();
    if (L == NULL) {
        return; // Failed to create state
    }
    
    // Load the source code
    int result = luaL_loadstring(L, source);
    if (result != LUA_OK) {
        lua_close(L);
        return; // Failed to compile
    }
    
    // Get the function from the stack
    if (lua_isfunction(L, -1)) {
        // Dump the bytecode using the custom writer
        lua_lock(L);
        luaU_dump(L, clLvalue(s2v(L->top.p - 1))->p, custom_writer, &wd, 0);
        lua_unlock(L);
    }
    
    // Clean up
    lua_close(L);
}
