# LuaCS

A .NET/C# implementation of the [Lua 5.4.8](https://www.lua.org/) virtual machine and runtime, enabling Lua scripting to be embedded in .NET applications.

## Features

- Full Lua 5.4.8 VM execution engine
- Load and execute Lua source files or precompiled bytecode
- Compile Lua source to bytecode via a native compiler bridge (`luac1.dll`)
- Stack-based C API similar to the reference Lua C API
- Standard library support: `print`, `math.random`, `os.clock`, `io.write`, `table.new`
- Extensible module loader system (`FileModuleLoader`, `CompositeModuleLoader`)
- Metatable support for all value types
- Debug hook support (line, count, call, return hooks)
- High-performance internals: struct-based `LuaValue` tagged union, unsafe optimizations, aggressive inlining

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- Windows (required for `luac1.dll` native compiler bridge)
- CMake (optional, to rebuild the native `luac1.dll` from source)

## Project Structure

```
luacs/
├── Lua/                        # Main C# library project
│   ├── Compiler/               # Binary bytecode load/dump (ProtoIO)
│   ├── Runtime/                # VM execution engine (LuaVM, ExecutionContext, Proto, OpCode)
│   ├── CodeAnalysis/           # Source code analysis utilities
│   ├── Internal/               # Internal helpers (collections, binary I/O, hashing)
│   ├── Loaders/                # Module loaders (FileModuleLoader, CompositeModuleLoader)
│   ├── Std/                    # Standard library (print, math, os, io, table)
│   ├── LuaGlobalState.cs       # Top-level state: environment, registry, loaded modules
│   ├── LuaState.cs             # Per-thread state: value stack, call stack, C API
│   ├── LuaValue.cs             # Tagged union struct for all Lua values
│   ├── LuaTable.cs             # Lua table implementation (array + hash)
│   ├── ExternalCompiler.cs     # P/Invoke bridge to native luac1.dll
│   └── Lua.csproj
├── lua-5.4.8/                  # Native Lua C source and CMake build
└── luacs.sln                   # Visual Studio solution
```

## Building

### Build the .NET library

```bash
dotnet build Lua/Lua.csproj
```

### Build the native compiler (optional)

The repository includes a prebuilt `lua-5.4.8/build/Debug/luac1.dll`. To rebuild it from source using CMake:

```bash
cd lua-5.4.8
cmake -B build
cmake --build build
```

## Usage

### Running a Lua source file

```csharp
using Lua;

var state = new LuaGlobalState();
state.OpenLibs(); // registers print, math, os, io, table

LuaState L = state.MainThread;
L.LoadSourceFile("script.lua"); // compiles and loads the script
LuaVM.Execute(L, 0, 0);        // call with 0 args, discard results
```

### Running precompiled Lua bytecode

```csharp
using Lua;

var state = new LuaGlobalState();
state.OpenLibs();

LuaState L = state.MainThread;
byte[] bytecode = File.ReadAllBytes("script.luac");
L.LoadBinary(bytecode, "script");
LuaVM.Execute(L, 0, 0);
```

### Compiling Lua source to bytecode

```csharp
using Lua;

byte[] bytecode = ExternalCompiler.Compile(@"
    print('Hello from Lua!')
    local x = 10 + 20
    print(x)
");
```

### Calling a C# function from Lua

Register a C# delegate as a Lua global:

```csharp
using Lua;

var state = new LuaGlobalState();
state.OpenLibs();

// Register a custom function
state.Environment["greet"] = new LuaCFunction(L =>
{
    string name = L.GetValue(1).ToString();
    Console.WriteLine($"Hello, {name}!");
    return 0; // number of return values pushed
});

LuaState L = state.MainThread;
L.LoadSourceFile("script.lua"); // script can call greet("World")
LuaVM.Execute(L, 0, 0);
```

### Stack API

`LuaState` exposes a stack-based API similar to the Lua C API:

```csharp
L.PushValue(new LuaValue(42));          // push integer
L.PushValue(new LuaValue(3.14));        // push float
L.PushValue(new LuaValue("hello"));     // push string
L.PushValue(new LuaValue(true));        // push boolean
L.PushValue(LuaValue.Nil);             // push nil
L.PushValue(new LuaValue(new LuaTable())); // push table

int top = L.GetTop();                   // number of values on the stack
LuaValue v = L.GetValue(1);            // get value at index 1 (1-based)
L.Pop(1);                               // pop one value
```

### Module loader

By default, `LuaGlobalState` uses `FileModuleLoader` to load `.lua` files by name. You can replace it with a custom loader:

```csharp
state.ModuleLoader = new CompositeModuleLoader(
    new FileModuleLoader(),
    myCustomLoader
);
```

## Standard Library

The following standard library functions are registered by `state.OpenLibs()`:

| Name | Description |
|------|-------------|
| `print(...)` | Print values to stdout, separated by spaces |
| `io.write(...)` | Write values to stdout without a newline |
| `math.random([m[, n]])` | Generate random numbers |
| `os.clock()` | Return process runtime in seconds |
| `table.new([asize[, dsize]])` | Create a table with pre-allocated capacity |

## Value Types

`LuaValue` is a struct that holds one of the following Lua value types:

| `LuaValueType` | Description |
|----------------|-------------|
| `Nil` | The nil value |
| `Boolean` | `true` or `false` |
| `Integer` | 64-bit signed integer (`long`) |
| `Float` | 64-bit float (`double`) |
| `String` | Immutable string |
| `Table` | Lua table (`LuaTable`) |
| `Function` | Lua closure or C# delegate |
| `UserData` | Custom .NET object implementing `ILuaUserData` |
| `Thread` | Lua coroutine (not yet fully implemented) |

## License

The Lua virtual machine and standard library are based on [Lua 5.4.8](https://www.lua.org/), which is licensed under the [MIT License](https://www.lua.org/license.html).  
Copyright © 1994–2025 Lua.org, PUC-Rio.
