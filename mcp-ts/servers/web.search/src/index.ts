import { WebSocketServer } from 'ws';
import { z } from 'zod';

const port = Number(process.env.PORT ?? 8081);
const server = new WebSocketServer({ port });

const searchRequestSchema = z.object({
  type: z.literal('search'),
  query: z.string()
});

server.on('connection', socket => {
  socket.send(JSON.stringify({ type: 'ready', message: 'web.search MCP server placeholder' }));

  socket.on('message', raw => {
    try {
      const payload = JSON.parse(String(raw));
      const request = searchRequestSchema.parse(payload);
      socket.send(
        JSON.stringify({
          type: 'result',
          query: request.query,
          results: []
        })
      );
    } catch (error) {
      socket.send(JSON.stringify({ type: 'error', message: (error as Error).message }));
    }
  });
});

console.log(`[web.search] listening on ws://localhost:${port}`);
