#!/usr/bin/env node
/**
 * Cross-platform portable launcher & runtime bootstrapper for Hachimi Starry Catgirl Pet (`hachimi-pet`).
 * - Resolves plugin root dynamically (zero hardcoded user paths).
 * - Checks if Electron desktop overlay is already healthy on 127.0.0.1:17890.
 * - Locates existing Electron binary or caches `electron@33.3.1` once into `~/.hachimi-pet/runtime`.
 */
import fs from 'node:fs';
import http from 'node:http';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync, spawn } from 'node:child_process';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PLUGIN_ROOT = path.resolve(__dirname, '..');
const APP_DIR = path.join(PLUGIN_ROOT, 'app');
const PORT = Number(process.env.HACHIMI_PET_PORT || process.env.AGY_WHALE_PORT || 17890);
const RUNTIME_DIR = path.join(os.homedir(), '.hachimi-pet', 'runtime');
const FOREGROUND = process.argv.includes('--foreground');

function checkHealth(timeoutMs = 1200) {
  return new Promise((resolve) => {
    const req = http.get(`http://127.0.0.1:${PORT}/health`, (res) => {
      let data = '';
      res.on('data', (c) => { data += c; });
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          resolve(Boolean(j?.ok));
        } catch {
          resolve(false);
        }
      });
    });
    req.setTimeout(timeoutMs, () => {
      req.destroy();
      resolve(false);
    });
    req.on('error', () => resolve(false));
  });
}

function platformElectronCandidates(baseDir) {
  const dist = path.join(baseDir, 'node_modules', 'electron', 'dist');
  if (process.platform === 'win32') {
    return [path.join(dist, 'electron.exe')];
  }
  if (process.platform === 'darwin') {
    return [path.join(dist, 'Electron.app', 'Contents', 'MacOS', 'Electron')];
  }
  return [path.join(dist, 'electron')];
}

function findElectronBinary() {
  if (process.env.HACHIMI_ELECTRON_BIN && fs.existsSync(process.env.HACHIMI_ELECTRON_BIN)) {
    return process.env.HACHIMI_ELECTRON_BIN;
  }

  const searchBases = [
    RUNTIME_DIR,
    PLUGIN_ROOT,
    path.resolve(PLUGIN_ROOT, '..'),
    path.resolve(PLUGIN_ROOT, '..', 'agy-hachimi-pet'),
  ];

  for (const base of searchBases) {
    for (const cand of platformElectronCandidates(base)) {
      if (fs.existsSync(cand)) return cand;
    }
  }

  // Try system PATH `electron` (skip .cmd/.bat/.ps1 shims on Windows so spawn never throws EINVAL)
  try {
    const cmd = process.platform === 'win32' ? 'where' : 'which';
    const out = execFileSync(cmd, ['electron'], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim();
    const candidates = out.split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
    for (const cand of candidates) {
      if (process.platform === 'win32' && /\.(cmd|bat|ps1)$/i.test(cand)) continue;
      if (fs.existsSync(cand)) return cand;
    }
  } catch { /* ignore */ }

  return '';
}

function bootstrapElectronRuntime() {
  fs.mkdirSync(RUNTIME_DIR, { recursive: true });
  const runtimePkgPath = path.join(RUNTIME_DIR, 'package.json');
  if (!fs.existsSync(runtimePkgPath)) {
    fs.writeFileSync(
      runtimePkgPath,
      JSON.stringify({ name: 'hachimi-pet-runtime', version: '1.0.0', private: true }, null, 2) + '\n',
      'utf8'
    );
  }

  console.error(`[hachimi-pet] First-run setup: installing Electron runtime into ${RUNTIME_DIR} ...`);
  const npmArgs = ['install', '--no-save', '--no-audit', '--no-fund', 'electron@33.3.1'];
  if (process.platform === 'win32') {
    const comspec = process.env.ComSpec || 'cmd.exe';
    execFileSync(comspec, ['/d', '/s', '/c', 'npm.cmd', ...npmArgs], {
      cwd: RUNTIME_DIR,
      stdio: 'inherit',
      windowsHide: true,
    });
  } else {
    execFileSync('npm', npmArgs, {
      cwd: RUNTIME_DIR,
      stdio: 'inherit',
    });
  }
  const bin = findElectronBinary();
  if (!bin) {
    throw new Error(`Electron binary not found after install under ${RUNTIME_DIR}`);
  }
  return bin;
}

export async function launchPet() {
  if (!fs.existsSync(path.join(APP_DIR, 'electron', 'main.cjs'))) {
    throw new Error(`Missing app payload at ${APP_DIR}`);
  }

  if (await checkHealth()) {
    console.log(JSON.stringify({ ok: true, alreadyRunning: true, port: PORT }));
    return 0;
  }

  // Prefer lightweight native Windows WebView2 shell (~0.85 MB, zero Electron download) on win32
  const wv2HostScript = path.join(APP_DIR, 'electron', 'webview2-host.cjs');
  const wv2ShellExe = path.join(APP_DIR, 'shell-win32', 'HachimiPetShell.exe');
  const forceElectron = String(process.env.HACHIMI_RUNTIME || '').toLowerCase() === 'electron';
  const useWebView2 =
    process.platform === 'win32' &&
    !forceElectron &&
    fs.existsSync(wv2HostScript) &&
    fs.existsSync(wv2ShellExe);

  let runtimeBin = '';
  let runtimeArgs = [];
  let runtimeCwd = APP_DIR;

  if (useWebView2) {
    runtimeBin = process.execPath;
    runtimeArgs = [wv2HostScript];
  } else {
    runtimeBin = findElectronBinary();
    if (!runtimeBin) {
      runtimeBin = bootstrapElectronRuntime();
    }
    runtimeArgs = [APP_DIR, '--sidecar'];
  }

  if (FOREGROUND) {
    return await new Promise((resolve) => {
      const child = spawn(runtimeBin, runtimeArgs, {
        cwd: runtimeCwd,
        stdio: 'inherit',
        windowsHide: useWebView2,
      });
      child.on('exit', (code) => resolve(code ?? 0));
      child.on('error', () => resolve(1));
    });
  }

  let pid = null;
  if (process.platform === 'win32') {
    // On Windows, `Win32_Process.Create` escapes sandboxing Job Objects so `/hachimi` persists cleanly!
    try {
      const esc = (s) => String(s).replace(/'/g, "''");
      const argStr = runtimeArgs.map((a) => `"${esc(a)}"`).join(' ');
      const psCmd = useWebView2
        ? `$sp = New-CimInstance -ClassName Win32_ProcessStartup -Property @{ ShowWindow = [uint16]0 } -ClientOnly; Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = '"${esc(runtimeBin)}" ${argStr}'; CurrentDirectory = '${esc(runtimeCwd)}'; ProcessStartupInformation = $sp } | Select-Object -ExpandProperty ProcessId`
        : `Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = '"${esc(runtimeBin)}" ${argStr}'; CurrentDirectory = '${esc(runtimeCwd)}' } | Select-Object -ExpandProperty ProcessId`;
      const out = execFileSync('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', psCmd], {
        encoding: 'utf8',
        windowsHide: true,
      }).trim();
      pid = Number(out) || null;
    } catch { /* fallback to spawn */ }
  }

  if (!pid) {
    const child = spawn(runtimeBin, runtimeArgs, {
      cwd: runtimeCwd,
      detached: true,
      stdio: 'ignore',
      windowsHide: useWebView2,
    });
    child.unref();
    pid = child.pid;
  }

  for (let i = 0; i < 20; i++) {
    await new Promise((r) => setTimeout(r, 300));
    if (await checkHealth(600)) {
      console.log(JSON.stringify({ ok: true, launched: true, runtime: useWebView2 ? 'webview2' : 'electron', pid, port: PORT }));
      return 0;
    }
  }

  console.log(JSON.stringify({ ok: false, error: 'Timed out waiting for Hachimi Pet bridge health check' }));
  return 1;
}

if (import.meta.url === `file:///${process.argv[1].replace(/\\/g, '/')}` || process.argv[1]?.endsWith('launch-pet.mjs')) {
  const code = await launchPet();
  process.exit(code);
}
