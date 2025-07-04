LibModPlugSharp
This folder provides build scripts and C# bindings for libopenmpt.
It mirrors a subset of the libopenmpt-sharp NuGet package so the
Cycloside project can build without external dependencies.

The build.sh and build.ps1 scripts download the official OpenMPT
sources and compile the native library using the CMake project in
`openmpt/build`. The resulting libopenmpt binary is placed in the
native/ directory and loaded at runtime by
Module via P/Invoke.

Building
Install cmake, a C compiler, and the .NET 8 SDK.

Run ./build.sh (Linux/macOS) or build.ps1 (Windows) to compile the
native library.

Build the C# project with dotnet build LibModPlugSharp.csproj.