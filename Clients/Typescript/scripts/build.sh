#!/bin/sh
dir=$(dirname "$0")/..
cd $dir
tsc
cp $dir/src/pkg.pb.js $dir/dist
npm run build