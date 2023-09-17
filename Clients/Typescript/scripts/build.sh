#!/bin/sh
dir=$(dirname "$0")/..
cd $dir
tsc
npm run build