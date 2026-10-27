#!/usr/bin/env bash
# Move into the app project folder, wherever the script is run from
cd "$(dirname "$0")/src/DiaryApp"
# Build in Debug, install on the USB-connected device and launch it
dotnet build -t:Run -f net10.0-android -p:AdbTarget=-d -p:AndroidSdkDirectory="$ANDROID_HOME"
