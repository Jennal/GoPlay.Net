#!/bin/sh -x
DIR=$(dirname "$0")/..
VERSION="1.0.0"
rm -f $DIR/packages/*
cd $DIR
dotnet clean
dotnet build --configuration Release Server.sln -p:Version="$VERSION"
dotnet pack --no-build --configuration Release --output packages Server.sln -p:Version="$VERSION"
#dotnet nuget push packages/GoPlay.Tools.1.0.0.nupkg --api-key $NUGET_KEY --source https://api.nuget.org/v3/index.json