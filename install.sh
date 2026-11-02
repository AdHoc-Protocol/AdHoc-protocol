#!/usr/bin/env bash
# Installs AdHocAgent on Linux and macOS:
#
#   curl -fsSL https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.sh | bash
#
# Run it again to update: the configuration file AdHocAgent.toml, with your personal UUID, is kept.
#
# Environment overrides:
#   ADHOC_VERSION      release tag to install, e.g. v1.2.3 (default: the latest release)
#   ADHOC_INSTALL_DIR  where the agent lives          (default: ~/.local/share/adhoc-agent)
#   ADHOC_BIN_DIR      where the `AdHocAgent` command goes (default: ~/.local/bin)
#   ADHOC_DOWNLOAD_URL a mirror of the release files   (default: the GitHub release)
set -euo pipefail

REPO="AdHoc-Protocol/AdHoc-protocol"
VERSION="${ADHOC_VERSION:-latest}"
INSTALL_DIR="${ADHOC_INSTALL_DIR:-$HOME/.local/share/adhoc-agent}"
BIN_DIR="${ADHOC_BIN_DIR:-$HOME/.local/bin}"

if [ -t 1 ]; then B=$'\e[1m'; G=$'\e[32m'; Y=$'\e[33m'; R=$'\e[31m'; D=$'\e[2m'; N=$'\e[0m'; else B= G= Y= R= D= N=; fi
step() { printf '%s==>%s %s\n' "$B" "$N" "$*"; }
warn() { printf '%swarning:%s %s\n' "$Y" "$N" "$*" >&2; }
fail() { printf '%serror:%s %s\n' "$R" "$N" "$*" >&2; exit 1; }

need() { command -v "$1" >/dev/null 2>&1 || fail "$1 is required: install it and run this script again."; }
need curl
need unzip

case "$(uname -s)" in
  Linux)  os=linux ;;
  Darwin) os=osx ;;
  *)      fail "$(uname -s) is not supported by this script. On Windows run in PowerShell:  irm https://raw.githubusercontent.com/$REPO/main/install.ps1 | iex" ;;
esac
case "$(uname -m)" in
  x86_64 | amd64)  arch=x64 ;;
  arm64 | aarch64) arch=arm64 ;;
  *)               fail "the $(uname -m) processor is not supported: AdHocAgent is built for x64 and arm64." ;;
esac
asset="AdHocAgent-$os-$arch.zip"

if [ -n "${ADHOC_DOWNLOAD_URL:-}" ]; then base="${ADHOC_DOWNLOAD_URL%/}"
elif [ "$VERSION" = latest ]; then base="https://github.com/$REPO/releases/latest/download"
else base="https://github.com/$REPO/releases/download/$VERSION"; fi

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

step "Downloading AdHocAgent ($VERSION, $os-$arch)"
curl -fSL --progress-bar "$base/$asset" -o "$tmp/$asset" ||
  fail "cannot download $base/$asset. Check the network, or the releases page: https://github.com/$REPO/releases"

if curl -fsSL "$base/SHA256SUMS.txt" -o "$tmp/SHA256SUMS.txt" 2>/dev/null; then
  expected="$(awk -v f="$asset" '$2 == f || $2 == "*"f { print $1 }' "$tmp/SHA256SUMS.txt")"
  if command -v sha256sum >/dev/null 2>&1; then actual="$(sha256sum "$tmp/$asset" | cut -d' ' -f1)"
  else actual="$(shasum -a 256 "$tmp/$asset" | cut -d' ' -f1)"; fi
  [ -n "$expected" ] || fail "SHA256SUMS.txt of the release has no line for $asset."
  [ "$expected" = "$actual" ] || fail "the checksum of $asset does not match the release: the download is damaged. Run the script again."
  step "Checksum verified"
else
  warn "the release has no SHA256SUMS.txt: the download is not verified."
fi

step "Installing into $INSTALL_DIR"
mkdir -p "$INSTALL_DIR" "$BIN_DIR"
# AdHocAgent.toml beside the executable holds the personal UUID: an update never replaces it
unzip -q -o "$tmp/$asset" -d "$tmp/unpacked" || [ $? -eq 1 ] || fail "cannot unpack $asset."  # 1: unzip warned and went on
[ -f "$INSTALL_DIR/AdHocAgent.toml" ] && rm -f "$tmp/unpacked/AdHocAgent.toml"
cp -R "$tmp/unpacked/." "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/AdHocAgent"
ln -sf "$INSTALL_DIR/AdHocAgent" "$BIN_DIR/AdHocAgent"

# AdHocAgent is framework-dependent: it runs on an installed .NET 10
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-runtimes 2>/dev/null | grep -q '^Microsoft\.NETCore\.App 10\.'; then
  warn "AdHocAgent runs on .NET 10, which is not found. Install the .NET 10 SDK (a description project needs it too):"
  printf '         %shttps://dotnet.microsoft.com/download/dotnet/10.0%s\n' "$B" "$N" >&2
fi

printf '\n%sAdHocAgent is installed.%s\n' "$G$B" "$N"
case ":$PATH:" in
  *":$BIN_DIR:"*) printf 'Run %sAdHocAgent%s to start.\n' "$B" "$N" ;;
  *)
    rc="$HOME/.profile"
    case "${SHELL:-}" in */zsh) rc="$HOME/.zshrc" ;; */bash) rc="$HOME/.bashrc" ;; esac
    printf '%s is not on your PATH yet. Add it with:\n\n' "$BIN_DIR"
    printf '    %secho '\''export PATH="%s:$PATH"'\'' >> %s && . %s%s\n\n' "$B" "$BIN_DIR" "$rc" "$rc" "$N"
    printf 'or run the agent as %s%s/AdHocAgent%s\n' "$B" "$BIN_DIR" "$N" ;;
esac
printf '%sNext: get your personal UUID - https://github.com/%s/blob/main/MANUAL.md#step-2-get-your-personal-uuid%s\n' "$D" "$REPO" "$N"
