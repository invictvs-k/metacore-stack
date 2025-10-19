# Python Room Entity Client & Agent

This package provides a pure-Python implementation of a client and a pluggable
AI agent that can join a Metacore RoomServer room as an entity. It relies on the
SignalR protocol over WebSockets for transport and delegates high level
reasoning to the OpenAI API.

## Features

* **RoomClient** — typed helpers for connecting to a RoomServer hub, joining a
  room, publishing messages, and subscribing to `message` / `event` broadcasts.
* **RoomAgent** — orchestration loop that consumes room activity and asks an
  OpenAI model for the next action to perform (chat, command, or event).
* **OpenAI integration** — thin wrapper around the official `openai` package
  with dependency injection friendly interfaces for testing.
* **Pytest suite** — end-to-end tests that boot the .NET RoomServer in-process
  and verify that the agent can connect, advertise its capabilities, and drive
  port based command messages.

## Getting Started

```bash
cd agents/python-room-entity
python -m venv .venv
source .venv/bin/activate
pip install -e .[test]
pytest
```

To run the agent manually you will need a running RoomServer instance and a
valid OpenAI API key (via `OPENAI_API_KEY`). The `examples/` folder contains a
minimal script that instantiates the agent, joins a room, and relays actions
from the selected OpenAI model.

## Project Layout

```
python_room_entity/
  agent.py          # High level RoomAgent implementation
  client.py         # SignalR based RoomClient
  config.py         # Typed models (EntitySpec, Message payload helpers)
  openai_client.py  # OpenAI API wrappers and interfaces
```

Tests live under `tests/` and spin up a temporary RoomServer to exercise the
full stack.
