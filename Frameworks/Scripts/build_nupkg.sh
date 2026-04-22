#!/bin/sh -x
DIR=$(dirname "$0")/..
PROJ="Server.sln"
TPL="../ProjectTemplates/GoPlay.Templates.csproj"
VERSION="0.9.1"
rm -f $DIR/packages/*
cd $DIR



dotnet clean
dotnet build --configuration Release $PROJ -p:Version="$VERSION"
dotnet pack --no-build --configuration Release --output packages $PROJ -p:Version="$VERSION"

# GoPlay.Templates 模板包：独立项目（不在 Server.sln 里），单独 pack 到同一 packages/ 目录，
# 以复用 publish_nupkg.sh 的通配推送逻辑（packages/GoPlay.*$VERSION*.nupkg）
dotnet pack --configuration Release --output packages $TPL -p:PackageVersion="$VERSION"
