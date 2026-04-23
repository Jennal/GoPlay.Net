#!/bin/sh -x
set -e

DIR=$(dirname "$0")/..
UNITY_DIR="$DIR/../../GoPlay.Unity/Assets/Frameworks/Plugins/GoPlay.Net"
TFM="netstandard2.1"

PROJECTS="
Client/Client.csproj
Core/Core.csproj
Common.Data/Common.Data.csproj
Transport.NetCoreServer/Transport.NetCoreServer.csproj
Transport.Ws/Transport.Ws.csproj
Transport.Wss/Transport.Wss.csproj
ThirdParty/NetCoreServer/source/NetCoreServer/NetCoreServer.csproj
"

OUTPUT_DIRS="
Client/bin/Release/$TFM
Core/bin/Release/$TFM
Common.Data/bin/Release/$TFM
Transport.NetCoreServer/bin/Release/$TFM
Transport.Ws/bin/Release/$TFM
Transport.Wss/bin/Release/$TFM
ThirdParty/NetCoreServer/source/NetCoreServer/bin/Release/$TFM
"

cd "$DIR"
mkdir -p "$UNITY_DIR"

for proj in $PROJECTS; do
    dotnet build --configuration Release --framework "$TFM" "$proj"
done

rm -f "$UNITY_DIR"/GoPlay*.dll
rm -f "$UNITY_DIR"/GoPlay*.pdb

for out_dir in $OUTPUT_DIRS; do
    for f in "$out_dir"/GoPlay*.dll "$out_dir"/GoPlay*.pdb "$out_dir"/GoPlay*.xml "$out_dir"/client.pfx "$out_dir"/server.pfx; do
        [ -e "$f" ] || continue
        case "$(basename "$f")" in
            GoPlay.NetCoreServer.xml|client.pfx|server.pfx)
                continue
                ;;
        esac
        cp -f "$f" "$UNITY_DIR"/
    done
done
