"""Wrappers around the official OpenAI client with test friendly hooks."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable, Optional, Protocol, TypedDict

try:  # pragma: no cover - import guarded for optional dependency resolution
    from openai import OpenAI
except ImportError:  # pragma: no cover
    OpenAI = None  # type: ignore[misc, assignment]


class ChatMessage(TypedDict):
    """A chat message with a role (e.g., 'user', 'assistant', 'system') and content."""

    role: str
    content: str


class OpenAIResponder(Protocol):
    """Protocol describing the interface required by :class:`RoomAgent`."""

    def respond(
        self,
        messages: Iterable[ChatMessage],
        *,
        model: Optional[str] = None,
        temperature: Optional[float] = None,
        max_tokens: Optional[int] = None,
    ) -> str:
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

    def respond(
        self,
        messages: Iterable[ChatMessage],
        *,
        model: Optional[str] = None,
        temperature: Optional[float] = None,
        max_tokens: Optional[int] = None,
    ) -> str:
        if self.client is None:  # pragma: no cover - defensive guard
            raise RuntimeError("OpenAI client not initialised")
        # Use provided overrides or fall back to instance defaults
        final_model = model if model is not None else self.model
        final_temperature = temperature if temperature is not None else self.temperature
        final_max_tokens = max_tokens if max_tokens is not None else self.max_tokens
        
        params = {
            "model": final_model,
            "temperature": float(final_temperature),
            "messages": list(messages),
        }
        if final_max_tokens is not None:
            params["max_tokens"] = int(final_max_tokens)
        response = self.client.chat.completions.create(**params)
        choice = response.choices[0]
        message = choice.message
        if message is None or message.content is None:
            raise RuntimeError("OpenAI response did not include message content")
        return message.content
