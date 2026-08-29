#!/bin/zsh
# Build the WebGL player and ship it to seeyuf.com/play via the website repo.
#
# Usage:
#   ./build_and_deploy_webgl.sh              # build + copy + commit + push (Netlify CI deploys)
#   ./build_and_deploy_webgl.sh --build-only # just build into Builds/WebGL
#
# Requires: Unity 6000.0.41f1 with WebGL module, the website repo cloned at $SITE_REPO,
# and the Unity editor CLOSED (batchmode needs the project lock).

set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.0.41f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$PROJECT/Builds/WebGL"
SITE_REPO="/Users/memokhoury/Desktop/work/SEEYUF/seeyuf_studios_website"
PLAY_DIR="$SITE_REPO/site/public/play"
LOG="$PROJECT/Logs/webgl_build.log"

if pgrep -f "Unity.app/Contents/MacOS/Unity.*$PROJECT" >/dev/null 2>&1; then
  echo "Unity has this project open — close the editor first (batchmode needs the lock)." >&2
  exit 1
fi

echo "==> Building WebGL (log: $LOG)"
"$UNITY" -batchmode -quit \
  -projectPath "$PROJECT" \
  -buildTarget WebGL \
  -executeMethod WebGLBuilder.Build \
  -outputPath "$BUILD_DIR" \
  -logFile "$LOG"

echo "==> Build done: $(du -sh "$BUILD_DIR" | cut -f1)"

if [[ "${1:-}" == "--build-only" ]]; then
  exit 0
fi

echo "==> Copying into website repo at site/public/play/"
rm -rf "$PLAY_DIR"
mkdir -p "$PLAY_DIR"
cp -R "$BUILD_DIR/." "$PLAY_DIR/"

cd "$SITE_REPO"
git add -A site/public/play netlify.toml
git commit -m "Ship spirit&hearts WebGL build to /play"
git push origin HEAD

echo "==> Pushed. Netlify CI will deploy — watch it at https://app.netlify.com"
echo "==> Live at https://seeyuf.com/play/ once the deploy finishes."
