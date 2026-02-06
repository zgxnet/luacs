#include <stdio.h>
#include <windows.h>

// Function pointer type for compile_lua
typedef void (*CompileLuaFunc)(const char *source, void (*func_write)(const char *data, int length, void* user_data), void* user_data);

// Callback function to handle compiled bytecode
void bytecode_writer(const char *data, int length, void* user_data) {
    printf("Received %d bytes of bytecode\n", length);
    // Here you could save to file or process the bytecode
}

int main() {
    // Load the DLL (try current directory first, then full path)
    HMODULE hDll = LoadLibraryA("luac1.dll");
    if (hDll == NULL) {
        hDll = LoadLibraryA(".\\luac1.dll");
    }
    if (hDll == NULL) {
        printf("Failed to load luac1.dll\n");
        return 1;
    }

    // Get the function pointer
    CompileLuaFunc compile_lua = (CompileLuaFunc)GetProcAddress(hDll, "compile_lua");
    if (compile_lua == NULL) {
        printf("Failed to get compile_lua function\n");
        FreeLibrary(hDll);
        return 1;
    }

    // Test Lua source code
    const char* lua_source = "print('Hello from Lua!')";
    
    printf("Compiling Lua source: %s\n", lua_source);
    
    // Call the compile function
    compile_lua(lua_source, bytecode_writer, NULL);
    
    printf("Compilation completed\n");

    // Clean up
    FreeLibrary(hDll);
    return 0;
}
