#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tools_dir="$project_root/.tools"
tweego="$tools_dir/tweego"
storyformats="$tools_dir/storyformats"

tweego_version="2.1.1"
sugarcube_version="2.37.3"

if [[ ! -x "$tweego" || ! -f "$storyformats/sugarcube-2/format.js" ]]; then
  command -v curl >/dev/null || { echo "curl is required." >&2; exit 1; }
  command -v unzip >/dev/null || { echo "unzip is required." >&2; exit 1; }

  temp_dir="$(mktemp -d)"
  trap 'rm -rf "$temp_dir"' EXIT

  mkdir -p "$tools_dir"

  curl -fsSL \
    "https://github.com/tmedwards/tweego/releases/download/v${tweego_version}/tweego-${tweego_version}-linux-x64.zip" \
    -o "$temp_dir/tweego.zip"
  unzip -oq "$temp_dir/tweego.zip" -d "$tools_dir"
  chmod +x "$tweego"

  curl -fsSL \
    "https://github.com/tmedwards/sugarcube-2/releases/download/v${sugarcube_version}/sugarcube-${sugarcube_version}-for-twine-2.1-local.zip" \
    -o "$temp_dir/sugarcube.zip"
  rm -rf "$storyformats/sugarcube-2"
  unzip -q "$temp_dir/sugarcube.zip" -d "$storyformats"
fi

chmod +x "$tweego"

mkdir -p "$project_root/dist"

TWEEGO_PATH="$storyformats" "$tweego" \
  -f sugarcube-2 \
  -o "$project_root/dist/2-17am.html" \
  "$project_root/src/story.twee"

echo "Built dist/2-17am.html"
