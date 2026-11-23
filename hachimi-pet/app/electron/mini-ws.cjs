const crypto = require('node:crypto');
const { EventEmitter } = require('node:events');

const WS_GUID = '258EAFA5-E914-47DA-95CA-C5AB0DC85B11';

class MiniWsClient extends EventEmitter {
  constructor(socket) {
    super();
    this.socket = socket;
    this.readyState = 1; // OPEN
    this._buf = Buffer.alloc(0);

    socket.setNoDelay(true);
    socket.on('data', (chunk) => this._onData(chunk));
    socket.on('close', () => {
      this.readyState = 3;
      this.emit('close');
    });
    socket.on('error', () => {
      this.readyState = 3;
      try { socket.destroy(); } catch { /* ignore */ }
    });
  }

  send(data) {
    if (this.readyState !== 1 || this.socket.destroyed) return;
    const payload = Buffer.isBuffer(data) ? data : Buffer.from(String(data), 'utf8');
    const len = payload.length;
    let header;
    if (len < 126) {
      header = Buffer.alloc(2);
      header[0] = 0x81; // FIN + text frame
      header[1] = len;
    } else if (len < 65536) {
      header = Buffer.alloc(4);
      header[0] = 0x81;
      header[1] = 126;
      header.writeUInt16BE(len, 2);
    } else {
      header = Buffer.alloc(10);
      header[0] = 0x81;
      header[1] = 127;
      header.writeBigUInt64BE(BigInt(len), 2);
    }
    try {
      this.socket.write(Buffer.concat([header, payload]));
    } catch {
      this.readyState = 3;
    }
  }

  _sendControl(opcode, payload = Buffer.alloc(0)) {
    if (this.socket.destroyed) return;
    const header = Buffer.from([0x80 | opcode, payload.length & 0x7f]);
    try { this.socket.write(Buffer.concat([header, payload])); } catch { /* ignore */ }
  }

  close() {
    if (this.readyState === 3) return;
    this.readyState = 3;
    this._sendControl(0x08);
    try { this.socket.end(); } catch { /* ignore */ }
  }

  _onData(chunk) {
    this._buf = Buffer.concat([this._buf, chunk]);
    while (this._buf.length >= 2) {
      const b0 = this._buf[0];
      const b1 = this._buf[1];
      const opcode = b0 & 0x0f;
      const masked = (b1 & 0x80) !== 0;
      let payloadLen = b1 & 0x7f;
      let offset = 2;

      if (payloadLen === 126) {
        if (this._buf.length < 4) return;
        payloadLen = this._buf.readUInt16BE(2);
        offset = 4;
      } else if (payloadLen === 127) {
        if (this._buf.length < 10) return;
        payloadLen = Number(this._buf.readBigUInt64BE(2));
        offset = 10;
      }

      const maskLen = masked ? 4 : 0;
      if (this._buf.length < offset + maskLen + payloadLen) return;

      const maskKey = masked ? this._buf.subarray(offset, offset + 4) : null;
      offset += maskLen;
      const body = Buffer.from(this._buf.subarray(offset, offset + payloadLen));
      this._buf = this._buf.subarray(offset + payloadLen);

      if (masked && maskKey) {
        for (let i = 0; i < body.length; i++) {
          body[i] ^= maskKey[i & 3];
        }
      }

      if (opcode === 0x08) {
        this.close();
        return;
      }
      if (opcode === 0x09) {
        this._sendControl(0x0a, body.subarray(0, 125)); // Pong
        continue;
      }
      if (opcode === 0x01 || opcode === 0x02) {
        this.emit('message', body);
      }
    }
  }
}

/**
 * Zero-dependency RFC6455 WebSocketServer drop-in replacement for `ws`.
 */
class WebSocketServer extends EventEmitter {
  constructor({ server }) {
    super();
    this.clients = new Set();
    if (server) {
      server.on('upgrade', (req, socket) => this._handleUpgrade(req, socket));
    }
  }

  _handleUpgrade(req, socket) {
    const key = req.headers['sec-websocket-key'];
    if (!key) {
      socket.destroy();
      return;
    }
    const accept = crypto.createHash('sha1').update(key + WS_GUID).digest('base64');
    const headers = [
      'HTTP/1.1 101 Switching Protocols',
      'Upgrade: websocket',
      'Connection: Upgrade',
      `Sec-WebSocket-Accept: ${accept}`,
      '\r\n',
    ];
    socket.write(headers.join('\r\n'));

    const client = new MiniWsClient(socket);
    this.clients.add(client);
    client.on('close', () => this.clients.delete(client));
    this.emit('connection', client, req);
  }

  close() {
    for (const c of this.clients) {
      c.close();
    }
    this.clients.clear();
  }
}

module.exports = { WebSocketServer };
