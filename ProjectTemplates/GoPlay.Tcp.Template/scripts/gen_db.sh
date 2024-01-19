#!/bin/sh
DIR=$(dirname "$0")
cd $DIR/../Common
dotnet ef dbcontext scaffold "Server=192.168.1.103;Port=3307;Database=GoPlayProj;User=root;Password=password;TreatTinyAsBoolean=true;CharacterSet=utf8mb4;" "Pomelo.EntityFrameworkCore.MySql" -o Db/Generated/ -f -n GoPlayProj.Database
sed -i '/public GoPlayProjContext()/,/}/d' Db/Generated/GoPlayProjContext.cs
sed -i '/protected override void OnConfiguring/,/DefaultWithLocalTime);/d' Db/Generated/GoPlayProjContext.cs
cd -
