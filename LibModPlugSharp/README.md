# LibModPlugSharp

This folder contains build scripts and a simple C# wrapper for compiling
[libopenmpt](https://lib.openmpt.org/) as a native library that can be
consumed from .NET applications.

The build steps fetch the official OpenMPT sources and produce
`libopenmpt` for the current platform. The resulting library can then be
accessed via P/Invoke bindings in `LibModPlugSharp.cs`.

## Building

1. Ensure that `cmake`, a C compiler and the .NET 8 SDK are installed.
2. Run `./build.sh` on Linux/macOS or `build.ps1` on Windows to
   download and compile the native library.
3. Build the C# project with `dotnet build LibModPlugSharp.csproj`.

The build scripts will place the compiled native library inside the
`native/` folder so it can be loaded at runtime.
