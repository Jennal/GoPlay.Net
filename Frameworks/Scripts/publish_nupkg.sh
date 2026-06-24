#!/bin/sh -x
DIR=$(dirname "$0")/..
TPL_DIR="../ProjectTemplates"
VERSION="1.0.6"
cd $DIR

sync_template_package_versions() {
    # 发布前也同步模板源码里的 GoPlay.* 引用版本，避免只运行 publish 脚本时版本引用滞后。
    find "$TPL_DIR" -type f -name '*.csproj' -print0 \
        | xargs -0 sed -i -E 's#(<PackageReference[[:space:]]+Include="GoPlay\.[^"]+"[[:space:]]+Version=")[^"]*(")#\1'"$VERSION"'\2#g'
}

push_packages() {
    for f in $1; do
        [ -e "$f" ] || continue
        dotnet nuget push "$f" --skip-duplicate --api-key $NUGET_KEY --source https://api.nuget.org/v3/index.json
    done
}

sync_template_package_versions

if [ ! -f "packages/GoPlay.Templates.$VERSION.nupkg" ]; then
    echo "Missing packages/GoPlay.Templates.$VERSION.nupkg. Run Frameworks/Scripts/build_nupkg.sh first."
    exit 1
fi

# Push nupkg
push_packages "packages/GoPlay.*$VERSION*.nupkg"

# Push snupkg
push_packages "packages/GoPlay.*$VERSION*.snupkg"
