import queue
from typing import Dict

from python_room_entity.client import RoomClient
from python_room_entity.config import EntitySpec


def test_room_client_join_chat_and_command(room_server: Dict[str, object]) -> None:
    client = RoomClient(f"{room_server['base_url']}/room")
    messages: "queue.Queue[Dict[str, object]]" = queue.Queue()
    events: "queue.Queue[Dict[str, object]]" = queue.Queue()
    client.add_message_handler(lambda message: messages.put(message))
    client.add_event_handler(lambda event: events.put(event))

    client.connect()
    entity = EntitySpec(
        entity_id="E-PYCLIENT",
        kind="agent",
        display_name="Python Client",
        capabilities=["test.port"],
    )
    members = client.join("room-abcdef", entity)
    assert any(member["id"] == "E-PYCLIENT" for member in members)

    client.send_chat("hello from python")
    chat_message = messages.get(timeout=10)
    assert chat_message["type"].lower() == "chat"
    assert chat_message["payload"]["text"] == "hello from python"

    client.send_command(target="E-PYCLIENT", port="test.port", inputs={"value": 42})
    command_message = messages.get(timeout=10)
    assert command_message["type"].lower() == "command"
    assert command_message["payload"]["target"] == "E-PYCLIENT"
    assert command_message["payload"]["inputs"]["value"] == 42

    # Verify that a ROOM.STATE event is published at least once
    event = events.get(timeout=10)
    assert event["payload"]["kind"] == "ROOM.STATE"

    client.leave()
    client.disconnect()
