"""Wrappers around the official OpenAI client with test friendly hooks."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, Iterable, Optional, Protocol

try:  # pragma: no cover - import guarded for optional dependency resolution
    from openai import OpenAI
except Exception:  # pragma: no cover
    OpenAI = None  # type: ignore[misc, assignment]


ChatMessage = Dict[str, str]


class OpenAIResponder(Protocol):
    """Protocol describing the interface required by :class:`RoomAgent`."""

    def respond(self, messages: Iterable[ChatMessage], **kwargs: object) -> str:
        """Return the model response for the given chat transcript."""


@dataclass(slots=True)
class OpenAIChatClient(OpenAIResponder):
    """Thin wrapper that delegates to ``openai``'s chat completions API."""

    model: str = "gpt-4o-mini"
    temperature: float = 0.2
    max_output_tokens: Optional[int] = 1_000
    client: Optional[OpenAI] = None

    def __post_init__(self) -> None:  # pragma: no cover - sanity guard
        if self.client is None:
            if OpenAI is None:
                raise RuntimeError("openai package is not available")
            self.client = OpenAI()

    def respond(self, messages: Iterable[ChatMessage], **kwargs: object) -> str:
        if self.client is None:  # pragma: no cover - defensive guard
            raise RuntimeError("OpenAI client not initialised")
        response = self.client.chat.completions.create(
            model=kwargs.get("model", self.model),
            temperature=float(kwargs.get("temperature", self.temperature)),
            max_output_tokens=kwargs.get("max_output_tokens", self.max_output_tokens),
            messages=list(messages),
        )
        choice = response.choices[0]
        message = choice.message
        if message is None or message.content is None:
            raise RuntimeError("OpenAI response did not include message content")
        return message.content
