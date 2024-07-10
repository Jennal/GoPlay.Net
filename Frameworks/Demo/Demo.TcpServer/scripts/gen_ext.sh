#!/bin/sh
DIR=$(dirname "$0")/..
OUT_DIR=$DIR/../../../../GoPlay.Unity/Assets/Demo/Scripts/GoPlayDemo/Generated

goplay extension -i $DIR/../Demo.Common -b ProcessorBase,GoPlayProjProcessor \
    -of $OUT_DIR/ClientExtensions.cs -tf $DIR/scripts/liquids/client.liquid \
    -nf GoPlay.Core.Protocols,GoPlay.Demo

# goplay extension -i $DIR -ob $DIR/admin/src/network/generated/client.extension.ts -b ProcessorBase,GoPlayProjProcessor -tb $DIR/scripts/liquids/server.ts.liquid \
#     -nb GoPlay.Core.Protocols,GoPlayProj.Protocols,GoPlayProj.Protocols.Admin -igt EchoProcessor -igm AdminProcessor.Login,AdminProcessor.LoginResult
