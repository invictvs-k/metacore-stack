/**
 * Message builder for constructing RoomSpec and API payloads
 */

class MessageBuilder {
  /**
   * Build a complete RoomSpec
   */
  static buildRoomSpec(config, options = {}) {
    const {
      roomId = config.testRoom.roomId,
      version = config.testRoom.specVersion,
      entities = config.entities,
      artifacts = [],
      policies = config.policies,
    } = options;

    return {
      spec: {
        apiVersion: 'v1',
        kind: 'RoomSpec',
        metadata: {
          name: roomId.replace('room-', ''),
          version: version,
        },
        spec: {
          roomId: roomId,
          entities: entities.map(e => ({
            id: e.id,
            kind: e.kind,
            displayName: e.displayName,
            visibility: e.visibility,
            ownerUserId: e.ownerUserId || null,
            capabilities: e.capabilities || [],
            policy: e.policy || {
              allow_commands_from: 'none',
              sandbox_mode: true,
              env_whitelist: [],
              scopes: [],
            },
          })),
          artifacts: artifacts.map(a => ({
            name: a.name,
            type: a.type,
            workspace: a.workspace,
            tags: a.tags || [],
            // Note: seedFrom would be a file path in real usage
            // For test client, we'll handle content separately
          })),
          policies: policies,
        },
      },
    };
  }

  /**
   * Build entity spec for direct API calls
   */
  static buildEntitySpec(entity, roomId) {
    return {
      roomId: roomId,
      entityId: entity.id,
      kind: entity.kind,
      displayName: entity.displayName,
      visibility: entity.visibility,
      ownerUserId: entity.ownerUserId || null,
      capabilities: entity.capabilities || [],
      policy: entity.policy || {
        allow_commands_from: 'none',
        sandbox_mode: true,
        env_whitelist: [],
        scopes: [],
      },
    };
  }

  /**
   * Build artifact spec for multipart upload
   */
  static buildArtifactSpec(artifact) {
    return {
      name: artifact.name,
      type: artifact.type,
      workspace: artifact.workspace,
      tags: artifact.tags || [],
    };
  }

  /**
   * Build policies spec
   */
  static buildPoliciesSpec(policies) {
    return {
      dmVisibilityDefault: policies.dmVisibilityDefault || 'team',
      allowResourceCreation: policies.allowResourceCreation ?? false,
      maxArtifactsPerEntity: policies.maxArtifactsPerEntity || 100,
    };
  }

  /**
   * Build minimal RoomSpec (for testing minimal viable spec)
   */
  static buildMinimalSpec(roomId) {
    return {
      spec: {
        apiVersion: 'v1',
        kind: 'RoomSpec',
        metadata: {
          name: roomId.replace('room-', ''),
          version: 1,
        },
        spec: {
          roomId: roomId,
          entities: [],
          artifacts: [],
          policies: {
            dmVisibilityDefault: 'team',
          },
        },
      },
    };
  }

  /**
   * Build spec with only entities
   */
  static buildEntitiesOnlySpec(roomId, entities) {
    return {
      spec: {
        apiVersion: 'v1',
        kind: 'RoomSpec',
        metadata: {
          name: roomId.replace('room-', ''),
          version: 1,
        },
        spec: {
          roomId: roomId,
          entities: entities,
          artifacts: [],
          policies: {
            dmVisibilityDefault: 'team',
          },
        },
      },
    };
  }

  /**
   * Validate RoomSpec structure
   */
  static validateRoomSpec(spec) {
    const errors = [];

    if (!spec.spec) {
      errors.push('Missing top-level "spec" field');
      return { valid: false, errors };
    }

    const s = spec.spec;

    if (s.apiVersion !== 'v1') {
      errors.push('apiVersion must be "v1"');
    }

    if (s.kind !== 'RoomSpec') {
      errors.push('kind must be "RoomSpec"');
    }

    if (!s.metadata || !s.metadata.name) {
      errors.push('metadata.name is required');
    }

    if (!s.spec || !s.spec.roomId) {
      errors.push('spec.roomId is required');
    }

    if (s.spec && s.spec.roomId && !/^room-[A-Za-z0-9_-]{6,}$/.test(s.spec.roomId)) {
      errors.push('spec.roomId must match pattern: room-[A-Za-z0-9_-]{6,}');
    }

    if (s.spec && !Array.isArray(s.spec.entities)) {
      errors.push('spec.entities must be an array');
    }

    if (s.spec && !Array.isArray(s.spec.artifacts)) {
      errors.push('spec.artifacts must be an array');
    }

    return {
      valid: errors.length === 0,
      errors,
    };
  }
}

export default MessageBuilder;
