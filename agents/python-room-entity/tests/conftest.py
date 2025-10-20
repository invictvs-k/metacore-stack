import os
import shutil
import subprocess
import tempfile
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
def room_server() -> Iterator[Dict[str, object]]:
    env = os.environ.copy()
    env.setdefault("ASPNETCORE_URLS", f"{SERVER_URL}")
    
    # Create temporary files to capture stdout and stderr
    stdout_file = tempfile.NamedTemporaryFile(mode='w+', delete=False, suffix='_stdout.log')
    stderr_file = tempfile.NamedTemporaryFile(mode='w+', delete=False, suffix='_stderr.log')
    stdout_file = tempfile.NamedTemporaryFile(mode='w+', delete=False, suffix='.log')
    stderr_file = tempfile.NamedTemporaryFile(mode='w+', delete=False, suffix='.log')
    
    try:
        process = subprocess.Popen(
            [
                "dotnet",
                "run",
                "--project",
                str(SERVER_PROJECT),
            ],
            cwd=str(ROOT),
            env=env,
            stdout=stdout_file,
            stderr=stderr_file,
        )
        try:
            _wait_for_health()
        except Exception:
            process.terminate()
            
            # Read the tail of the output files for debugging
            stdout_file.flush()
            stderr_file.flush()
            
            def read_tail(file_path: str, lines: int = 50) -> str:
                """Read the last N lines from a file."""
                try:
                    with open(file_path, 'r') as f:
                        all_lines = f.readlines()
                        tail_lines = all_lines[-lines:] if len(all_lines) > lines else all_lines
                        return ''.join(tail_lines)
                except Exception as e:
                    return f"Error reading file: {e}"
            # Read the tail of stdout and stderr for debugging
            stdout_file.flush()
            stderr_file.flush()
            
            def read_tail(filepath: str, lines: int = 50) -> str:
                try:
                    with open(filepath, 'r') as f:
                        content = f.readlines()
                        tail = content[-lines:] if len(content) > lines else content
                        return ''.join(tail)
                except Exception:
                    return "(unable to read output)"
            
            stdout_tail = read_tail(stdout_file.name)
            stderr_tail = read_tail(stderr_file.name)
            
            error_msg = "RoomServer failed to start (healthcheck failed).\n"
            if stdout_tail:
                error_msg += f"\n--- Last 50 lines of stdout ---\n{stdout_tail}"
            if stderr_tail:
                error_msg += f"\n--- Last 50 lines of stderr ---\n{stderr_tail}"
            error_msg = "RoomServer failed to start (healthcheck failed)."
            if stdout_tail.strip():
                error_msg += f"\n\nLast 50 lines of stdout:\n{stdout_tail}"
            if stderr_tail.strip():
                error_msg += f"\n\nLast 50 lines of stderr:\n{stderr_tail}"
            
            raise RuntimeError(error_msg) from None

        yield {"base_url": SERVER_URL, "process": process}

        process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
    finally:
        # Clean up temporary files
        stdout_file.close()
        stderr_file.close()
        try:
            os.unlink(stdout_file.name)
        except Exception:
            pass
        try:
            os.unlink(stderr_file.name)
        except Exception:
            pass


@pytest.fixture(scope="session", autouse=True)
def ensure_server_exists(room_server: Dict[str, object]) -> None:
    # The fixture ensures the server is started before any tests run.
    return None
