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

## Status

Currently a placeholder. Add schema definitions as configuration formats are formalized.
