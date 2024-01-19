#!/bin/sh
DIR=$(dirname "$0")/..
# TODO:
goplay extension -i $DIR -ob $DIR/Client.Extension/ClientExtensions.be.cs -b ProcessorBase,GoPlayProjProcessor -tb $DIR/scripts/liquids/server.liquid \
    -nb GoPlay.Core.Protocols \
    -of $DIR/Client.Extension/ClientExtensions.fe.cs -tf $DIR/scripts/liquids/server.liquid \
    -nf GoPlay.Core.Protocols

# goplay extension -i $DIR -ob $DIR/admin/src/network/generated/client.extension.ts -b ProcessorBase,GoPlayProjProcessor -tb $DIR/scripts/liquids/server.ts.liquid \
#     -nb GoPlay.Core.Protocols,GoPlayProj.Protocols,GoPlayProj.Protocols.Admin -igt EchoProcessor -igm AdminProcessor.Login,AdminProcessor.LoginResult
