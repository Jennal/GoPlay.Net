#!/bin/sh
BASE_DIR=$(dirname "$0")/..
DST_DIR=$1

# echo $BASE_DIR
# echo $DST_DIR

$BASE_DIR/Scripts/build_proto.sh

cd $BASE_DIR/Client
dotnet build -c Release

cd ..
mkdir -p $DST_DIR
## declare an array variable
declare -a arr=(
    "Google.Protobuf.dll" 
    "System.Buffers.dll" 
    "System.Memory.dll" 
    "System.Runtime.CompilerServices.Unsafe.dll" 
    "GoPlay.Service.Client.dll" 
    "GoPlay.Service.Core.dll"
)

## now loop through the above array
for i in "${arr[@]}"
do
   file=$BASE_DIR/Client/bin/Release/net471/$i
   cp -f $file $DST_DIR/
done
