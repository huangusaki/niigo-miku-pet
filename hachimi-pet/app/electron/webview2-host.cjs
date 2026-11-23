const fs = require('node:fs');
const path = require('node:path');
const { spawn } = require('node:child_process');
const { createBridgeServer, DEFAULT_PORT } = require('./bridge-server.cjs');

const APP_ROOT = path.resolve(__dirname, '..');

function resolveShellExe() {
  const candidates = [
    path.join(APP_ROOT, 'shell-win32', 'HachimiPetShell.exe'),
    path.join(APP_ROOT, 'shell-win32', 'bin', 'HachimiPetShell.exe'),
  ];
  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }
  return '';
}

function resolveTrayIcon() {
  const candidates = [
    path.join(APP_ROOT, 'dist', 'coopanion', 'whale', 'thumbs', 'astral.png'),
    path.join(APP_ROOT, 'public', 'coopanion', 'whale', 'thumbs', 'astral.png'),
  ];
  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }
  return '';
}

async function main() {
  let shellChild = null;
  let shuttingDown = false;

  const shutdown = (code = 0) => {
    if (shuttingDown) return;
    shuttingDown = true;
    try {
      if (shellChild && !shellChild.killed) {
        shellChild.kill();
      }
    } catch { /* ignore */ }
    try {
      bridge.close();
    } catch { /* ignore */ }
    process.exit(code);
  };

  const bridge = createBridgeServer({
    port: DEFAULT_PORT,
    onQuit: () => shutdown(0),
  });

  const info = await bridge.start();
  if (info?.reused) {
    // Another bridge instance is already serving on this port
    process.exit(0);
  }

  const shellExe = resolveShellExe();
  if (!shellExe) {
    console.error('[WebView2Host] Missing HachimiPetShell.exe under shell-win32/');
    shutdown(1);
    return;
  }

  const shellArgs = [
    `--port=${DEFAULT_PORT}`,
    `--url=http://127.0.0.1:${DEFAULT_PORT}/`,
    `--owner-pid=${process.pid}`,
  ];
  const iconPath = resolveTrayIcon();
  if (iconPath) {
    shellArgs.push(`--icon=${iconPath}`);
  }

  shellChild = spawn(shellExe, shellArgs, {
    cwd: path.dirname(shellExe),
    stdio: 'ignore',
    windowsHide: false,
  });

  shellChild.on('exit', () => {
    shutdown(0);
  });
  shellChild.on('error', (err) => {
    console.error('[WebView2Host] Failed to start shell:', err);
    shutdown(1);
  });

  process.on('SIGINT', () => shutdown(0));
  process.on('SIGTERM', () => shutdown(0));
}

main().catch((err) => {
  console.error('[WebView2Host] Fatal:', err);
  process.exit(1);
});
