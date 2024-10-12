#!/bin/sh
DIR=$(dirname "$0")/../Demo/Demo.WsServer
cd $DIR

dotnet build -c debug -f net7.0 -r linux-x64 --self-contained
cd bin/Debug/net7.0/linux-x64

docker run -it --rm -p 8888:8888 -v $(pwd):/app mcr.microsoft.com/dotnet/sdk:7.0 bash -c "cd /app && ./Demo.WsServer start -p 8888"