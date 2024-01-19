#!/bin/sh
DIR=$(dirname "$0")/../../../proto
PROTOC=protoc
$PROTOC -I=$DIR --csharp_opt=file_extension=.g.cs --csharp_out=$DIR/../backend/Codes/Common/Protocols/Generated $DIR/*.proto
$PROTOC -I=$DIR -I=$DIR/.. --csharp_opt=file_extension=.g.cs --csharp_out=$DIR/../backend/Codes/Common/Protocols/Generated $DIR/Admin/*.proto