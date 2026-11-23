const { spawn } = require('node:child_process');
const { EventEmitter } = require('node:events');

/**
 * Manages headless / standalone / resumed Antigravity CLI (`agy`) turns for Codex-Mini mode
 * or when continuing from an idle session on the desktop pet.
 */
class AgyRunner extends EventEmitter {
  constructor() {
    super();
    /** @type {import('node:child_process').ChildProcess | null} */
    this.activeChild = null;
    this.activeRunId = null;
  }

  get isRunning() {
    return this.activeChild !== null;
  }

  /**
   * Run a prompt via `agy` native binary (`shell: false`) with `stream-json` output.
   *
   * @param {{
   *   prompt: string,
   *   ownerSessionId?: string,
   *   cliConversationId?: string | null,
   *   cwd?: string,
   *   model?: string,
   *   execMode?: 'accept-edits' | 'auto' | 'plan'
   * }} opts
   */
  runTurn(opts) {
    if (this.activeChild) {
      this.stop();
    }

    const runId = `run-${Date.now()}`;
    this.activeRunId = runId;
    const ownerSessionId = opts.ownerSessionId || null;

    const args = [
      '--output-format',
      'stream-json',
      '--mode',
      opts.execMode || 'auto',
    ];

    if (opts.cliConversationId) {
      args.push('--conversation', opts.cliConversationId);
    }
    if (opts.projectId && opts.projectId !== 'default-cli-project') {
      args.push('--project', opts.projectId);
    }
    if (opts.model && opts.model !== 'auto') {
      args.push('--model', opts.model);
    }
    // Always place -p <prompt> last; with shell: false, spaces/quotes/newlines are passed intact in argv
    args.push('-p', opts.prompt);

    const cwd = opts.cwd || process.cwd();
    const agyBin = process.platform === 'win32' ? 'agy.exe' : 'agy';

    this.emit('start', {
      runId,
      ownerSessionId,
      prompt: opts.prompt,
      cliConversationId: opts.cliConversationId || null,
      cwd,
    });

    const child = spawn(agyBin, args, {
      cwd,
      shell: false,
      stdio: ['ignore', 'pipe', 'pipe'],
      env: { ...process.env },
      windowsHide: true,
    });

    this.activeChild = child;
    let stdoutRemainder = '';
    let aggregatedText = '';
    let stderrText = '';
    let resolvedCliId = opts.cliConversationId || null;

    const onParsed = (info) => {
      if (info.cliConversationId) {
        resolvedCliId = info.cliConversationId;
      }
      if (info.textDelta) {
        aggregatedText += info.textDelta;
        this.emit('delta', {
          runId,
          ownerSessionId,
          cliConversationId: resolvedCliId,
          delta: info.textDelta,
          fullText: aggregatedText,
        });
      }
      if (typeof info.fullResult === 'string' && info.fullResult.length > 0) {
        aggregatedText = info.fullResult;
      }
      if (info.thinking) {
        this.emit('thinking', {
          runId,
          ownerSessionId,
          cliConversationId: resolvedCliId,
          thinking: info.thinking,
        });
      }
      if (info.toolName) {
        this.emit('tool', {
          runId,
          ownerSessionId,
          cliConversationId: resolvedCliId,
          toolName: info.toolName,
          toolArgs: info.toolArgs || {},
        });
      }
    };

    child.stdout.on('data', (chunk) => {
      stdoutRemainder += chunk.toString('utf8');
      const lines = stdoutRemainder.split(/\r?\n/);
      stdoutRemainder = lines.pop() ?? '';

      for (const rawLine of lines) {
        const line = rawLine.trim();
        if (!line) continue;
        this._handleStreamLine(line, onParsed);
      }
    });

    child.stderr.on('data', (chunk) => {
      const s = chunk.toString('utf8');
      stderrText += s;
      const m = s.match(/--conversation[=\s]+([0-9a-f-]{36})/i);
      if (m) resolvedCliId = m[1];
    });

    child.on('error', (err) => {
      if (this.activeChild === child) this.activeChild = null;
      this.emit('done', {
        runId,
        code: -1,
        ownerSessionId,
        cliConversationId: resolvedCliId,
        reply: '',
        stderr: err.message,
      });
    });

    child.on('close', (code) => {
      if (stdoutRemainder.trim()) {
        this._handleStreamLine(stdoutRemainder.trim(), onParsed);
      }
      if (this.activeChild === child) this.activeChild = null;
      this.emit('done', {
        runId,
        code: code ?? 0,
        ownerSessionId,
        cliConversationId: resolvedCliId,
        reply: aggregatedText.trim(),
        stderr: code !== 0 ? stderrText.trim() : '',
      });
    });

    return runId;
  }

  _handleStreamLine(line, cb) {
    try {
      const obj = JSON.parse(line);

      // Native agy 1.5.x stream-json schema: { event: "init" | "step_update" | "result", ... }
      if (obj.event === 'init') {
        if (obj.conversation_id) cb({ cliConversationId: obj.conversation_id });
        return;
      }

      if (obj.event === 'step_update' && obj.step_update) {
        const su = obj.step_update;
        if (su.conversation_id) cb({ cliConversationId: su.conversation_id });
        if (typeof su.text_delta === 'string' && su.text_delta.length > 0) {
          cb({ textDelta: su.text_delta });
        }
        if (typeof su.thinking_delta === 'string' && su.thinking_delta.length > 0) {
          cb({ thinking: su.thinking_delta });
        } else if (typeof su.thinking === 'string' && su.thinking.length > 0) {
          cb({ thinking: su.thinking });
        }
        if (
          su.step_type &&
          su.state === 'ACTIVE' &&
          !['user_input', 'agent_response', 'planner_response'].includes(su.step_type)
        ) {
          cb({ toolName: su.step_type, toolArgs: su.args || {} });
        }
        return;
      }

      if (obj.event === 'result' && obj.result) {
        const r = obj.result;
        if (r.conversation_id) cb({ cliConversationId: r.conversation_id });
        if (typeof r.response === 'string') cb({ fullResult: r.response });
        return;
      }

      // Generic fallback shapes
      const cid = obj.conversationId || obj.conversation_id || obj.session_id || null;
      if (cid) cb({ cliConversationId: cid });
      if (typeof obj.result === 'string') cb({ fullResult: obj.result });
      else if (typeof obj.delta === 'string') cb({ textDelta: obj.delta });
      else if (typeof obj.content === 'string') cb({ textDelta: obj.content });
    } catch {
      // Non-JSON plain output line
      cb({ textDelta: line + '\n' });
    }
  }

  stop() {
    if (!this.activeChild) return false;
    const child = this.activeChild;
    this.activeChild = null;
    try {
      if (process.platform === 'win32' && child.pid) {
        spawn('taskkill', ['/pid', String(child.pid), '/f', '/t'], { windowsHide: true });
      } else {
        child.kill('SIGTERM');
      }
    } catch { /* ignore */ }
    this.emit('stopped', { runId: this.activeRunId });
    return true;
  }
}

module.exports = { AgyRunner };
