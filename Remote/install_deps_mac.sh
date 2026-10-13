#!/usr/bin/env bash
# Install ML-Agents Release 22 (matches com.unity.ml-agents 4.0.1) on macOS.
# Run from the repo root: ./Remote/install_deps_mac.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

PY_VERSION="3.10.19"
VENV_DIR=".venv"
MLAGENTS_SRC=".mlagents-src/ml-agents"
MLAGENTS_TAG="release_22"

if ! command -v pyenv >/dev/null 2>&1; then
  echo "pyenv not found. Install with: brew install pyenv" >&2
  exit 1
fi

if ! pyenv versions --bare | grep -qx "$PY_VERSION"; then
  echo "Installing Python $PY_VERSION via pyenv..."
  pyenv install "$PY_VERSION"
fi

pyenv local "$PY_VERSION"

if [[ ! -d "$VENV_DIR" ]]; then
  echo "Creating venv at $VENV_DIR..."
  python -m venv "$VENV_DIR"
fi

# shellcheck disable=SC1091
source "$VENV_DIR/bin/activate"

python -m pip install --upgrade pip setuptools wheel

if [[ ! -d "$MLAGENTS_SRC" ]]; then
  echo "Cloning Unity ML-Agents @ $MLAGENTS_TAG..."
  mkdir -p "$(dirname "$MLAGENTS_SRC")"
  git clone --depth 1 --branch "$MLAGENTS_TAG" \
    https://github.com/Unity-Technologies/ml-agents.git "$MLAGENTS_SRC"
fi

# Release 22 pins grpcio<=1.48.2, which has no Apple Silicon wheel and a broken
# sdist on modern setuptools (no pkg_resources). Relax the upper bound so pip
# picks an arm64 wheel of a newer grpcio.
#
# Its torch dependency is also unbounded above. Modern torch (e.g. 2.12) uses the
# new ONNX exporter, which expects onnxscript/onnx_ir and newer onnx/protobuf
# versions that conflict with ML-Agents Release 22. Pin torch to the first
# supported Release 22 version so exports stay on the compatible ONNX path.
python - <<'PY'
import pathlib
replacements = {
    '        "grpcio>=1.11.0,<=1.48.2",': '        "grpcio>=1.48.2",',
    '        "torch>=2.1.1",': '        "torch==2.1.1",',
}
for rel in ("ml-agents-envs/setup.py", "ml-agents/setup.py"):
    p = pathlib.Path(".mlagents-src/ml-agents") / rel
    text = p.read_text()
    updated = text
    for old, new in replacements.items():
        updated = updated.replace(old, new)
    if updated != text:
        p.write_text(updated)
        print(f"patched dependency constraints in {p}")
PY

python -m pip install --ignore-requires-python \
  "$MLAGENTS_SRC/ml-agents-envs" \
  "$MLAGENTS_SRC/ml-agents" \
  -r "$SCRIPT_DIR/requirements.txt"

# Keep the ML-Agents Release 22 stack pinned even if pip's resolver has seen a
# newer package from a previous install attempt.
python -m pip install --force-reinstall \
  "torch==2.1.1" \
  "onnx==1.15.0" \
  "protobuf==3.20.3" \
  "numpy==1.23.5"

echo
echo "Done. Activate with: source $VENV_DIR/bin/activate"
mlagents-learn --help >/dev/null && echo "mlagents-learn OK"
