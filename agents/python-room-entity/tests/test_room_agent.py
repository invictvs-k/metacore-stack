import json
import queue
from typing import Dict, Iterable, List

from python_room_entity.agent import RoomAgent
from python_room_entity.client import RoomClient
from python_room_entity.config import EntitySpec
from python_room_entity.openai_client import ChatMessage, OpenAIResponder


class MockResponder(OpenAIResponder):
    def __init__(self) -> None:
        self.calls: List[List[ChatMessage]] = []

    def respond(self, messages: Iterable[ChatMessage], **kwargs: object) -> str:
        transcript = list(messages)
        self.calls.append(transcript)
        last_user = next((m for m in reversed(transcript) if m["role"] == "user"), None)
        target = "E-HUMAN"
        if last_user:
            try:
                payload = json.loads(last_user["content"])
                target = payload.get("from", target)
            except json.JSONDecodeError:
                pass
        return json.dumps({
            "action": "command",
            "target": target,
            "port": "test.port",
            "inputs": {"ack": True},
        })


def test_room_agent_sends_command(room_server: Dict[str, object]) -> None:
    room_id = "room-agent01"
    responder = MockResponder()
    agent_client = RoomClient(f"{room_server['base_url']}/room")
    agent_entity = EntitySpec(
        entity_id="E-AGENT",
        kind="agent",
        display_name="Test Agent",
        capabilities=["test.port"],
    )
    agent = RoomAgent(
        client=agent_client,
        entity=agent_entity,
        room_id=room_id,
        responder=responder,
    )
    agent_members = agent.start()
    assert any(member["id"] == "E-AGENT" for member in agent_members)

    human_client = RoomClient(f"{room_server['base_url']}/room")
    human_messages: "queue.Queue[Dict[str, object]]" = queue.Queue()
    human_client.add_message_handler(lambda message: human_messages.put(message))
    human_client.connect()
    human_entity = EntitySpec(
        entity_id="E-HUMAN",
        kind="human",
        display_name="Human",
        capabilities=[],
    )
    human_client.join(room_id, human_entity)

    human_client.send_chat("agent, please trigger test.port")

    # Drain messages until a 'command' is received
    command = None
    for _ in range(20):  # Try up to 20 times (total timeout up to 20s)
        msg = human_messages.get(timeout=1)
        if msg["type"].lower() == "command":
            command = msg
            break
    assert command is not None, "Did not receive a 'command' message"
    assert command["payload"]["target"] == "E-HUMAN"
    assert command["payload"]["inputs"]["ack"] is True

    agent.stop()
    human_client.leave()
    human_client.disconnect()
