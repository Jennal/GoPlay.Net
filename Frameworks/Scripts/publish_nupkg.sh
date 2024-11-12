#!/bin/sh -x
DIR=$(dirname "$0")/..
VERSION="0.2.23"
cd $DIR

# Push nupkg
for f in `ls packages/GoPlay.*$VERSION*.nupkg`; do
    dotnet nuget push $f --skip-duplicate --api-key $NUGET_KEY --source https://api.nuget.org/v3/index.json    
done

# Push snupkg
for f in `ls packages/GoPlay.*$VERSION*.snupkg`; do
    dotnet nuget push $f --skip-duplicate --api-key $NUGET_KEY --source https://api.nuget.org/v3/index.json    
done
