#!/bin/sh -x
DIR=$(dirname "$0")/..
PROJ="Server.sln"
VERSION="0.2.23"
rm -f $DIR/packages/*
cd $DIR



dotnet clean
dotnet build --configuration Release $PROJ -p:Version="$VERSION"
dotnet pack --no-build --configuration Release --output packages $PROJ -p:Version="$VERSION"
