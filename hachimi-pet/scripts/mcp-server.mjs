#!/usr/bin/env node
/**
 * Zero-dependency MCP Stdio Server for Hachimi Starry Catgirl Desktop Pet (`hachimi-pet`).
 * Exposes `hachimi_pet_notify`.
 */
import http from 'node:http';
import readline from 'node:readline';

const PORT = Number(process.env.HACHIMI_PET_PORT || process.env.AGY_WHALE_PORT || 17890);

function postBridge(path, body, timeoutMs = 4000) {
  const payload = JSON.stringify(body || {});
  return new Promise((resolve, reject) => {
    const req = http.request(
      {
        hostname: '127.0.0.1',
        port: PORT,
        path,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(payload),
        },
      },
      (res) => {
        let data = '';
        res.setEncoding('utf8');
        res.on('data', (c) => { data += c; });
        res.on('end', () => {
          try {
            resolve(JSON.parse(data));
          } catch {
            resolve({ raw: data });
          }
        });
      }
    );
    req.setTimeout(timeoutMs, () => {
      req.destroy(new Error('Bridge request timed out'));
    });
    req.on('error', reject);
    req.write(payload);
    req.end();
  });
}

const TOOLS = [
  {
    name: 'hachimi_pet_notify',
    description: 'Make the Hachimi Starry Catgirl desktop pet show a speech bubble, facial expression, and motion animation on the user desktop.',
    inputSchema: {
      type: 'object',
      properties: {
        message: { type: 'string', description: 'Short message shown in the pet speech bubble.' },
        expression: {
          type: 'string',
          description: 'Optional expression: neutral, happy, wink, love, shy, surprised, angry, sad, sleepy, thinking, smug, pout, worried, determined, flustered, excited, confused.',
        },
        motion: {
          type: 'string',
          description: 'Optional motion: wave, jump, hop, nod, shake, spin, dance, bow, look.',
        },
      },
      required: ['message'],
    },
  },
];

function sendRpc(obj) {
  process.stdout.write(JSON.stringify(obj) + '\n');
}

async function handleRequest(msg) {
  if (!msg || typeof msg !== 'object') return;
  const { id, method, params } = msg;
  if (id === undefined || id === null) return;

  try {
    if (method === 'initialize') {
      sendRpc({
        jsonrpc: '2.0',
        id,
        result: {
          protocolVersion: params?.protocolVersion || '2024-11-05',
          capabilities: { tools: {} },
          serverInfo: { name: 'hachimi-pet', version: '1.0.0' },
        },
      });
      return;
    }

    if (method === 'ping') {
      sendRpc({ jsonrpc: '2.0', id, result: {} });
      return;
    }

    if (method === 'tools/list') {
      sendRpc({ jsonrpc: '2.0', id, result: { tools: TOOLS } });
      return;
    }

    if (method === 'tools/call') {
      const name = params?.name;
      const args = params?.arguments || {};

      if (name === 'hachimi_pet_notify') {
        try {
          await postBridge('/api/mcp/notify', args, 3000);
          sendRpc({
            jsonrpc: '2.0',
            id,
            result: {
              content: [{ type: 'text', text: 'Hachimi desktop pet notified successfully.' }],
            },
          });
        } catch (err) {
          sendRpc({
            jsonrpc: '2.0',
            id,
            result: {
              content: [{ type: 'text', text: `Desktop pet is not currently open (${err.message}). Run /hachimi to launch it.` }],
            },
          });
        }
        return;
      }

      sendRpc({
        jsonrpc: '2.0',
        id,
        error: { code: -32601, message: `Unknown tool: ${name}` },
      });
      return;
    }

    sendRpc({
      jsonrpc: '2.0',
      id,
      error: { code: -32601, message: `Method not found: ${method}` },
    });
  } catch (err) {
    sendRpc({
      jsonrpc: '2.0',
      id,
      error: { code: -32603, message: String(err?.message || err) },
    });
  }
}

const rl = readline.createInterface({ input: process.stdin, terminal: false });
rl.on('line', (line) => {
  const trimmed = line.trim();
  if (!trimmed || trimmed.startsWith('Content-Length:')) return;
  try {
    handleRequest(JSON.parse(trimmed));
  } catch {
    // Ignore malformed lines
  }
});
