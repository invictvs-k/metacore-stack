"""Unit tests for SSL verification configuration in RoomClient."""
from unittest.mock import MagicMock, patch

from python_room_entity.client import RoomClient


def test_ssl_verification_defaults_to_true_for_https() -> None:
    """Test that SSL verification is enabled by default for HTTPS URLs."""
    client = RoomClient("https://example.com/room")
    assert client.verify_ssl is True


def test_ssl_verification_defaults_to_false_for_http() -> None:
    """Test that SSL verification is disabled by default for HTTP URLs."""
    client = RoomClient("http://example.com/room")
    assert client.verify_ssl is False


def test_ssl_verification_can_be_explicitly_set() -> None:
    """Test that SSL verification can be explicitly configured."""
    # Explicitly disable for HTTPS
    client1 = RoomClient("https://example.com/room", verify_ssl=False)
    assert client1.verify_ssl is False
    # Explicitly enable for HTTP
    client2 = RoomClient("http://example.com/room", verify_ssl=True)
    assert client2.verify_ssl is True


@patch("python_room_entity.client.HubConnectionBuilder")
def test_ssl_verification_passed_to_hub_connection(mock_builder_class: MagicMock) -> None:
    """Test that the verify_ssl setting is passed to the HubConnectionBuilder."""
    mock_builder = MagicMock()
    mock_builder_class.return_value = mock_builder
    mock_builder.with_url.return_value = mock_builder
    mock_builder.configure_logging.return_value = mock_builder
    mock_builder.with_automatic_reconnect.return_value = mock_builder
    mock_builder.build.return_value = MagicMock()
    # Test with HTTPS (should default to True)
    client_https = RoomClient("https://example.com/room")
    client_https.connect()
    # Verify the with_url was called with verify_ssl=True
    call_args = mock_builder.with_url.call_args
    assert call_args[0][0] == "https://example.com/room"
    assert call_args[1]["options"]["verify_ssl"] is True
    
    # Reset mock
    mock_builder.reset_mock()
    mock_builder.with_url.return_value = mock_builder
    mock_builder.configure_logging.return_value = mock_builder
    mock_builder.with_automatic_reconnect.return_value = mock_builder
    mock_builder.build.return_value = MagicMock()
    
    # Test with HTTP (should default to False)
    client_http = RoomClient("http://example.com/room")
    client_http.connect()
    
    # Verify the with_url was called with verify_ssl=False
    call_args = mock_builder.with_url.call_args
    assert call_args[0][0] == "http://example.com/room"
    assert call_args[1]["options"]["verify_ssl"] is False
