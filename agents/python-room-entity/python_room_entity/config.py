"""Typed configuration models used by the room entity client."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, Iterable, List, Mapping, Optional

from pydantic import BaseModel, Field, field_validator


class CommandPayload(BaseModel):
    """Shape for `command` type message payloads."""

    target: str = Field(..., description="EntityId that should receive the command")
    port: Optional[str] = Field(None, description="Port identifier exposed by the target entity")
    inputs: Dict[str, Any] = Field(default_factory=dict)

    @field_validator("target")
    @classmethod
    def _validate_target(cls, value: str) -> str:
        if not value or not value.startswith("E-"):
            raise ValueError("target must be a valid EntityId starting with 'E-'")
        return value


class ChatPayload(BaseModel):
    """Shape for `chat` type messages."""

    text: str

    @field_validator("text")
    @classmethod
    def _validate_text(cls, value: str) -> str:
        if not value.strip():
            raise ValueError("chat text must not be empty")
        return value


class EventPayload(BaseModel):
    """Shape for `event` type messages."""

    kind: str
    data: Dict[str, Any] = Field(default_factory=dict)

    @field_validator("kind")
    @classmethod
    def _validate_kind(cls, value: str) -> str:
        if not value or value.upper() != value:
            raise ValueError("event kind must be SCREAMING_CASE (e.g. ROOM.STATE)")
        return value


@dataclass(slots=True)
class MessagePayload:
    """Convenience builder for message payloads."""

    channel: str = "room"
    type: str = "chat"
    payload: Mapping[str, Any] | BaseModel = field(default_factory=dict)
    correlation_id: Optional[str] = None

    def as_dict(self) -> Dict[str, Any]:
        data: Dict[str, Any] = {
            "channel": self.channel,
            "type": self.type,
            "payload": self.payload.model_dump() if isinstance(self.payload, BaseModel) else dict(self.payload),
        }
        if self.correlation_id:
            data["correlationId"] = self.correlation_id
        return data


@dataclass(slots=True)
class EntitySpec:
    """Configuration for an entity joining a room."""

    entity_id: str
    kind: str
    display_name: str
    visibility: str = "public"
    capabilities: Iterable[str] = field(default_factory=list)
    owner_user_id: Optional[str] = None
    policy: Optional[Dict[str, Any]] = None

    def as_dict(self) -> Dict[str, Any]:
        caps: List[str] = list(self.capabilities)
        if any(not cap for cap in caps):
            raise ValueError("capabilities must not contain empty identifiers")
        entity = {
            "id": self.entity_id,
            "kind": self.kind,
            "display_name": self.display_name,
            "visibility": self.visibility,
            "capabilities": caps,
        }
        if self.owner_user_id:
            entity["owner_user_id"] = self.owner_user_id
        if self.policy:
            entity["policy"] = self.policy
        return entity
