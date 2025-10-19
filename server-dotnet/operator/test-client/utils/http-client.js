/**
 * HTTP Client for interacting with RoomOperator and RoomServer
 */

import axios from 'axios';

class HttpClient {
  constructor(config, logger) {
    this.config = config;
    this.logger = logger;

    // Create axios instance for RoomOperator
    this.operatorClient = axios.create({
      baseURL: config.operator.baseUrl,
      timeout: config.operator.timeout,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Create axios instance for RoomServer (for validation)
    this.roomServerClient = axios.create({
      baseURL: config.roomServer.baseUrl,
      timeout: config.roomServer.timeout,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add auth token if provided
    if (config.auth.token) {
      this.roomServerClient.defaults.headers.common['Authorization'] = `Bearer ${config.auth.token}`;
    }

    // Add request/response interceptors for logging
    this._setupInterceptors();
  }

  _setupInterceptors() {
    // Operator client interceptors
    this.operatorClient.interceptors.request.use(
      (config) => {
        this.logger.debug(`→ ${(config.method?.toUpperCase() ?? 'UNKNOWN')} ${config.baseURL}${config.url}`);
        return config;
      },
      (error) => {
        this.logger.error('Request error', error.message);
        return Promise.reject(error);
      }
    );

    this.operatorClient.interceptors.response.use(
      (response) => {
        this.logger.debug(`← ${response.status} ${response.config.url}`, {
          duration: response.headers['x-response-time'] || 'N/A',
        });
        return response;
      },
      (error) => {
        if (error.response) {
          this.logger.debug(`← ${error.response.status} ${error.config.url}`, {
            error: error.response.data,
          });
        }
        return Promise.reject(error);
      }
    );

    // RoomServer client interceptors
    this.roomServerClient.interceptors.request.use(
      (config) => {
        this.logger.debug(`→ ${(config.method?.toUpperCase() ?? 'UNKNOWN')} ${config.baseURL}${config.url}`);
        return config;
      },
      (error) => Promise.reject(error)
    );

    this.roomServerClient.interceptors.response.use(
      (response) => {
        this.logger.debug(`← ${response.status} ${response.config.url}`);
        return response;
      },
      (error) => Promise.reject(error)
    );
  }

  // ============ RoomOperator API Methods ============

  /**
   * Apply a RoomSpec to the operator
   */
  async applySpec(spec, options = {}) {
    const headers = {};
    if (options.dryRun) {
      headers['X-Dry-Run'] = 'true';
    }
    if (options.confirm) {
      headers['X-Confirm'] = 'true';
    }

    try {
      const response = await this.operatorClient.post('/apply', spec, { headers });
      return { success: true, data: response.data };
    } catch (error) {
      return this._handleError(error, 'applySpec');
    }
  }

  /**
   * Get operator status
   */
  async getOperatorStatus() {
    try {
      const response = await this.operatorClient.get('/status');
      return { success: true, data: response.data };
    } catch (error) {
      return this._handleError(error, 'getOperatorStatus');
    }
  }

  /**
   * Get room status from operator
   */
  async getRoomStatus(roomId) {
    try {
      const response = await this.operatorClient.get(`/status/rooms/${roomId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return this._handleError(error, 'getRoomStatus');
    }
  }

  /**
   * Get operator health
   */
  async getOperatorHealth() {
    try {
      const response = await this.operatorClient.get('/health');
      return { success: true, data: response.data };
    } catch (error) {
      return this._handleError(error, 'getOperatorHealth');
    }
  }

  /**
   * Get operator audit logs
   */
  async getAuditLogs(count = 100, correlationId = null) {
    try {
      const params = { count };
      if (correlationId) {
        params.correlationId = correlationId;
      }
      const response = await this.operatorClient.get('/audit', { params });
      return { success: true, data: response.data };
    } catch (error) {
      return this._handleError(error, 'getAuditLogs');
    }
  }

  /**
   * Get operator metrics
   */
  async getMetrics() {
    try {
      const response = await this.operatorClient.get('/metrics');
      return { success: true, data: response.data };
    } catch (error) {
      return this._handleError(error, 'getMetrics');
    }
  }

  // ============ RoomServer API Methods (for validation) ============

  /**
   * Get room state from RoomServer
   */
  async getRoomState(roomId) {
    try {
      const response = await this.roomServerClient.get(`/room/${roomId}/state`);
      return { success: true, data: response.data };
    } catch (error) {
      return this._handleError(error, 'getRoomState');
    }
  }

  /**
   * Check if RoomServer is healthy
   */
  async checkRoomServerHealth() {
    try {
      const response = await this.roomServerClient.get('/health');
      return { success: true, data: response.data };
    } catch (error) {
      return this._handleError(error, 'checkRoomServerHealth');
    }
  }

  // ============ Helper Methods ============

  _handleError(error, operation) {
    if (error.response) {
      // Server responded with error status
      return {
        success: false,
        error: {
          operation,
          status: error.response.status,
          message: error.response.data?.message || error.response.statusText,
          data: error.response.data,
        },
      };
    } else if (error.request) {
      // Request made but no response
      return {
        success: false,
        error: {
          operation,
          message: 'No response from server',
          details: error.message,
        },
      };
    } else {
      // Error setting up request
      return {
        success: false,
        error: {
          operation,
          message: error.message,
        },
      };
    }
  }

  /**
   * Sleep for specified milliseconds
   */
  async sleep(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  /**
   * Retry an operation with exponential backoff
   */
  async retry(operation, maxRetries = 3, baseDelay = 1000) {
    let lastError;
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        return await operation();
      } catch (error) {
        lastError = error;
        
        if (attempt < maxRetries) {
          const delay = baseDelay * Math.pow(2, attempt - 1);
          this.logger.warn(`Attempt ${attempt} failed, retrying in ${delay}ms...`, error.message);
          await this.sleep(delay);
        }
      }
    }
    
    throw lastError;
  }
}

export default HttpClient;
