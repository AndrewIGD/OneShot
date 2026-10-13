"""Upload files to Google Drive with a service account.

Requires: ``pip install google-api-python-client google-auth``

Share ``GDRIVE_ROOT_FOLDER_ID`` with the service account email (Editor) so
uploads land in your Drive; service accounts only write where they have access.

Run ``python -m src.gdrive`` or ``python src/gdrive.py`` to recursively upload a
local folder (see :func:`upload_folder`). Each path must contain a ``results``
segment; see :func:`upload_file`.

Avoid committing real keys; replace the placeholders below locally.
"""

from __future__ import annotations

import os
import argparse
import json
from pathlib import Path

from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

_SCOPES = ("https://www.googleapis.com/auth/drive.file",)

CREDENTIALS_JSON = os.environ.get("GOOGLE_CREDENTIALS_JSON", "")

GDRIVE_ROOT_FOLDER_ID = "IGDStickBrawlers"

_RESULTS_SEGMENT = "results"

__all__ = [
    "upload_file",
    "upload_folder",
    "CREDENTIALS_JSON",
    "GDRIVE_ROOT_FOLDER_ID",
]


def _drive_credentials():
    # 1. Environment variable path
    env_creds = os.environ.get("GOOGLE_APPLICATION_CREDENTIALS")
    if env_creds and Path(env_creds).is_file():
        return service_account.Credentials.from_service_account_file(
            env_creds,
            scopes=_SCOPES,
        )

    # 2. Local service_account.json in Remote/ or project root
    for candidate in [
        Path(__file__).resolve().parent.parent / "service_account.json",
        Path(__file__).resolve().parent.parent / "credentials.json",
        Path.cwd() / "service_account.json",
        Path.cwd() / "credentials.json",
    ]:
        if candidate.is_file():
            return service_account.Credentials.from_service_account_file(
                str(candidate),
                scopes=_SCOPES,
            )

    # 3. String in CREDENTIALS_JSON or GOOGLE_CREDENTIALS_JSON env var
    raw = CREDENTIALS_JSON.strip()
    if not raw or raw == "PASTE_SERVICE_ACCOUNT_JSON_HERE":
        raise ValueError(
            "Service account credentials not found. Provide credentials via "
            "GOOGLE_APPLICATION_CREDENTIALS, a service_account.json file in Remote/, "
            "or set GOOGLE_CREDENTIALS_JSON.",
        )
    info = json.loads(raw)
    return service_account.Credentials.from_service_account_info(
        info,
        scopes=_SCOPES,
    )


def _drive_subpath_after_results(local: Path) -> tuple[list[str], str]:
    """Return (folder_names_under_results, filename) for Drive mirror layout.

    ``…/OneShot/results/experiment11/Stick.onnx`` → ``(['experiment11'], 'Stick.onnx')``
    """
    try:
        idx = local.parts.index(_RESULTS_SEGMENT)
    except ValueError:
        raise ValueError(
            f"Path must contain a '{_RESULTS_SEGMENT}' segment: {local}",
        ) from None
    tail = local.parts[idx + 1 :]
    if not tail:
        raise ValueError(f"No path after '{_RESULTS_SEGMENT}/': {local}")
    *folder_parts, filename = tail
    return list(folder_parts), filename


def _escape_drive_query_literal(value: str) -> str:
    return value.replace("\\", "\\\\").replace("'", "\\'")


def _find_child_folder(service, parent_id: str, name: str) -> str | None:
    safe = _escape_drive_query_literal(name)
    q = (
        f"'{parent_id}' in parents and "
        "mimeType='application/vnd.google-apps.folder' and "
        f"name='{safe}' and trashed=false"
    )
    resp = (
        service.files()
        .list(
            q=q,
            spaces="drive",
            fields="files(id)",
            pageSize=10,
            supportsAllDrives=True,
            includeItemsFromAllDrives=True,
        )
        .execute()
    )
    files = resp.get("files", [])
    return files[0]["id"] if files else None


def _create_folder(service, parent_id: str, name: str) -> str:
    body = {
        "name": name,
        "mimeType": "application/vnd.google-apps.folder",
        "parents": [parent_id],
    }
    created = (
        service.files()
        .create(body=body, fields="id", supportsAllDrives=True)
        .execute()
    )
    cid = created.get("id")
    if not cid:
        raise RuntimeError(f"Drive API did not return folder id for {name!r}")
    return cid


def _ensure_child_folder(service, parent_id: str, name: str) -> str:
    found = _find_child_folder(service, parent_id, name)
    if found:
        return found
    return _create_folder(service, parent_id, name)


def _ensure_folder_chain(service, root_folder_id: str, folder_parts: list[str]) -> str:
    current = root_folder_id
    for part in folder_parts:
        current = _ensure_child_folder(service, current, part)
    return current


def upload_file(path: str | Path) -> str:
    """Upload under ``GDRIVE_ROOT_FOLDER_ID``, mirroring path below ``results/``.

    Local ``…/results/experiment11/Stick.onnx`` becomes
    ``<GDRIVE_ROOT_FOLDER_ID>/experiment11/Stick.onnx`` (creating folders as needed).
    Returns the Drive file id.
    """
    local = Path(path).expanduser().resolve()
    if not local.is_file():
        raise FileNotFoundError(local)

    folder_parts, upload_name = _drive_subpath_after_results(local)

    folder_id = GDRIVE_ROOT_FOLDER_ID.strip()
    if not folder_id or folder_id == "PASTE_FOLDER_ID_HERE":
        raise ValueError("Set GDRIVE_ROOT_FOLDER_ID in gdrive.py.")

    creds = _drive_credentials()
    service = build("drive", "v3", credentials=creds, cache_discovery=False)

    parent_id = _ensure_folder_chain(service, folder_id, folder_parts)

    body = {"name": upload_name, "parents": [parent_id]}
    media = MediaFileUpload(str(local), resumable=True)
    created = (
        service.files()
        .create(
            body=body,
            media_body=media,
            fields="id",
            supportsAllDrives=True,
        )
        .execute()
    )
    file_id = created.get("id")
    if not file_id:
        raise RuntimeError("Drive API returned no file id")
    return file_id


def upload_folder(folder: str | Path) -> None:
    """Recursively upload every file under ``folder`` via :func:`upload_file`.

    For each subdirectory, calls itself again. Skips names starting with ``.``
    for both files and directories (e.g. ``.DS_Store``, ``.git``).
    """
    root = Path(folder).expanduser().resolve()
    if not root.is_dir():
        raise NotADirectoryError(root)

    for entry in sorted(root.iterdir(), key=lambda p: p.name):
        if entry.name.startswith("."):
            continue
        if entry.is_file():
            upload_file(entry)
        elif entry.is_dir():
            upload_folder(entry)