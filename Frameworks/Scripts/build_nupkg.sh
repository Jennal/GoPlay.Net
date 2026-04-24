#!/bin/sh -x
DIR=$(dirname "$0")/..
PROJ="Server.sln"
TPL_DIR="../ProjectTemplates"
TPL="$TPL_DIR/GoPlay.Templates.csproj"
VERSION="1.0.3"
rm -f $DIR/packages/*
cd $DIR

# 将 ProjectTemplates 下所有子模板 csproj 中的 GoPlay.* PackageReference 版本号统一改为 $VERSION
# 避免模板包里内嵌的版本号与本次一起发布的 GoPlay.* nupkg 脱节。
# 仅改形如：<PackageReference Include="GoPlay.xxx" Version="x.y.z" ... />
find "$TPL_DIR" -type f -name '*.csproj' -print0 \
    | xargs -0 sed -i -E 's#(<PackageReference[[:space:]]+Include="GoPlay\.[^"]+"[[:space:]]+Version=")[^"]*(")#\1'"$VERSION"'\2#g'

dotnet clean
dotnet build --configuration Release $PROJ -p:Version="$VERSION"
dotnet pack --no-build --configuration Release --output packages $PROJ -p:Version="$VERSION"

# GoPlay.Templates 模板包：独立项目（不在 Server.sln 里），单独 pack 到同一 packages/ 目录，
# 以复用 publish_nupkg.sh 的通配推送逻辑（packages/GoPlay.*$VERSION*.nupkg）
dotnet pack --configuration Release --output packages $TPL -p:PackageVersion="$VERSION"
