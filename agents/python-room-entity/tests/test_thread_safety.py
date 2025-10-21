"""Unit tests for thread safety in RoomClient."""
import threading
import time
from unittest.mock import MagicMock, patch

from python_room_entity.client import RoomClient


@patch("python_room_entity.client.HubConnectionBuilder")
def test_is_connected_thread_safe_with_handle_close(mock_builder_class: MagicMock) -> None:
    """Test that is_connected is thread-safe when _handle_close is called concurrently."""
    # Setup mock
    mock_builder = MagicMock()
    mock_builder_class.return_value = mock_builder
    mock_builder.with_url.return_value = mock_builder
    mock_builder.configure_logging.return_value = mock_builder
    mock_builder.with_automatic_reconnect.return_value = mock_builder
    
    mock_hub = MagicMock()
    mock_builder.build.return_value = mock_hub
    
    client = RoomClient("https://example.com/room")
    client.connect()
    
    # Verify connected state
    assert client.is_connected
    
    # Track any exceptions raised in threads
    exceptions = []
    results = []
    
    def check_connection_repeatedly():
        """Repeatedly check is_connected to detect race conditions."""
        try:
            for _ in range(100):
                # This should never raise an exception, even during concurrent _handle_close
                is_conn = client.is_connected
                results.append(is_conn)
                time.sleep(0.001)  # Small delay to increase chance of race
        except Exception as e:
            exceptions.append(e)
    
    def trigger_close():
        """Trigger _handle_close to simulate disconnect."""
        try:
            time.sleep(0.01)  # Let reader thread start first
            client._handle_close()
        except Exception as e:
            exceptions.append(e)
    
    # Start threads
    reader_thread = threading.Thread(target=check_connection_repeatedly)
    closer_thread = threading.Thread(target=trigger_close)
    
    reader_thread.start()
    closer_thread.start()
    
    reader_thread.join()
    closer_thread.join()
    
    # No exceptions should have been raised
    assert not exceptions, f"Unexpected exceptions: {exceptions}"
    
    # After _handle_close, is_connected should return False
    assert not client.is_connected
    
    # Results should contain both True and False values
    # (True before close, False after close)
    assert True in results
    assert False in results


@patch("python_room_entity.client.HubConnectionBuilder")
def test_is_connected_thread_safe_with_disconnect(mock_builder_class: MagicMock) -> None:
    """Test that is_connected is thread-safe when disconnect is called concurrently."""
    # Setup mock
    mock_builder = MagicMock()
    mock_builder_class.return_value = mock_builder
    mock_builder.with_url.return_value = mock_builder
    mock_builder.configure_logging.return_value = mock_builder
    mock_builder.with_automatic_reconnect.return_value = mock_builder
    
    mock_hub = MagicMock()
    mock_builder.build.return_value = mock_hub
    
    client = RoomClient("https://example.com/room")
    client.connect()
    
    # Verify connected state
    assert client.is_connected
    
    # Track any exceptions raised in threads
    exceptions = []
    results = []
    
    def check_connection_repeatedly():
        """Repeatedly check is_connected to detect race conditions."""
        try:
            for _ in range(100):
                # This should never raise an exception, even during concurrent disconnect
                is_conn = client.is_connected
                results.append(is_conn)
                time.sleep(0.001)  # Small delay to increase chance of race
        except Exception as e:
            exceptions.append(e)
    
    def trigger_disconnect():
        """Trigger disconnect to simulate graceful shutdown."""
        try:
            time.sleep(0.01)  # Let reader thread start first
            client.disconnect()
        except Exception as e:
            exceptions.append(e)
    
    # Start threads
    reader_thread = threading.Thread(target=check_connection_repeatedly)
    disconnector_thread = threading.Thread(target=trigger_disconnect)
    
    reader_thread.start()
    disconnector_thread.start()
    
    reader_thread.join()
    disconnector_thread.join()
    
    # No exceptions should have been raised
    assert not exceptions, f"Unexpected exceptions: {exceptions}"
    
    # After disconnect, is_connected should return False
    assert not client.is_connected
    
    # Results should contain both True and False values
    # (True before disconnect, False after disconnect)
    assert True in results
    assert False in results


@patch("python_room_entity.client.HubConnectionBuilder")
def test_connect_and_disconnect_are_thread_safe(mock_builder_class: MagicMock) -> None:
    """Test that concurrent calls to connect/disconnect don't cause race conditions."""
    # Setup mock
    mock_builder = MagicMock()
    mock_builder_class.return_value = mock_builder
    mock_builder.with_url.return_value = mock_builder
    mock_builder.configure_logging.return_value = mock_builder
    mock_builder.with_automatic_reconnect.return_value = mock_builder
    
    mock_hub_1 = MagicMock()
    mock_hub_2 = MagicMock()
    mock_builder.build.side_effect = [mock_hub_1, mock_hub_2]
    
    client = RoomClient("https://example.com/room")
    
    # Track any exceptions raised in threads
    exceptions = []
    
    def connect_and_disconnect():
        """Connect then disconnect."""
        try:
            client.connect()
            time.sleep(0.01)
            client.disconnect()
        except Exception as e:
            exceptions.append(e)
    
    def check_connection():
        """Check connection state repeatedly."""
        try:
            for _ in range(50):
                client.is_connected
                time.sleep(0.001)
        except Exception as e:
            exceptions.append(e)
    
    # Start threads
    connector_thread = threading.Thread(target=connect_and_disconnect)
    checker_thread = threading.Thread(target=check_connection)
    
    connector_thread.start()
    checker_thread.start()
    
    connector_thread.join()
    checker_thread.join()
    
    # No exceptions should have been raised
    assert not exceptions, f"Unexpected exceptions: {exceptions}"
    
    # Final state should be disconnected
    assert not client.is_connected
