#!/bin/sh
BASEDIR=$(dirname "$0")/..
protoc -I=$BASEDIR/Res/Proto3 --csharp_opt=file_extension=.g.cs --csharp_out=$BASEDIR/Core/Protocols/Generated $BASEDIR/Res/Proto3/*.proto
#protoc -I=Res/Proto3 --csharp_opt=file_extension=.g.cs --csharp_out=Core/Protocols/Generated Res/Proto3/*.proto