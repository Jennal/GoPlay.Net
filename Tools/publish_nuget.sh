#!/bin/sh -x
DIR=$(dirname "$0")
VERSION="1.0.0"
rm -f $DIR/packages/*
dotnet clean
dotnet build --configuration Release $DIR/Tools.sln -p:Version="$VERSION"
dotnet pack --no-build --configuration Release --output $DIR/packages $DIR/Tools.sln -p:Version="$VERSION"
