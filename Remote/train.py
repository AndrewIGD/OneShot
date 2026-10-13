from __future__ import annotations

import argparse
import os
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

from src.gdrive import upload_folder

REMOTE_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = REMOTE_DIR.parent
RESULTS_PARENT = PROJECT_ROOT / "results"


def _is_root() -> bool:
    return hasattr(os, "geteuid") and os.geteuid() == 0


def _has_passwordless_sudo() -> bool:
    try:
        return (
            subprocess.run(
                ["sudo", "-n", "true"],
                capture_output=True,
                timeout=10,
                check=False,
            ).returncode
            == 0
        )
    except (FileNotFoundError, subprocess.TimeoutExpired):
        return False


def require_shutdown_privileges() -> None:
    """Exit unless we can run `shutdown -h now` (root or passwordless sudo)."""
    if os.name != "posix":
        raise SystemExit(
            "This script only supports POSIX shutdown; run on Linux/macOS as root or with passwordless sudo.",
        )
    if _is_root():
        return
    if _has_passwordless_sudo():
        return
    raise SystemExit(
        "Refusing to start: need root or passwordless sudo so the machine can "
        "halt after training (`shutdown -h now`). Example: run with "
        "`sudo python ...` or add a NOPASSWD rule for shutdown.",
    )


def shutdown_host() -> None:
    if _is_root():
        subprocess.run(["shutdown", "-h", "now"], check=True)
    else:
        subprocess.run(["sudo", "-n", "shutdown", "-h", "now"], check=True)


def run_command_with_log(
    cmd: list[str],
    log_path: Path,
    *,
    cwd: Path | None = None,
) -> None:
    """Run a command, streaming combined stdout/stderr to the console and log_path."""
    with log_path.open("w", encoding="utf-8") as log_file:
        proc = subprocess.Popen(
            cmd,
            cwd=os.fspath(cwd) if cwd is not None else None,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            bufsize=1,
        )
        try:
            assert proc.stdout is not None
            for line in proc.stdout:
                log_file.write(line)
                log_file.flush()
                sys.stdout.write(line)
                sys.stdout.flush()
        finally:
            retcode = proc.wait()
        if retcode != 0:
            raise subprocess.CalledProcessError(retcode, cmd)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run ML-Agents training with TensorBoard for a given experiment config.",
    )
    parser.add_argument(
        "experiment",
        type=Path,
        help="Path to the trainer YAML (e.g. Assets/Configs/Legacy/Experiment4.yaml)",
    )
    parser.add_argument(
        "--run-id",
        default=None,
        help="ML-Agents run id (default: experiment config filename without extension)",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Overwrite existing run with the same run-id under this session folder",
    )
    parser.add_argument(
        "--env-path",
        type=Path,
        default=None,
        metavar="PATH",
        help="Unity build executable path (overrides env_settings.env_path); forwarded as mlagents-learn --env",
    )
    parser.add_argument(
        "--no-default-unity-args",
        action="store_true",
        help="When using --env-path, skip default Unity CLI args (-batchmode -nographics -logFile)",
    )
    parser.add_argument(
        "--env-arg",
        dest="unity_extra_args",
        action="append",
        default=None,
        metavar="TOKEN",
        help=(
            "Extra Unity player token after mlagents-learn --env-args (repeat flag per token)"
        ),
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    require_shutdown_privileges()

    experiment_path = (
        args.experiment.resolve()
        if args.experiment.is_absolute()
        else (PROJECT_ROOT / args.experiment).resolve()
    )
    if not experiment_path.is_file():
        raise SystemExit(f"Experiment config not found: {experiment_path}")

    run_id = args.run_id if args.run_id is not None else experiment_path.stem

    now_utc = datetime.now(timezone.utc).isoformat()
    now_utc_fs = now_utc.replace(":", "-")

    session_dir = RESULTS_PARENT / now_utc_fs
    session_dir.mkdir(parents=True, exist_ok=True)

    env_executable: Path | None = None
    if args.env_path is not None:
        env_executable = (
            args.env_path.resolve()
            if args.env_path.is_absolute()
            else (PROJECT_ROOT / args.env_path).resolve()
        )

    subprocess.Popen(
        [
            "tensorboard",
            "--logdir",
            str(session_dir.resolve()),
            "--host",
            "0.0.0.0",
            "--port",
            "6006",
        ]
    )

    cmd = [
        "mlagents-learn",
        str(experiment_path),
        "--run-id",
        run_id,
        "--results-dir",
        str(session_dir.resolve()),
    ]
    if args.force:
        cmd.append("--force")
    if env_executable is not None:
        cmd.extend(["--env", str(env_executable)])

        unity_tokens: list[str] = []
        if not args.no_default_unity_args:
            unity_log_path = session_dir / f"{run_id}_UnityPlayer.log"
            unity_tokens.extend(
                [
                    "-batchmode",
                    "-nographics",
                    "-logFile",
                    str(unity_log_path.resolve()),
                ],
            )
        extra = args.unity_extra_args or []
        unity_tokens.extend(extra)
        if unity_tokens:
            cmd.append("--env-args")
            cmd.extend(unity_tokens)

    mlagents_cwd = env_executable.parent if env_executable is not None else None

    print(f"Experiment config: {experiment_path}")
    if env_executable is not None:
        print(f"Environment executable: {env_executable}")
        print(f"mlagents-learn cwd (Unity player directory): {mlagents_cwd}")
        if not args.no_default_unity_args:
            print(f"Unity player log: {session_dir / f'{run_id}_UnityPlayer.log'}")
    print(f"Run id: {run_id}")
    print(f"Results directory (session): {session_dir.resolve()}")
    print(f"TensorBoard: http://0.0.0.0:6006 (logdir={session_dir.resolve()})")
    print(f"Running: {' '.join(cmd)}")

    train_log = session_dir / f"{run_id}_train_console.log"
    print(f"Training console log: {train_log.resolve()}")

    run_command_with_log(cmd, train_log, cwd=mlagents_cwd)

    upload_folder(session_dir)

    print("Training finished; halting host.", flush=True)
    shutdown_host()


if __name__ == "__main__":
    main()
