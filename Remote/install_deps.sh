#!/usr/bin/env bash
set -euo pipefail

if [[ "$(id -u)" -ne 0 ]]; then
  echo "This script must be run with root privileges (e.g. sudo $0)." >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

apt-get update
apt-get install -y python3 python3-pip python3-venv

python3 -m pip install --upgrade pip
#python3 -m pip install -r requirements.txt
python3 -m pip install mlagents
