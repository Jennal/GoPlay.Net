#!/bin/sh -x
DIR=$(dirname "$0")
VERSION="1.0.3"
rm -f $DIR/packages/*
dotnet clean
dotnet build --configuration Release $DIR/Tools.sln -p:Version="$VERSION"
dotnet pack --no-build --configuration Release --output $DIR/packages $DIR/Tools.sln -p:Version="$VERSION"
dotnet nuget push packages/GoPlay.Tools.$VERSION.nupkg --api-key $NUGET_KEY --source https://api.nuget.org/v3/index.json