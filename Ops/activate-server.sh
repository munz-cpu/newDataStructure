#!/usr/bin/env bash
set -euo pipefail

environment=${1:?environment}
release=${2:?release}
archive=${3:?archive}
digest=${4:?sha256}
[[ "$environment" == development || "$environment" == production ]]
[[ "$release" =~ ^[0-9a-f]{40}-[0-9]+$ && "$digest" =~ ^[0-9a-f]{64}$ ]]
[[ "$archive" == "/tmp/divinewar-$environment-"* && -f "$archive" ]]
root="$HOME/divinewar/$environment"
mkdir -p "$root/releases"
exec 8>"$root/deploy.lock"
flock -n 8 || { echo 'A deployment is already in progress.' >&2; exit 1; }
staging=$(mktemp -d "$root/releases/.incoming-XXXXXX")
trap 'rm -rf -- "$staging"; rm -f -- "$archive"' EXIT
printf '%s  %s\n' "$digest" "$archive" | sha256sum --check --status
tar --no-same-owner -xzf "$archive" -C "$staging"
test -x "$staging/DivineWarServer.x86_64"
unit="divinewar-server@$environment.service"
systemctl --user cat "$unit" >/dev/null
target="$root/releases/$release"
if [[ ! -d "$target" ]]; then mv -T "$staging" "$target"; fi
previous=$(readlink "$root/current" || true)
link_release() {
    ln -sfnT "$1" "$root/current.new"
    mv -Tf "$root/current.new" "$root/current"
}
link_release "$target"
if systemctl --user restart "$unit"; then
    invocation=$(systemctl --user show "$unit" --property=InvocationID --value)
    for _ in {1..60}; do
        if [[ -n "$invocation" ]] && systemctl --user is-active --quiet "$unit" &&
            journalctl --user "_SYSTEMD_INVOCATION_ID=$invocation" --no-pager -o cat |
            grep -F '[DivineWar] JOIN_CODE=' >/dev/null; then
            echo 'The dedicated server registered a room.'
            exit 0
        fi
        sleep 2
    done
fi
echo 'The server failed to register a room. Restoring the previous release.' >&2
if [[ -n "$previous" ]]; then
    link_release "$previous"
    systemctl --user restart "$unit"
else
    systemctl --user stop "$unit"
    rm -f "$root/current"
fi
exit 1
