# Interfaces

This directory contains machine-readable interface contracts and API specifications.

## Purpose

Store formal API contracts, OpenAPI specifications, GraphQL schemas, and other interface definitions that define the boundaries between system components.

## Usage

- Place OpenAPI/Swagger specifications here
- Store GraphQL schemas
- Document service contracts
- Include TypeScript interface definitions or similar type contracts
- Add protocol buffer definitions if applicable

## Convention

Files should be named descriptively:
- `{component-name}.openapi.yaml` for OpenAPI specs
- `{component-name}.graphql` for GraphQL schemas
- `{component-name}.proto` for protocol buffers
- `{component-name}-contract.ts` for TypeScript contracts

## Available Specifications

Machine-readable API contracts are being consolidated. Current schema placeholders:

- **Integration API** - See [configs/schemas/integration-api.openapi.yaml](../../configs/schemas/integration-api.openapi.yaml) (OpenAPI specification, to be completed in Prompt 3)
- **SSE Events** - See [configs/schemas/sse.events.schema.json](../../configs/schemas/sse.events.schema.json) (JSON Schema for event payloads, to be completed in Prompt 3)
- **Commands Catalog** - See [configs/schemas/commands.catalog.schema.json](../../configs/schemas/commands.catalog.schema.json) (JSON Schema for command definitions, to be completed in Prompt 3)

## Status

Schema placeholders created. Will be populated in Prompt 3 with complete specifications.
