#!/usr/bin/env bash
set -e

# Download OpenMPT sources if not already present
if [ ! -d openmpt ]; then
    git clone --depth 1 https://github.com/OpenMPT/openmpt.git
fi

mkdir -p build
cd build
cmake ../openmpt/build -DCMAKE_BUILD_TYPE=Release -DLIBOPENMPT_STATIC=OFF \
      -DLIBOPENMPT_BUILD_TEST=OFF -DLIBOPENMPT_BUILD_EXAMPLES=OFF
cmake --build . --config Release

# Copy the resulting library to the native folder
cd ..
mkdir -p native
cp build/bin/libopenmpt.* native/ 2>/dev/null || true
