#!/bin/sh
# 启动 Demo.WsServer 并跑 jest 单测。
# 流程：dotnet build -> 后台 dotnet run -> 轮询端口 -> npm test -> 清理子进程。
set -eu

DIR=$(cd "$(dirname "$0")/.." && pwd)
cd "$DIR"

SERVER_PROJ="$DIR/../../Frameworks/Demo/Demo.WsServer"
SERVER_PORT=${GOPLAY_TEST_PORT:-8888}
LOG_FILE="$DIR/.test_server.log"
SERVER_PID=""

cleanup() {
  if [ -n "$SERVER_PID" ]; then
    echo "[test.sh] Stopping server (PID $SERVER_PID)..."
    kill "$SERVER_PID" 2>/dev/null || true
    # 宽限 3s 等优雅退出，再强杀
    for _ in 1 2 3; do
      kill -0 "$SERVER_PID" 2>/dev/null || break
      sleep 1
    done
    kill -9 "$SERVER_PID" 2>/dev/null || true
    wait "$SERVER_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

echo "[test.sh] Workspace: $DIR"
echo "[test.sh] Server project: $SERVER_PROJ"
echo "[test.sh] Building Demo.WsServer (Debug)..."
dotnet build "$SERVER_PROJ" -c Debug -nologo -v minimal

SERVER_DLL=$(ls "$SERVER_PROJ"/bin/Debug/net*/Demo.WsServer.dll 2>/dev/null | head -n 1)
if [ -z "$SERVER_DLL" ]; then
  echo "[test.sh] ERROR: Demo.WsServer.dll not found after build" >&2
  exit 1
fi
SERVER_BIN_DIR=$(dirname "$SERVER_DLL")
SERVER_DLL_NAME=$(basename "$SERVER_DLL")
echo "[test.sh] Server dll: $SERVER_DLL"

# Program.cs 里写死了 AddStaticContent("../../../../../../Clients/Typescript/demo")，
# 相对 bin/Debug/net*/ 解析到仓库 Clients/Typescript/demo。目录不存在 FileSystemWatcher 会抛。
# 为了不改服务端源码，这里兜底 mkdir 一个空目录。
mkdir -p "$DIR/demo"

echo "[test.sh] Starting server on port $SERVER_PORT..."
# 必须在 bin 目录启动，AddStaticContent 的相对路径才能正确解析。
# 直接跑 dll 而不是 `dotnet run`，避免 watcher/launcher 让 kill 不级联。
(cd "$SERVER_BIN_DIR" && dotnet "$SERVER_DLL_NAME" start -p "$SERVER_PORT") >"$LOG_FILE" 2>&1 &
SERVER_PID=$!
echo "[test.sh] Server PID: $SERVER_PID (log: $LOG_FILE)"

echo "[test.sh] Waiting for port $SERVER_PORT..."
node -e "
const net = require('net');
const port = Number(process.argv[1]);
const deadline = Date.now() + 30000;
(function tick(){
  const s = net.createConnection({ host: '127.0.0.1', port }, () => { s.end(); process.exit(0); });
  s.once('error', () => {
    if (Date.now() > deadline) { console.error('[test.sh] timeout waiting server'); process.exit(1); }
    setTimeout(tick, 300);
  });
})();
" "$SERVER_PORT" || {
  echo "[test.sh] Server failed to accept on $SERVER_PORT. Recent log:" >&2
  tail -n 80 "$LOG_FILE" >&2 || true
  exit 1
}
echo "[test.sh] Server ready."

echo "[test.sh] Running jest (incl. unit_test/e2e)..."
# jest.config.js 默认把 unit_test/e2e 排除（没服务端时别误跑）；这里显式把它放回来。
npx jest --runInBand --testPathIgnorePatterns=/node_modules/ --testPathIgnorePatterns=/dist/
