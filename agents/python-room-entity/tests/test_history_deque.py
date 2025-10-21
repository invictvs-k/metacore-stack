"""Unit tests for RoomAgent history management using deque."""

from collections import deque
from unittest.mock import Mock

from python_room_entity.agent import RoomAgent
from python_room_entity.client import RoomClient
from python_room_entity.config import EntitySpec
from python_room_entity.openai_client import OpenAIResponder


def test_history_initialized_as_deque():
    """Test that history is initialized as a deque with correct maxlen."""
    mock_client = Mock(spec=RoomClient)
    mock_responder = Mock(spec=OpenAIResponder)
    entity = EntitySpec(
        entity_id="E-TEST",
        kind="test",
        display_name="Test",
        capabilities=[]
    )
    
    agent = RoomAgent(
        client=mock_client,
        entity=entity,
        room_id="test-room",
        responder=mock_responder,
        max_history=10
    )
    
    assert isinstance(agent._history, deque)
    assert agent._history.maxlen == 10
    assert len(agent._history) == 0


def test_history_respects_custom_max_history():
    """Test that custom max_history values are respected."""
    mock_client = Mock(spec=RoomClient)
    mock_responder = Mock(spec=OpenAIResponder)
    entity = EntitySpec(
        entity_id="E-TEST",
        kind="test",
        display_name="Test",
        capabilities=[]
    )
    
    agent = RoomAgent(
        client=mock_client,
        entity=entity,
        room_id="test-room",
        responder=mock_responder,
        max_history=5
    )
    
    assert agent._history.maxlen == 5


def test_append_history_adds_entries():
    """Test that _append_history correctly adds entries to the deque."""
    mock_client = Mock(spec=RoomClient)
    mock_responder = Mock(spec=OpenAIResponder)
    entity = EntitySpec(
        entity_id="E-TEST",
        kind="test",
        display_name="Test",
        capabilities=[]
    )
    
    agent = RoomAgent(
        client=mock_client,
        entity=entity,
        room_id="test-room",
        responder=mock_responder,
        max_history=5
    )
    
    # Add some messages
    agent._append_history({"role": "user", "content": "message 1"})
    agent._append_history({"role": "assistant", "content": "response 1"})
    agent._append_history({"role": "user", "content": "message 2"})
    
    assert len(agent._history) == 3
    assert agent._history[0]["content"] == "message 1"
    assert agent._history[1]["content"] == "response 1"
    assert agent._history[2]["content"] == "message 2"


def test_history_auto_trims_when_exceeding_maxlen():
    """Test that history automatically trims oldest entries when exceeding maxlen."""
    mock_client = Mock(spec=RoomClient)
    mock_responder = Mock(spec=OpenAIResponder)
    entity = EntitySpec(
        entity_id="E-TEST",
        kind="test",
        display_name="Test",
        capabilities=[]
    )
    
    agent = RoomAgent(
        client=mock_client,
        entity=entity,
        room_id="test-room",
        responder=mock_responder,
        max_history=3
    )
    
    # Add more messages than max_history
    for i in range(10):
        agent._append_history({"role": "user", "content": f"message {i}"})
    
    # Should only keep the last 3 messages
    assert len(agent._history) == 3
    assert agent._history[0]["content"] == "message 7"
    assert agent._history[1]["content"] == "message 8"
    assert agent._history[2]["content"] == "message 9"


def test_history_can_be_extended_to_list():
    """Test that history can be iterated and extended to a list (needed for _react method)."""
    mock_client = Mock(spec=RoomClient)
    mock_responder = Mock(spec=OpenAIResponder)
    entity = EntitySpec(
        entity_id="E-TEST",
        kind="test",
        display_name="Test",
        capabilities=[]
    )
    
    agent = RoomAgent(
        client=mock_client,
        entity=entity,
        room_id="test-room",
        responder=mock_responder,
        max_history=10
    )
    
    # Add some messages
    agent._append_history({"role": "user", "content": "message 1"})
    agent._append_history({"role": "assistant", "content": "response 1"})
    
    # Simulate what happens in _react method
    transcript = [{"role": "system", "content": "system prompt"}]
    transcript.extend(agent._history)
    
    assert len(transcript) == 3
    assert transcript[0]["role"] == "system"
    assert transcript[1]["content"] == "message 1"
    assert transcript[2]["content"] == "response 1"


def test_history_trimming_maintains_order():
    """Test that when history is trimmed, the order is maintained (FIFO)."""
    mock_client = Mock(spec=RoomClient)
    mock_responder = Mock(spec=OpenAIResponder)
    entity = EntitySpec(
        entity_id="E-TEST",
        kind="test",
        display_name="Test",
        capabilities=[]
    )
    
    agent = RoomAgent(
        client=mock_client,
        entity=entity,
        room_id="test-room",
        responder=mock_responder,
        max_history=5
    )
    
    # Add messages alternating between user and assistant
    for i in range(10):
        if i % 2 == 0:
            agent._append_history({"role": "user", "content": f"message {i}"})
        else:
            agent._append_history({"role": "assistant", "content": f"response {i}"})
    
    # Verify order is maintained
    assert len(agent._history) == 5
    messages = list(agent._history)
    for i, msg in enumerate(messages):
        expected_idx = i + 5  # Since we added 10 and max is 5, should have 5-9
        if expected_idx % 2 == 0:
            assert msg["role"] == "user"
            assert msg["content"] == f"message {expected_idx}"
        else:
            assert msg["role"] == "assistant"
            assert msg["content"] == f"response {expected_idx}"
