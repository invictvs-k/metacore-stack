"""Wrappers around the official OpenAI client with test friendly hooks."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, Iterable, Optional, Protocol, TypedDict

try:  # pragma: no cover - import guarded for optional dependency resolution
    from openai import OpenAI
except Exception:  # pragma: no cover
    OpenAI = None  # type: ignore[misc, assignment]


class ChatMessage(TypedDict):
    """A chat message with a role (e.g., 'user', 'assistant', 'system') and content."""

    role: str
    content: str


class OpenAIResponder(Protocol):
    """Protocol describing the interface required by :class:`RoomAgent`."""

    def respond(self, messages: Iterable[ChatMessage], **kwargs: object) -> str:
        """Return the model response for the given chat transcript."""


@dataclass(slots=True)
class OpenAIChatClient(OpenAIResponder):
    """Thin wrapper that delegates to ``openai``'s chat completions API."""

    model: str = "gpt-4o-mini"
    temperature: float = 0.2
    max_tokens: Optional[int] = 1_000
    client: Optional[OpenAI] = None

    def __post_init__(self) -> None:  # pragma: no cover - sanity guard
        if self.client is None:
            if OpenAI is None:
                raise RuntimeError("openai package is not available")
            self.client = OpenAI()

    def respond(self, messages: Iterable[ChatMessage], **kwargs: object) -> str:
        if self.client is None:  # pragma: no cover - defensive guard
            raise RuntimeError("OpenAI client not initialised")
        # Normalize overrides: if None is passed, use the default
        model = kwargs.get("model", self.model)
        if model is None:
            model = self.model
        temperature = kwargs.get("temperature", self.temperature)
        if temperature is None:
            temperature = self.temperature
        temperature = float(temperature)
        max_tokens = kwargs.get("max_tokens", self.max_tokens)
        if max_tokens is None:
            max_tokens = self.max_tokens
        params = {
            "model": model,
            "temperature": temperature,
            "messages": list(messages),
        }
        if max_tokens is not None:
            params["max_tokens"] = int(max_tokens)
        response = self.client.chat.completions.create(**params)
        choice = response.choices[0]
        message = choice.message
        if message is None or message.content is None:
            raise RuntimeError("OpenAI response did not include message content")
        return message.content
