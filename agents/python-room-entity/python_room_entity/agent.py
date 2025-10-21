"""High level orchestration logic that turns a RoomClient into an AI agent."""

from __future__ import annotations

import json
import logging
from collections import deque
from dataclasses import dataclass, field
from typing import Any, Deque, Dict, Iterable, List

from .client import RoomClient
from .config import EntitySpec
from .openai_client import ChatMessage, OpenAIResponder

DEFAULT_SYSTEM_PROMPT = """You are a helpful multi-modal room entity that collaborates with other
participants. Always respond with a JSON object describing the action you wish
me to perform. The schema is one of:

- {"action": "chat", "text": string, "channel"?: string}
- {"action": "command", "target": EntityId, "port"?: string, "inputs"?: object, "channel"?: string}
- {"action": "event", "kind": string (SCREAMING_CASE), "data"?: object, "channel"?: string}
- {"action": "none"} (to skip a turn)

If you need to send multiple actions, reply with an array of the objects above.
"""

LOGGER = logging.getLogger(__name__)


@dataclass
class RoomAgent:
    """Coordinates RoomClient IO with an OpenAI reasoning back-end."""

    client: RoomClient
    entity: EntitySpec
    room_id: str
    responder: OpenAIResponder
    system_prompt: str = DEFAULT_SYSTEM_PROMPT
    max_history: int = 20
    _history: Deque[ChatMessage] = field(init=False)
    _running: bool = field(default=False, init=False)

    def __post_init__(self) -> None:
        """Initialize the history deque with the configured max_history."""
        self._history = deque(maxlen=self.max_history)

    def start(self) -> List[Dict[str, Any]]:
        """Connect to the room and begin processing incoming messages."""

        if self._running:
            raise RuntimeError("agent already running")
        self.client.add_message_handler(self._on_message)
        self.client.add_event_handler(self._on_event)
        self.client.connect()
        members = self.client.join(self.room_id, self.entity)
        self._running = True
        LOGGER.info("Agent %s joined room %s", self.entity.entity_id, self.room_id)
        return members

    def stop(self) -> None:
        """Leave the room and close the client connection."""

        if not self._running:
            return
        self.client.leave()
        self.client.disconnect()
        self._running = False
        LOGGER.info("Agent %s stopped", self.entity.entity_id)

    # Internal event handlers -------------------------------------------------

    def _on_message(self, message: Dict[str, Any]) -> None:
        if not self._running:
            return
        if message.get("from") == self.entity.entity_id:
            return
        formatted = self._format_room_message(message)
        self._append_history({"role": "user", "content": formatted})
        self._react()

    def _on_event(self, event: Dict[str, Any]) -> None:
        if not self._running:
            return
        formatted = self._format_room_event(event)
        self._append_history({"role": "user", "content": formatted})

    # Decision loop ----------------------------------------------------------

    def _react(self) -> None:
        transcript: List[ChatMessage] = [{"role": "system", "content": self.system_prompt}]
        transcript.extend(self._history)
        try:
            content = self.responder.respond(transcript)
        except Exception:  # pragma: no cover - logging guard
            LOGGER.exception("OpenAI responder failed")
            return
        self._append_history({"role": "assistant", "content": content})
        for action in self._parse_actions(content):
            self._execute_action(action)

    # Helpers ----------------------------------------------------------------

    def _execute_action(self, action: Dict[str, Any]) -> None:
        action_type = action.get("action")
        if action_type == "chat":
            text = action.get("text")
            if text:
                channel = action.get("channel", "room")
                self.client.send_chat(text=text, channel=channel)
        elif action_type == "command":
            target = action.get("target")
            if not target:
                LOGGER.warning("command action missing target: %s", action)
                return
            port = action.get("port")
            inputs = action.get("inputs") or {}
            channel = action.get("channel", "room")
            self.client.send_command(target=target, port=port, inputs=inputs, channel=channel)
        elif action_type == "event":
            kind = action.get("kind")
            if not kind:
                LOGGER.warning("event action missing kind: %s", action)
                return
            channel = action.get("channel", "room")
            data = action.get("data") or {}
            self.client.send_event(kind=kind, data=data, channel=channel)
        elif action_type in {"none", None}:
            LOGGER.debug("agent chose to skip action: %s", action)
        else:
            LOGGER.warning("unknown action type: %s", action_type)

    def _parse_actions(self, content: str) -> Iterable[Dict[str, Any]]:
        try:
            data = json.loads(content)
        except json.JSONDecodeError:
            return [{"action": "chat", "text": content}]
        if isinstance(data, dict):
            return [data]
        if isinstance(data, list):
            return [item for item in data if isinstance(item, dict)]
        return [{"action": "chat", "text": content}]

    def _format_room_message(self, message: Dict[str, Any]) -> str:
        sender = message.get("from", "unknown")
        channel = message.get("channel", "room")
        payload = message.get("payload")
        type_ = message.get("type")
        return json.dumps({
            "type": type_,
            "from": sender,
            "channel": channel,
            "payload": payload,
        }, ensure_ascii=False)

    def _format_room_event(self, event: Dict[str, Any]) -> str:
        return json.dumps({
            "event": event.get("payload", {}).get("kind"),
            "data": event.get("payload", {}).get("data"),
        }, ensure_ascii=False)

    def _append_history(self, entry: ChatMessage) -> None:
        self._history.append(entry)
