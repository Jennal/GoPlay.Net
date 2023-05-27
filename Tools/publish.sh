#!/bin/sh -x
DIR=$(dirname "$0")
rm -f $DIR/packages/*
dotnet clean
dotnet build --configuration Release $DIR/Tools.sln -p:Version="1.0.$(date "+%m%d").$(date "+%H%M")"
dotnet pack --no-build --configuration Release --output $DIR/packages $DIR/Tools.sln -p:Version="1.0.$(date "+%m%d").$(date "+%H%M")"
dotnet tool update --global --add-source $DIR/packages GoPlay.Tools
