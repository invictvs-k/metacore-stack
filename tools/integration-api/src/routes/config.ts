import { Router } from 'express';
import { getConfig, saveConfig, getConfigChecksum } from '../services/config.js';

export const configRouter = Router();

// GET /api/config - Get current configuration
configRouter.get('/', (req, res) => {
  try {
    const config = getConfig();
    res.json(config);
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});

// PUT /api/config - Update configuration
configRouter.put('/', async (req, res) => {
  try {
    const newConfig = req.body;
    await saveConfig(newConfig);
    res.json({ success: true, config: newConfig });
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});

// GET /api/config/version - Get configuration version/checksum
configRouter.get('/version', (req, res) => {
  try {
    const config = getConfig();
    const checksum = getConfigChecksum();
    res.json({
      version: config.version,
      checksum
    });
  } catch (error: any) {
    res.status(500).json({ error: error.message });
  }
});
