"""SignalR based client for interacting with the RoomServer hub."""

from __future__ import annotations

import json
import logging
import threading
import time
from dataclasses import dataclass
from typing import Any, Callable, Dict, Iterable, List, Optional

from signalrcore.hub_connection_builder import HubConnectionBuilder

from .config import ChatPayload, CommandPayload, EntitySpec, EventPayload, MessagePayload

MessageHandler = Callable[[Dict[str, Any]], None]
EventHandler = Callable[[Dict[str, Any]], None]

LOGGER = logging.getLogger(__name__)


@dataclass
class RoomConnectionState:
    """Tracks the mutable state of the active SignalR connection."""

    room_id: Optional[str] = None
    entity: Optional[EntitySpec] = None


class RoomClient:
    """High level RoomServer client that wraps the SignalR hub connection."""

    def __init__(
        self,
        hub_url: str,
        *,
        headers: Optional[Dict[str, str]] = None,
        logger: Optional[logging.Logger] = None,
        reconnect_delays: Iterable[float] | None = None,
        verify_ssl: Optional[bool] = None,
    ) -> None:
        self._hub_url = hub_url
        self._headers = headers or {}
        self._logger = logger or LOGGER
        self._reconnect_delays = list(reconnect_delays or (0, 2, 5, 10))
        # Infer verify_ssl from URL scheme if not explicitly provided
        if verify_ssl is None:
            self._verify_ssl = hub_url.startswith("https://")
        else:
            self._verify_ssl = verify_ssl
        self._hub = None
        self._state = RoomConnectionState()
        self._message_handlers: List[MessageHandler] = []
        self._event_handlers: List[EventHandler] = []
        self._lock = threading.Lock()
        self._disconnected = threading.Event()
        self._disconnected.set()

    @property
    def is_connected(self) -> bool:
        return self._hub is not None and not self._disconnected.is_set()

    @property
    def verify_ssl(self) -> bool:
        """Whether SSL verification is enabled for the hub connection."""
        return self._verify_ssl
    def connect(self) -> None:
        """Initialise the SignalR hub connection."""

        if self._hub is not None:
            raise RuntimeError("room client is already connected")

        # Build hub in a local variable first
        hub = (
            HubConnectionBuilder()
            .with_url(
                self._hub_url,
                options={
                    "headers": self._headers,
                    "verify_ssl": self._verify_ssl,
                },
            )
            .configure_logging(logging.INFO)
            .with_automatic_reconnect({"type": "raw", "keep_alive_interval": 10, "reconnect_interval": self._reconnect_delays})
            .build()
        )

        # Register handlers on the local hub
        hub.on("message", self._handle_message)
        hub.on("event", self._handle_event)
        hub.on_close(self._handle_close)

        # Only clear disconnected flag and assign to self._hub after successful start
        try:
            hub.start()
            self._hub = hub
            self._disconnected.clear()
            self._logger.info("Connected to hub %s", self._hub_url)
        except Exception:
            # Re-set disconnected flag on failure (it was already set, so this is defensive)
            self._disconnected.set()
            raise

    def disconnect(self) -> None:
        """Gracefully close the SignalR connection."""

        if self._hub is None:
            return

        try:
            self._hub.stop()
        finally:
            self._hub = None
            self._disconnected.set()
            self._logger.info("Disconnected from hub")

    def join(self, room_id: str, entity: EntitySpec) -> List[Dict[str, Any]]:
        """Join a room as the provided entity."""

        self._ensure_connected()
        payload = entity.as_dict()
        self._logger.debug("Joining room %s with payload %s", room_id, payload)
        members = self._hub.invoke("Join", [room_id, payload])
        if not isinstance(members, list):
            raise RuntimeError(f"unexpected join response: {members!r}")
        with self._lock:
            self._state.room_id = room_id
            self._state.entity = entity
        return members

    def leave(self) -> None:
        """Leave the current room if joined."""

        self._ensure_connected()
        if not self._state.room_id or not self._state.entity:
            return
        self._hub.invoke("Leave", [self._state.room_id, self._state.entity.entity_id])
        self._logger.info("Left room %s", self._state.room_id)
        with self._lock:
            self._state.room_id = None
            self._state.entity = None

    def send_message(self, message: MessagePayload) -> None:
        """Send an arbitrary message payload to the room."""

        self._ensure_joined()
        data = message.as_dict()
        data["from"] = self._state.entity.entity_id if self._state.entity else None
        response = self._hub.invoke("SendToRoom", [self._state.room_id, data])
        self._logger.debug("Sent message response: %s", response)

    def send_chat(self, text: str, channel: str = "room") -> None:
        payload = MessagePayload(channel=channel, type="chat", payload=ChatPayload(text=text))
        self.send_message(payload)

    def send_command(self, target: str, port: Optional[str], inputs: Optional[Dict[str, Any]] = None, *, channel: str = "room") -> None:
        payload = MessagePayload(
            channel=channel,
            type="command",
            payload=CommandPayload(target=target, port=port, inputs=inputs or {}),
        )
        self.send_message(payload)

    def send_event(self, kind: str, data: Optional[Dict[str, Any]] = None, *, channel: str = "room") -> None:
        payload = MessagePayload(channel=channel, type="event", payload=EventPayload(kind=kind, data=data or {}))
        self.send_message(payload)

    def add_message_handler(self, handler: MessageHandler) -> None:
        self._message_handlers.append(handler)

    def add_event_handler(self, handler: EventHandler) -> None:
        self._event_handlers.append(handler)

    def _handle_message(self, args: List[Any]) -> None:
        message = self._unwrap_args(args)
        if not message:
            return
        for handler in list(self._message_handlers):
            try:
                handler(message)
            except Exception:  # pragma: no cover - logging side effect only
                self._logger.exception("message handler raised")

    def _handle_event(self, args: List[Any]) -> None:
        event = self._unwrap_args(args)
        if not event:
            return
        for handler in list(self._event_handlers):
            try:
                handler(event)
            except Exception:  # pragma: no cover
                self._logger.exception("event handler raised")

    def _handle_close(self, *args: Any) -> None:
        with self._lock:
            self._hub = None
        self._disconnected.set()
        self._logger.warning("SignalR connection closed")

    def _unwrap_args(self, args: List[Any]) -> Optional[Dict[str, Any]]:
        if not args:
            return None
        if len(args) == 1 and isinstance(args[0], str):
            try:
                return json.loads(args[0])
            except json.JSONDecodeError:
                pass
        value = args[0]
        if isinstance(value, dict):
            return value
        return None

    def _ensure_connected(self) -> None:
        if self._hub is None:
            raise RuntimeError("room client is not connected")

    def _ensure_joined(self) -> None:
        if self._state.room_id is None or self._state.entity is None:
            raise RuntimeError("room client must join a room before sending messages")

    def wait_until_connected(self, timeout: float = 10.0) -> bool:
        """Wait until the hub connection transitions to the connected state."""

        start = time.time()
        while time.time() - start < timeout:
            if self.is_connected:
                return True
            time.sleep(0.1)
        return False
