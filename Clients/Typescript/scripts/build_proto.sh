#!/bin/sh
PROTO_DIR=$(dirname "$0")/../../../Frameworks/Res/Proto3
OUT_DIR=$(dirname "$0")/../src

pbjs -t static -o $OUT_DIR/pkg.pb.js $PROTO_DIR/*.proto
pbts -o $OUT_DIR/pkg.pb.d.ts $OUT_DIR/pkg.pb.js