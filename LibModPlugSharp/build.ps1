Param()

$ErrorActionPreference = 'Stop'

if (-not (Test-Path openmpt)) {
    git clone --depth 1 https://github.com/OpenMPT/openmpt.git
}

New-Item -ItemType Directory -Force -Path build | Out-Null
Push-Location build
cmake ../openmpt/build -DCMAKE_BUILD_TYPE=Release -DLIBOPENMPT_STATIC=OFF -DLIBOPENMPT_BUILD_TEST=OFF -DLIBOPENMPT_BUILD_EXAMPLES=OFF
cmake --build . --config Release
Pop-Location

New-Item -ItemType Directory -Force -Path native | Out-Null
Get-ChildItem build/bin/libopenmpt.* | Copy-Item -Destination native -Force
