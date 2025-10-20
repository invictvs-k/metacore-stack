# Configuration Schemas

This directory contains JSON Schema definitions for configuration files.

## Purpose

Define and validate configuration file formats used throughout the system:
- Application configuration schemas
- Environment configuration schemas
- Build configuration schemas
- Deployment configuration schemas

## Why Schema Validation?

- **Catch errors early**: Invalid configurations are detected before deployment
- **Documentation**: Schemas serve as documentation for valid configuration options
- **IDE support**: Enable autocomplete and validation in editors
- **Consistency**: Ensure all environments use valid configurations

## Usage

1. Create a `.schema.json` file for each configuration type
2. Reference the schema in your configuration file using `$schema` property
3. Use validation tools (e.g., AJV) to validate configurations in CI/CD

## Example

```json
{
  "$schema": "./configs/schemas/dashboard-config.schema.json",
  "apiEndpoint": "http://localhost:40901",
  "port": 5173
}
```

## Related

See the `/schemas` directory at the repository root for domain object schemas (Room, Entity, Message, etc.).
This directory is specifically for **configuration file** schemas.

## Available Schemas

### API Contracts & Events

- **[integration-api.openapi.yaml](integration-api.openapi.yaml)** - OpenAPI specification for the Integration API (placeholder, to be filled in Prompt 3)
- **[sse.events.schema.json](sse.events.schema.json)** - JSON Schema for Server-Sent Events (SSE) payload structure (placeholder, to be filled in Prompt 3)
- **[commands.catalog.schema.json](commands.catalog.schema.json)** - JSON Schema for the commands catalog (placeholder, to be filled in Prompt 3)

## Status

Schema placeholders created. These will be fully populated in Prompt 3 with:
- Complete endpoint definitions
- Request/response models
- Event type specifications
- Command parameter schemas
- Validation rules
