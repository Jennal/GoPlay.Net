#!/bin/sh -x
DIR=$(dirname "$0")/..
VERSION="0.2.0"
rm -f $DIR/packages/*
cd $DIR
dotnet clean
dotnet build --configuration Release Server.sln -p:Version="$VERSION"
dotnet pack --no-build --configuration Release --output packages Server.sln -p:Version="$VERSION"
