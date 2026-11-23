#!/usr/bin/env node
/**
 * Ultra-lightweight lifecycle hook forwarder for Antigravity -> Hachimi Pet Bridge.
 * Guarantees <75ms non-blocking fallback when the desktop pet is closed so Antigravity never lags.
 */
import http from 'node:http';

const eventName = process.argv[2] || 'Unknown';
const port = Number(process.env.HACHIMI_PET_PORT || process.env.AGY_WHALE_PORT || 17890);

function fallbackResponse(ev) {
  if (ev === 'PreToolUse') return { decision: 'ask' };
  if (ev === 'Stop') return { decision: 'stop' };
  return {};
}

async function readStdin() {
  const chunks = [];
  for await (const chunk of process.stdin) {
    chunks.push(chunk);
  }
  return Buffer.concat(chunks).toString('utf8');
}

function postHook(ev, rawBody) {
  return new Promise((resolve) => {
    let settled = false;
    const finish = (obj) => {
      if (settled) return;
      settled = true;
      resolve(obj);
    };

    const req = http.request(
      {
        hostname: '127.0.0.1',
        port,
        path: `/api/hook/${encodeURIComponent(ev)}`,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(rawBody),
        },
      },
      (res) => {
        let data = '';
        res.setEncoding('utf8');
        res.on('data', (c) => { data += c; });
        res.on('end', () => {
          try {
            finish(JSON.parse(data));
          } catch {
            finish(fallbackResponse(ev));
          }
        });
      }
    );

    const connectTimer = setTimeout(() => {
      req.destroy();
      finish(fallbackResponse(ev));
    }, 75);

    req.on('socket', (socket) => {
      socket.on('connect', () => {
        clearTimeout(connectTimer);
        req.setTimeout(3500, () => {
          req.destroy();
          finish(fallbackResponse(ev));
        });
      });
    });

    req.on('error', () => {
      clearTimeout(connectTimer);
      finish(fallbackResponse(ev));
    });

    req.write(rawBody);
    req.end();
  });
}

const raw = (await readStdin()) || '{}';
const out = await postHook(eventName, raw);
process.stdout.write(JSON.stringify(out) + '\n');
