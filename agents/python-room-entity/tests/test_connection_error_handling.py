"""Unit tests for connection error handling in RoomClient."""
from unittest.mock import MagicMock, patch

import pytest

from python_room_entity.client import RoomClient


@patch("python_room_entity.client.HubConnectionBuilder")
def test_connect_failure_preserves_clean_state(mock_builder_class: MagicMock) -> None:
    """Test that if hub.start() raises, the client remains in a disconnected state."""
    # Setup mock to fail on start()
    mock_builder = MagicMock()
    mock_builder_class.return_value = mock_builder
    mock_builder.with_url.return_value = mock_builder
    mock_builder.configure_logging.return_value = mock_builder
    mock_builder.with_automatic_reconnect.return_value = mock_builder
    
    mock_hub = MagicMock()
    mock_builder.build.return_value = mock_hub
    mock_hub.start.side_effect = RuntimeError("Connection failed")
    
    client = RoomClient("https://example.com/room")
    
    # Verify initial state
    assert client._hub is None
    assert client._disconnected.is_set()
    assert not client.is_connected
    
    # Attempt to connect (should fail)
    with pytest.raises(RuntimeError, match="Connection failed"):
        client.connect()
    
    # Verify that client remains in disconnected state
    assert client._hub is None  # Should not be assigned on failure
    assert client._disconnected.is_set()  # Should be re-set after failure
    assert not client.is_connected
    
    # Should be able to attempt connection again
    mock_hub.start.side_effect = None  # Clear the error for next attempt
    client.connect()  # Should not raise "already connected" error
    
    # Verify successful connection
    assert client._hub is mock_hub
    assert not client._disconnected.is_set()
    assert client.is_connected


@patch("python_room_entity.client.HubConnectionBuilder")
def test_connect_failure_during_start_allows_retry(mock_builder_class: MagicMock) -> None:
    """Test that connection can be retried after a failed start() attempt."""
    mock_builder = MagicMock()
    mock_builder_class.return_value = mock_builder
    mock_builder.with_url.return_value = mock_builder
    mock_builder.configure_logging.return_value = mock_builder
    mock_builder.with_automatic_reconnect.return_value = mock_builder
    
    # First hub instance will fail
    mock_hub_1 = MagicMock()
    mock_hub_1.start.side_effect = ConnectionError("Network error")
    
    # Second hub instance will succeed
    mock_hub_2 = MagicMock()
    mock_hub_2.start.side_effect = None
    
    mock_builder.build.side_effect = [mock_hub_1, mock_hub_2]
    
    client = RoomClient("https://example.com/room")
    
    # First attempt should fail
    with pytest.raises(ConnectionError, match="Network error"):
        client.connect()
    
    # Client should be in disconnected state
    assert client._hub is None
    assert client._disconnected.is_set()
    
    # Second attempt should succeed
    client.connect()
    assert client._hub is mock_hub_2
    assert not client._disconnected.is_set()
    assert client.is_connected


@patch("python_room_entity.client.HubConnectionBuilder")
def test_handlers_registered_before_start(mock_builder_class: MagicMock) -> None:
    """Test that handlers are registered on the hub before start() is called."""
    mock_builder = MagicMock()
    mock_builder_class.return_value = mock_builder
    mock_builder.with_url.return_value = mock_builder
    mock_builder.configure_logging.return_value = mock_builder
    mock_builder.with_automatic_reconnect.return_value = mock_builder
    
    mock_hub = MagicMock()
    mock_builder.build.return_value = mock_hub
    
    client = RoomClient("https://example.com/room")
    client.connect()
    
    # Verify handlers were registered before start was called
    assert mock_hub.on.call_count == 2
    mock_hub.on.assert_any_call("message", client._handle_message)
    mock_hub.on.assert_any_call("event", client._handle_event)
    assert mock_hub.on_close.call_count == 1
    mock_hub.on_close.assert_called_once_with(client._handle_close)
    
    # Verify start was called
    mock_hub.start.assert_called_once()
