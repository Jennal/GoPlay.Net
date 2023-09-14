#!/bin/sh
name=$1
openssl genrsa -out $name.key 2048
openssl req -new -key $name.key -out $name.csr
openssl x509 -req -days 36500 -in $name.csr -signkey $name.key -out $name.crt
openssl pkcs12 -export -out $name.pfx -inkey $name.key -in $name.crt