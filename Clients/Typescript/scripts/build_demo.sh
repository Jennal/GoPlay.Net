#!/bin/sh
DIR=$(cd "$(dirname "$0")/.." && pwd)
cd "$DIR"

npm run build
npm run build:bundle