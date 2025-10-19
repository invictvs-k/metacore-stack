"""Unit tests for OpenAI client wrapper."""

from unittest.mock import MagicMock, Mock

from python_room_entity.openai_client import OpenAIChatClient


def test_openai_client_uses_max_tokens_parameter():
    """Verify that the OpenAI client uses the correct 'max_tokens' parameter."""
    # Create a mock OpenAI client
    mock_client = MagicMock()
    mock_response = Mock()
    mock_response.choices = [Mock()]
    mock_response.choices[0].message = Mock()
    mock_response.choices[0].message.content = "Test response"
    mock_client.chat.completions.create.return_value = mock_response

    # Create the client with a specific max_tokens value
    client = OpenAIChatClient(
        model="gpt-4o-mini",
        temperature=0.5,
        max_tokens=500,
        client=mock_client,
    )

    # Make a request
    messages = [{"role": "user", "content": "Hello"}]
    response = client.respond(messages)

    # Verify the response
    assert response == "Test response"

    # Verify that the create method was called with the correct parameters
    mock_client.chat.completions.create.assert_called_once()
    call_kwargs = mock_client.chat.completions.create.call_args.kwargs

    # The key assertion: verify that 'max_tokens' is used, not 'max_output_tokens'
    assert "max_tokens" in call_kwargs
    assert call_kwargs["max_tokens"] == 500
    assert "max_output_tokens" not in call_kwargs


def test_openai_client_respects_kwarg_overrides():
    """Verify that kwargs can override default max_tokens value."""
    mock_client = MagicMock()
    mock_response = Mock()
    mock_response.choices = [Mock()]
    mock_response.choices[0].message = Mock()
    mock_response.choices[0].message.content = "Override response"
    mock_client.chat.completions.create.return_value = mock_response

    client = OpenAIChatClient(
        max_tokens=500,
        client=mock_client,
    )

    messages = [{"role": "user", "content": "Test"}]
    response = client.respond(messages, max_tokens=1000)

    assert response == "Override response"

    # Verify the overridden value was used
    call_kwargs = mock_client.chat.completions.create.call_args.kwargs
    assert call_kwargs["max_tokens"] == 1000


def test_openai_client_defaults_to_1000_max_tokens():
    """Verify that the default max_tokens value is 1000."""
    mock_client = MagicMock()
    mock_response = Mock()
    mock_response.choices = [Mock()]
    mock_response.choices[0].message = Mock()
    mock_response.choices[0].message.content = "Default response"
    mock_client.chat.completions.create.return_value = mock_response

    # Create client without specifying max_tokens
    client = OpenAIChatClient(client=mock_client)

    messages = [{"role": "user", "content": "Test"}]
    response = client.respond(messages)

    # Verify default value
    call_kwargs = mock_client.chat.completions.create.call_args.kwargs
    assert call_kwargs["max_tokens"] == 1_000
