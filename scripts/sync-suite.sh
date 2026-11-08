#!/usr/bin/env bash
# Motif.NET shared web kit: copied from github.com/motif-dotnet/.github/tree/main/brand/web. Don't edit an app's copy.
#
# Copies the kit into this repo's Blazor app (the one *.App folder at the root), or with --check,
# reports whether the app's copy has drifted from it.
#
#   scripts/sync-suite.sh                    copy the kit in from GitHub
#   scripts/sync-suite.sh --check            report drift; exits 1 if there is any
#   scripts/sync-suite.sh --from ../Motif.NET    use a local clone of motif-dotnet/.github instead
#
# What goes where, in the app:
#   brand/web/motif.css  ->  wwwroot/css/motif.css
#   brand/web/fonts/     ->  wwwroot/fonts/   (the whole folder)
#   brand/web/Suite/     ->  Suite/           (the whole folder: C# and Razor in namespace Motif.Suite)
#   brand/web/sync-suite.sh -> scripts/sync-suite.sh (this script)
set -euo pipefail

check=false
from=""
while [ $# -gt 0 ]; do
  case "$1" in
    --check) check=true ;;
    --from) from="$2"; shift ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
  shift
done

root="$(cd "$(dirname "$0")/.." && pwd)"
apps=("$root"/*.App)
if [ ${#apps[@]} -ne 1 ] || [ ! -d "${apps[0]}" ]; then
  echo "Expected one *.App folder in $root" >&2; exit 2
fi
app="${apps[0]}"

if [ -z "$from" ]; then
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' EXIT
  git clone --quiet --depth 1 https://github.com/motif-dotnet/.github.git "$tmp/kit"
  from="$tmp/kit"
fi
kit="$from/brand/web"
[ -f "$kit/motif.css" ] || { echo "No kit at $kit" >&2; exit 2; }

# pairs of kit path and app path
pairs=(
  "$kit/motif.css" "$app/wwwroot/css/motif.css"
  "$kit/fonts" "$app/wwwroot/fonts"
  "$kit/Suite" "$app/Suite"
  "$kit/sync-suite.sh" "$root/scripts/sync-suite.sh"
)

if $check; then
  drift=0
  for ((i = 0; i < ${#pairs[@]}; i += 2)); do
    if ! diff -r --strip-trailing-cr -q "${pairs[i]}" "${pairs[i+1]}" > /dev/null 2>&1; then
      echo "Drifted: ${pairs[i+1]#"$root"/}"
      diff -r --strip-trailing-cr -q "${pairs[i]}" "${pairs[i+1]}" 2>&1 | sed 's/^/  /' || true
      drift=1
    fi
  done
  if [ $drift -eq 1 ]; then
    echo "The app's copy of the Motif web kit differs from motif-dotnet/.github brand/web. Run scripts/sync-suite.sh to bring it up to date."
    exit 1
  fi
  echo "The app's copy of the Motif web kit is up to date."
  exit 0
fi

# One block, read whole before it runs, because the last copy replaces this script.
{
  for ((i = 0; i < ${#pairs[@]}; i += 2)); do
    src="${pairs[i]}"; dst="${pairs[i+1]}"
    if [ -d "$src" ]; then
      rm -rf "$dst"; mkdir -p "$dst"; cp -R "$src"/. "$dst"/
    else
      mkdir -p "$(dirname "$dst")"; cp "$src" "$dst"
    fi
  done
  echo "Copied the Motif web kit into ${app#"$root"/}."
  exit 0
}
