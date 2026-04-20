#!/usr/bin/env bash
set -u
PROJ="Frameworks/UnitTest/UnitTest.csproj"
FIXTURES=(
  TestPackage
  TestExpression
  TestProcessor
  TestMaxConcurrency
  TestServer
  TestClient
  TestNetCoreServer
  TestWsServer
  TestWssServer
  TestHttpServer
  TestListeners
  TestDeferCall
  TestDelayCall
)
TIMEOUT=90
mkdir -p /tmp/per-fixture-logs
echo "fixture,elapsed_sec,exit_code,passed,failed,total"
for fx in "${FIXTURES[@]}"; do
  log=/tmp/per-fixture-logs/$fx.log
  start=$(date +%s)
  timeout --kill-after=10 ${TIMEOUT}s \
    dotnet test "$PROJ" -c Release --no-build --nologo \
      --filter "FullyQualifiedName~$fx&FullyQualifiedName!~Benchmark" \
      -f net7.0 > "$log" 2>&1
  rc=$?
  end=$(date +%s)
  elapsed=$((end-start))
  # Try to grab the summary line
  summary=$(grep -E "Passed!|Failed!" "$log" | tail -1)
  passed=$(echo "$summary" | grep -oE 'Passed:[[:space:]]*[0-9]+' | grep -oE '[0-9]+' || echo "?")
  failed=$(echo "$summary" | grep -oE 'Failed:[[:space:]]*[0-9]+' | grep -oE '[0-9]+' || echo "?")
  total=$(echo "$summary"  | grep -oE 'Total:[[:space:]]*[0-9]+'  | grep -oE '[0-9]+' || echo "?")
  echo "$fx,$elapsed,$rc,$passed,$failed,$total"
done
