#ifndef LUAC1_H
#define LUAC1_H

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
    #ifdef BUILDING_LUAC1_DLL
        #define LUAC1_API __declspec(dllexport)
    #else
        #define LUAC1_API __declspec(dllimport)
    #endif
#else
    #define LUAC1_API
#endif

/**
 * Compile Lua source code to bytecode
 * @param source The Lua source code to compile
 * @param func_write Callback function to receive the compiled bytecode
 *                   Called with data pointer, length, and user data for each chunk
 * @param user_data User-defined data passed to func_write callback
 */
LUAC1_API void compile_lua(const char *source, void (*func_write)(const char *data, int length, void* user_data), void* user_data);

#ifdef __cplusplus
}
#endif

#endif /* LUAC1_H */
