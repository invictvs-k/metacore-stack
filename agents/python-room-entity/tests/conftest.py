import os
import shutil
import subprocess
import time
from pathlib import Path
from typing import Dict, Iterator

import pytest
import requests

if shutil.which("dotnet") is None:
    pytest.skip("dotnet executable is required for integration tests", allow_module_level=True)

ROOT = Path(__file__).resolve().parents[3]
SERVER_PROJECT = ROOT / "server-dotnet" / "src" / "RoomServer" / "RoomServer.csproj"
SERVER_URL = "http://127.0.0.1:5010"


def _wait_for_health(timeout: float = 45.0) -> None:
    deadline = time.time() + timeout
    while time.time() < deadline:
        try:
            response = requests.get(f"{SERVER_URL}/health", timeout=2.0)
            if response.status_code == 200:
                return
            else:
                time.sleep(0.5)
        except requests.RequestException:
            time.sleep(0.5)
    raise RuntimeError("RoomServer healthcheck did not become ready in time")


@pytest.fixture(scope="session")
def room_server() -> Iterator[Dict[str, str]]:
    env = os.environ.copy()
    env.setdefault("ASPNETCORE_URLS", f"{SERVER_URL}")
    process = subprocess.Popen(
        [
            "dotnet",
            "run",
            "--project",
            str(SERVER_PROJECT),
        ],
        cwd=str(ROOT),
        env=env,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        bufsize=1,
    )
    try:
        _wait_for_health()
    except Exception:
        process.terminate()
        stdout, _ = process.communicate(timeout=5)
        raise RuntimeError(f"RoomServer failed to start:\n{stdout}") from None

    yield {"base_url": SERVER_URL, "process": process}

    process.terminate()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()


@pytest.fixture(scope="session", autouse=True)
def ensure_server_exists(room_server: Dict[str, str]) -> None:
    # The fixture ensures the server is started before any tests run.
    return None
