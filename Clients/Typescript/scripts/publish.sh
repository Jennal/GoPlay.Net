#!/bin/sh
# 发布到 npmjs 的一站式脚本：
#   1) clean + 编译到 dist/
#   2) 跑测试（默认：离线单测 + 起 Demo.WsServer 的 e2e）
#   3) npm publish --dry-run 展示 tarball 内容
#   4) 等用户手工确认
#   5) npm publish
#   6) 可选：打 git tag
#
# 用法：
#   bash scripts/publish.sh              # 跑全套测试（含 e2e，需要 .NET SDK）
#   SKIP_E2E=1 bash scripts/publish.sh   # 只跑离线单测（CI 或没 .NET 时）
#   NPM_TAG=next bash scripts/publish.sh # 以非 latest tag 发布
#   SKIP_GIT_TAG=1 bash scripts/publish.sh
#
# 注意：
# - 需要先 `npm login`。脚本会检查登录态。
# - prepublishOnly 已经会再跑一次 build+test，这里提前跑是为了尽早失败 + 交互确认。
set -eu

DIR=$(cd "$(dirname "$0")/.." && pwd)
cd "$DIR"

SKIP_E2E=${SKIP_E2E:-0}
SKIP_GIT_TAG=${SKIP_GIT_TAG:-0}
NPM_TAG=${NPM_TAG:-latest}

PKG_NAME=$(node -p "require('./package.json').name")
PKG_VERSION=$(node -p "require('./package.json').version")

log() { printf '[publish.sh] %s\n' "$*"; }
die() { printf '[publish.sh] ERROR: %s\n' "$*" >&2; exit 1; }

confirm() {
  prompt=$1
  printf '[publish.sh] %s [y/N] ' "$prompt"
  # Git Bash on Windows 里 read 需要 -r，外加 </dev/tty 防止被管道吞掉。
  if [ -t 0 ]; then
    read -r ans
  else
    read -r ans </dev/tty
  fi
  case "$ans" in
    y|Y|yes|YES) return 0 ;;
    *) return 1 ;;
  esac
}

log "Package : $PKG_NAME@$PKG_VERSION"
log "NPM tag : $NPM_TAG"
log "Workdir : $DIR"

# --- 前置检查 ---------------------------------------------------------------
command -v node >/dev/null 2>&1 || die "node not found on PATH"
command -v npm  >/dev/null 2>&1 || die "npm not found on PATH"

log "Checking npm auth..."
if ! NPM_USER=$(npm whoami 2>/dev/null); then
  die "npm not logged in. Run 'npm login' first."
fi
log "Logged in as: $NPM_USER"

# git 状态警告（不强制阻断，发布前最好工作区干净）
if command -v git >/dev/null 2>&1 && [ -d "$DIR/.git" ] || git -C "$DIR" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  if [ -n "$(git -C "$DIR" status --porcelain 2>/dev/null)" ]; then
    log "WARN: git working tree not clean."
    git -C "$DIR" status --short || true
    confirm "Continue anyway?" || die "aborted by user"
  fi
fi

# 同名同版本已发布则拒绝
if npm view "$PKG_NAME@$PKG_VERSION" version >/dev/null 2>&1; then
  die "$PKG_NAME@$PKG_VERSION already exists on registry. Bump version first (npm version patch|minor|major)."
fi

# --- Step 1: 编译 -----------------------------------------------------------
log "Step 1/5: clean & build"
rm -rf "$DIR/dist"
npm run build

# --- Step 2: 测试 -----------------------------------------------------------
if [ "$SKIP_E2E" = "1" ]; then
  log "Step 2/5: offline unit tests only (SKIP_E2E=1)"
  npm test
else
  log "Step 2/5: running e2e (offline units + Demo.WsServer)"
  if ! command -v dotnet >/dev/null 2>&1; then
    die "dotnet not found but e2e requested. Install .NET SDK or rerun with SKIP_E2E=1."
  fi
  bash "$DIR/scripts/test.sh"
fi

# --- Step 3: 打包预览 -------------------------------------------------------
log "Step 3/5: npm publish --dry-run (tarball preview)"
npm publish --dry-run --tag "$NPM_TAG"

# --- Step 4: 人工确认 -------------------------------------------------------
log "Step 4/5: confirm"
log "About to publish $PKG_NAME@$PKG_VERSION to $(npm config get registry) with tag '$NPM_TAG'."
confirm "Proceed with real publish?" || die "aborted by user"

# --- Step 5: 真发布 + 打 tag ------------------------------------------------
log "Step 5/5: npm publish"
npm publish --tag "$NPM_TAG"
log "Published $PKG_NAME@$PKG_VERSION"

if [ "$SKIP_GIT_TAG" = "1" ]; then
  log "SKIP_GIT_TAG=1 -> skipping git tag"
else
  if command -v git >/dev/null 2>&1 && git -C "$DIR" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    TAG="ts-client-v$PKG_VERSION"
    if git -C "$DIR" rev-parse -q --verify "refs/tags/$TAG" >/dev/null; then
      log "git tag $TAG already exists, skipping"
    else
      if confirm "Create git tag $TAG at HEAD?"; then
        git -C "$DIR" tag -a "$TAG" -m "$PKG_NAME $PKG_VERSION"
        log "Created tag $TAG. Push with: git push origin $TAG"
      fi
    fi
  fi
fi

log "Done."
