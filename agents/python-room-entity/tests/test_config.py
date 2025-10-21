"""Tests for configuration models and validators."""

import pytest
from pydantic import ValidationError

from python_room_entity.config import ChatPayload, CommandPayload, EventPayload


class TestEventPayload:
    """Tests for EventPayload validation."""

    def test_valid_screaming_case_with_dot(self):
        """Test that valid SCREAMING_CASE with dots is accepted."""
        event = EventPayload(kind="ROOM.STATE", data={})
        assert event.kind == "ROOM.STATE"

    def test_valid_screaming_case_with_underscore(self):
        """Test that valid SCREAMING_CASE with underscores is accepted."""
        event = EventPayload(kind="ROOM_STATE", data={})
        assert event.kind == "ROOM_STATE"

    def test_valid_screaming_case_with_numbers(self):
        """Test that valid SCREAMING_CASE with numbers is accepted."""
        event = EventPayload(kind="ROOM123", data={})
        assert event.kind == "ROOM123"

    def test_valid_screaming_case_complex(self):
        """Test complex valid SCREAMING_CASE patterns."""
        event = EventPayload(kind="ABC.DEF_GHI.123", data={})
        assert event.kind == "ABC.DEF_GHI.123"

    def test_invalid_mixed_case(self):
        """Test that mixed case is rejected."""
        with pytest.raises(ValidationError) as exc_info:
            EventPayload(kind="Room.State", data={})
        assert "event kind must be SCREAMING_CASE" in str(exc_info.value)

    def test_invalid_lowercase(self):
        """Test that lowercase is rejected."""
        with pytest.raises(ValidationError) as exc_info:
            EventPayload(kind="room.state", data={})
        assert "event kind must be SCREAMING_CASE" in str(exc_info.value)

    def test_invalid_with_hyphen(self):
        """Test that strings with hyphens are rejected."""
        with pytest.raises(ValidationError) as exc_info:
            EventPayload(kind="TEST-CASE", data={})
        assert "event kind must be SCREAMING_CASE" in str(exc_info.value)

    def test_invalid_with_space(self):
        """Test that strings with spaces are rejected."""
        with pytest.raises(ValidationError) as exc_info:
            EventPayload(kind="TEST CASE", data={})
        assert "event kind must be SCREAMING_CASE" in str(exc_info.value)

    def test_invalid_with_special_char(self):
        """Test that strings with special characters are rejected."""
        with pytest.raises(ValidationError) as exc_info:
            EventPayload(kind="TEST@CASE", data={})
        assert "event kind must be SCREAMING_CASE" in str(exc_info.value)

    def test_invalid_empty_string(self):
        """Test that empty string is rejected."""
        with pytest.raises(ValidationError) as exc_info:
            EventPayload(kind="", data={})
        assert "event kind must be SCREAMING_CASE" in str(exc_info.value)


class TestCommandPayload:
    """Tests for CommandPayload validation."""

    def test_valid_target(self):
        """Test that valid EntityId is accepted."""
        cmd = CommandPayload(target="E-123456", inputs={})
        assert cmd.target == "E-123456"

    def test_invalid_target_without_prefix(self):
        """Test that EntityId without E- prefix is rejected."""
        with pytest.raises(ValidationError) as exc_info:
            CommandPayload(target="123456", inputs={})
        assert "target must be a valid EntityId starting with 'E-'" in str(exc_info.value)

    def test_invalid_empty_target(self):
        """Test that empty target is rejected."""
        with pytest.raises(ValidationError) as exc_info:
            CommandPayload(target="", inputs={})
        assert "target must be a valid EntityId starting with 'E-'" in str(exc_info.value)


class TestChatPayload:
    """Tests for ChatPayload validation."""

    def test_valid_text(self):
        """Test that valid chat text is accepted."""
        chat = ChatPayload(text="Hello, world!")
        assert chat.text == "Hello, world!"

    def test_invalid_empty_text(self):
        """Test that empty text is rejected."""
        with pytest.raises(ValidationError) as exc_info:
            ChatPayload(text="")
        assert "chat text must not be empty" in str(exc_info.value)

    def test_invalid_whitespace_only_text(self):
        """Test that whitespace-only text is rejected."""
        with pytest.raises(ValidationError) as exc_info:
            ChatPayload(text="   ")
        assert "chat text must not be empty" in str(exc_info.value)
