#!/bin/sh
dir=$(dirname $0)
in_dir=$dir/../../../Frameworks/Res/Proto3
out_dir=$dir/..
pbjs --no-beautify --no-comments -t static -w commonjs -o $out_dir/pkg.pb.js $in_dir/*.proto

pbjs -t static-module -w commonjs -o $out_dir/../Typescript/src/pkg.pb.js $in_dir/*.proto
pbts -o $out_dir/../Typescript/src/pkg.d.ts $out_dir/../Typescript/src/pkg.pb.js