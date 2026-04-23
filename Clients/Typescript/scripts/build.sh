#!/bin/sh
# 编译 TS -> dist/，把 protobuf 生成的 pkg.pb.js / pkg.pb.d.ts 拷过去
# （tsc 不处理 .js，也不会把 .d.ts 再 emit 一遍）。
set -eu

DIR=$(cd "$(dirname "$0")/.." && pwd)
cd "$DIR"

npm run build
