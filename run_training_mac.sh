#!/usr/bin/env bash
set -e

CONFIG="${1:-Assets/Configs/Experiment1V2.yaml}"
RUN_ID="${2:-mac_experiment_$(date +%Y%m%d_%H%M%S)}"
EXTRA_ARGS="${@:3}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

if [ ! -d ".venv" ]; then
    echo "Error: Python virtual environment (.venv) not found in $SCRIPT_DIR"
    exit 1
fi

if [ ! -d "Builds/Mac/oneshot.app" ]; then
    echo "Error: Mac build not found at Builds/Mac/oneshot.app"
    exit 1
fi

chmod +x "Builds/Mac/oneshot.app/Contents/MacOS/IGDStickBrawlers"

echo "=========================================================="
echo " Starting ML-Agents Training on macOS"
echo " Config:    $CONFIG"
echo " Run ID:    $RUN_ID"
echo " Build:     Builds/Mac/oneshot.app"
echo "=========================================================="

./.venv/bin/mlagents-learn "$CONFIG" \
    --run-id="$RUN_ID" \
    --env="Builds/Mac/oneshot.app" \
    --torch-device=cpu \
    $EXTRA_ARGS
