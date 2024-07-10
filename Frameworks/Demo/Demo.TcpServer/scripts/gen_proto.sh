#!/bin/sh
DIR=$(dirname "$0")/../proto
DIR_CLIENT=$(dirname "$0")/../../../../../GoPlay.Unity/Assets/Demo/Scripts/GoPlayDemo
PROTOC=protoc

mkdir -p $DIR/../Protocols/Generated
mkdir -p $DIR_CLIENT/Protocols/Generated

$PROTOC -I=$DIR --csharp_opt=file_extension=.g.cs --csharp_out=$DIR/../../Demo.Common/Protocols/Generated $DIR/*.proto
$PROTOC -I=$DIR --csharp_opt=file_extension=.g.cs --csharp_out=$DIR_CLIENT/Protocols/Generated $DIR/*.proto