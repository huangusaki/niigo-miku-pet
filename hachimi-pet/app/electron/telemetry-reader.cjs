const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');
const { execFile } = require('node:child_process');

const HOME = os.homedir();
const CONV_DB_DIRS = [
  path.join(HOME, '.gemini', 'antigravity', 'conversations'),
  path.join(HOME, '.gemini', 'antigravity-cli', 'conversations'),
];

const DEFAULT_CONTEXT_LIMIT = 256000;

function readVarint(buf, offset) {
  let shift = 0n;
  let val = 0n;
  let i = offset;
  while (i < buf.length) {
    const b = buf[i++];
    val |= BigInt(b & 0x7f) << shift;
    if ((b & 0x80) === 0) {
      return { value: Number(val), next: i };
    }
    shift += 7n;
    if (shift > 70n) break;
  }
  return { value: Number(val), next: i };
}

function parseProtoFields(buf) {
  /** @type {Record<number, Array<number | Buffer>>} */
  const fields = {};
  let i = 0;
  while (i < buf.length) {
    const tagRes = readVarint(buf, i);
    i = tagRes.next;
    const fieldNum = tagRes.value >>> 3;
    const wireType = tagRes.value & 0x07;
    if (fieldNum <= 0) break;

    if (wireType === 0) {
      const vRes = readVarint(buf, i);
      i = vRes.next;
      (fields[fieldNum] ||= []).push(vRes.value);
    } else if (wireType === 2) {
      const lenRes = readVarint(buf, i);
      i = lenRes.next;
      if (lenRes.value < 0 || i + lenRes.value > buf.length) break;
      (fields[fieldNum] ||= []).push(buf.subarray(i, i + lenRes.value));
      i += lenRes.value;
    } else if (wireType === 5) {
      i += 4;
    } else if (wireType === 1) {
      i += 8;
    } else {
      break;
    }
  }
  return fields;
}

function parseProtoTimestampOrDuration(buf) {
  if (!Buffer.isBuffer(buf)) return 0;
  const f = parseProtoFields(buf);
  const sec = typeof f[1]?.[0] === 'number' ? f[1][0] : 0;
  const nanos = typeof f[2]?.[0] === 'number' ? f[2][0] : 0;
  return sec + nanos / 1e9;
}

/**
 * Extract exact context window usage (`.1.9.10.1` used / `.1.9.10.4` limit) and durations from `gen_metadata.data`.
 */
function decodeGenMetadataBlob(genBuf) {
  if (!Buffer.isBuffer(genBuf) || genBuf.length === 0) return null;
  const root = parseProtoFields(genBuf);
  const m1Buf = root[1]?.[0];
  if (!Buffer.isBuffer(m1Buf)) return null;
  const m1 = parseProtoFields(m1Buf);

  let contextUsed = 0;
  let contextLimit = 0;

  const m9Buf = m1[9]?.[0];
  if (Buffer.isBuffer(m9Buf)) {
    const m9 = parseProtoFields(m9Buf);
    const m10Buf = m9[10]?.[0];
    if (Buffer.isBuffer(m10Buf)) {
      const m10 = parseProtoFields(m10Buf);
      if (typeof m10[1]?.[0] === 'number') contextUsed = m10[1][0];
      if (typeof m10[4]?.[0] === 'number') contextLimit = m10[4][0];
    }
  }

  const ttftDur = parseProtoTimestampOrDuration(m1[11]?.[0]);
  const streamDur = parseProtoTimestampOrDuration(m1[12]?.[0]);

  let outTokens = 0;
  const m4Buf = m1[4]?.[0];
  if (Buffer.isBuffer(m4Buf)) {
    const m4 = parseProtoFields(m4Buf);
    if (typeof m4[3]?.[0] === 'number') outTokens = m4[3][0];
  }

  if (!contextUsed && !contextLimit && !outTokens) return null;
  return { contextUsed, contextLimit, ttftDur, streamDur, outTokens };
}

/**
 * Decode `steps.metadata` + `gen_metadata.data` into exact Antigravity telemetry:
 * - `综合 X tok/s · 流式 Y · TTFT Zs · N tok`
 * - `used / limit (pct%)`
 */
function decodeStepMetadataBlob(metaBuf, genInfo = null) {
  if (!Buffer.isBuffer(metaBuf) || metaBuf.length === 0) return null;
  const f = parseProtoFields(metaBuf);
  const usageBuf = f[9]?.[0];
  if (!Buffer.isBuffer(usageBuf)) return null;

  const u = parseProtoFields(usageBuf);
  const inputTokens = typeof u[2]?.[0] === 'number' ? u[2][0] : 0;
  const outputTokens = typeof u[3]?.[0] === 'number' ? u[3][0] : 0;
  const cacheReadTokens = typeof u[5]?.[0] === 'number' ? u[5][0] : 0;
  const thinkingTokens = typeof u[9]?.[0] === 'number' ? u[9][0] : 0;
  const visibleOutTokens = typeof u[10]?.[0] === 'number' ? u[10][0] : Math.max(0, outputTokens - thinkingTokens);

  const tStart = parseProtoTimestampOrDuration(f[1]?.[0]);
  const tFirst = parseProtoTimestampOrDuration(f[6]?.[0]);
  const tEnd = parseProtoTimestampOrDuration(f[7]?.[0]);

  const totalSec = tEnd > tStart ? tEnd - tStart : ((genInfo?.ttftDur || 0) + (genInfo?.streamDur || 0));
  const ttftSec = tFirst > tStart ? tFirst - tStart : (genInfo?.ttftDur || 0);
  const streamSec = tEnd > tFirst && tFirst > 0 ? tEnd - tFirst : (genInfo?.streamDur || 0);

  const overallTokPerSec = totalSec > 0.05 && outputTokens > 0
    ? Number((outputTokens / totalSec).toFixed(1))
    : 0;

  const streamBasisTokens = visibleOutTokens > 0 ? visibleOutTokens : outputTokens;
  const streamTokPerSec = streamSec > 0.05 && streamBasisTokens > 0
    ? Number((streamBasisTokens / streamSec).toFixed(1))
    : overallTokPerSec;

  const rawContextSum = inputTokens + cacheReadTokens;
  const contextUsed = genInfo?.contextUsed > 0 ? genInfo.contextUsed : rawContextSum;
  const contextLimit = genInfo?.contextLimit > 0
    ? genInfo.contextLimit
    : (contextUsed > DEFAULT_CONTEXT_LIMIT ? 512000 : DEFAULT_CONTEXT_LIMIT);

  const contextPercent = contextUsed > 0 && contextLimit > 0
    ? Math.min(100, Number(((contextUsed / contextLimit) * 100).toFixed(1)))
    : 0;

  return {
    contextUsed,
    contextLimit,
    contextPercent,
    overallTokPerSec,
    streamTokPerSec,
    ttftSec: ttftSec > 0 ? Number(ttftSec.toFixed(2)) : 0,
    outputTokens,
    inputTokens,
    cacheReadTokens,
    compacted: false,
    updatedAt: Date.now(),
  };
}

function buildCompactedTelemetry(compactedSummaryTokens = 0, contextLimit = DEFAULT_CONTEXT_LIMIT, preCompactInputTokens = 0) {
  const limit = contextLimit > 0
    ? contextLimit
    : (preCompactInputTokens > DEFAULT_CONTEXT_LIMIT ? 512000 : DEFAULT_CONTEXT_LIMIT);
  const used = Math.max(0, Number(compactedSummaryTokens || 0));
  const pct = used > 0 && limit > 0
    ? Math.min(100, Number(((used / limit) * 100).toFixed(1)))
    : 0;
  return {
    contextUsed: used,
    contextLimit: limit,
    contextPercent: pct,
    overallTokPerSec: 0,
    streamTokPerSec: 0,
    ttftSec: 0,
    outputTokens: used,
    inputTokens: 0,
    cacheReadTokens: 0,
    compacted: true,
    updatedAt: Date.now(),
  };
}

function findConversationDbPath(conversationId) {
  if (!conversationId) return '';
  for (const dir of CONV_DB_DIRS) {
    const p = path.join(dir, `${conversationId}.db`);
    if (fs.existsSync(p)) return p;
  }
  return '';
}

/**
 * Query exact Context Window (`gen_metadata`) + Token Throughput (`steps.metadata`) from `<conversationId>.db`!
 * Uses a WAL/DB mtime cache so zero Python processes are spawned when the conversation DB hasn't changed.
 */
const dbTelemetryCache = new Map();

function getDbMtimeSig(dbPath) {
  try {
    const st = fs.statSync(dbPath);
    let walMtime = 0;
    let walSize = 0;
    const walPath = `${dbPath}-wal`;
    if (fs.existsSync(walPath)) {
      const ws = fs.statSync(walPath);
      walMtime = ws.mtimeMs;
      walSize = ws.size;
    }
    return `${st.mtimeMs}:${st.size}:${walMtime}:${walSize}`;
  } catch {
    return '';
  }
}

function readLatestDbTelemetry(conversationId) {
  return new Promise((resolve) => {
    const dbPath = findConversationDbPath(conversationId);
    if (!dbPath) {
      resolve(null);
      return;
    }

    const sig = getDbMtimeSig(dbPath);
    const cached = dbTelemetryCache.get(conversationId);
    if (sig && cached && cached.sig === sig) {
      resolve(cached.data);
      return;
    }
    const cacheAndResolve = (data) => {
      if (sig && data) {
        dbTelemetryCache.set(conversationId, { sig, data });
        if (dbTelemetryCache.size > 24) {
          const firstKey = dbTelemetryCache.keys().next().value;
          if (firstKey) dbTelemetryCache.delete(firstKey);
        }
      }
      resolve(data);
    };

    const pyScript = [
      'import sqlite3, sys, base64, json',
      'uri = "file:" + sys.argv[1].replace("\\\\", "/") + "?mode=ro"',
      'conn = sqlite3.connect(uri, uri=True, timeout=1.5)',
      'steps = conn.execute("SELECT idx, step_type, metadata FROM steps WHERE (metadata IS NOT NULL AND length(metadata) > 40) OR step_type = 23 ORDER BY idx DESC LIMIT 12").fetchall()',
      'gens = conn.execute("SELECT idx, data FROM gen_metadata WHERE data IS NOT NULL AND length(data) < 20000 ORDER BY idx DESC LIMIT 8").fetchall()',
      'out = {',
      '  "steps": [{"idx": r[0], "stepType": r[1], "b64": base64.b64encode(r[2]).decode("ascii") if r[2] else ""} for r in steps],',
      '  "gens": [{"idx": r[0], "b64": base64.b64encode(r[1]).decode("ascii")} for r in gens]',
      '}',
      'sys.stdout.write(json.dumps(out))',
    ].join('\n');

    const pyCandidates = process.platform === 'win32' ? ['python', 'py', 'python3'] : ['python3', 'python'];
    const tryExec = (idx) => {
      if (idx >= pyCandidates.length) {
        resolve(null);
        return;
      }
      execFile(
        pyCandidates[idx],
        ['-c', pyScript, dbPath],
        { windowsHide: true, timeout: 2500, maxBuffer: 1024 * 1024 },
        (err, stdout) => {
          if (err || !stdout) {
            tryExec(idx + 1);
            return;
          }
          try {
            const payload = JSON.parse(stdout);
            const parsedGens = [];
            for (const g of payload.gens || []) {
              const parsedGen = decodeGenMetadataBlob(Buffer.from(g.b64, 'base64'));
              if (parsedGen && parsedGen.contextLimit > 0) {
                parsedGens.push(parsedGen);
              }
            }
            const bestGen = parsedGens[0] || null;

            for (const r of payload.steps || []) {
              const buf = r.b64 ? Buffer.from(r.b64, 'base64') : null;
              const rawParsed = buf ? decodeStepMetadataBlob(buf, null) : null;

              // step_type === 23 is CHECKPOINT (/compact). If it is newer than any PLANNER_RESPONSE,
              // gen_metadata only holds stale pre-compaction contextUsed; report compacted summary tokens instead.
              if (Number(r.stepType) === 23) {
                const knownLimit = bestGen?.contextLimit || ((rawParsed?.inputTokens || 0) > DEFAULT_CONTEXT_LIMIT ? 512000 : DEFAULT_CONTEXT_LIMIT);
                cacheAndResolve(
                  buildCompactedTelemetry(rawParsed?.outputTokens || 0, knownLimit, rawParsed?.inputTokens || 0)
                );
                return;
              }

              if (!buf || !rawParsed) continue;
              let matchedGen =
                (rawParsed.outputTokens > 0
                  ? parsedGens.find((g) => g.outTokens === rawParsed.outputTokens)
                  : null) || bestGen;
              // Guard against pairing a fresh post-compaction step with a stale pre-compaction gen_metadata row
              if (
                matchedGen &&
                rawParsed.contextUsed > 0 &&
                matchedGen.contextUsed > 0 &&
                (rawParsed.contextUsed < matchedGen.contextUsed * 0.45 ||
                  matchedGen.contextUsed < rawParsed.contextUsed * 0.45)
              ) {
                matchedGen = {
                  ...matchedGen,
                  contextUsed: 0,
                };
              }

              const parsed = decodeStepMetadataBlob(buf, matchedGen);
              if (parsed && (parsed.contextUsed > 0 || parsed.outputTokens > 0)) {
                cacheAndResolve(parsed);
                return;
              }
            }

            if (bestGen) {
              const contextLimit = bestGen.contextLimit || DEFAULT_CONTEXT_LIMIT;
              const contextUsed = bestGen.contextUsed || 0;
              cacheAndResolve({
                contextUsed,
                contextLimit,
                contextPercent: contextUsed > 0 ? Math.min(100, Number(((contextUsed / contextLimit) * 100).toFixed(1))) : 0,
                overallTokPerSec: 0,
                streamTokPerSec: 0,
                ttftSec: bestGen.ttftDur ? Number(bestGen.ttftDur.toFixed(2)) : 0,
                outputTokens: bestGen.outTokens || 0,
                inputTokens: 0,
                cacheReadTokens: 0,
                updatedAt: Date.now(),
              });
              return;
            }
            resolve(null);
          } catch {
            resolve(null);
          }
        }
      );
    };
    tryExec(0);
  });
}

/**
 * Compute instant telemetry from a `transcript.jsonl` `PLANNER_RESPONSE` object (used immediately before DB refine).
 */
function buildTranscriptTelemetry(step, prevTelemetry = null, invocationStartMs = 0) {
  const inputTokens = Number(step.input_tokens || 0);
  const cacheReadTokens = Number(step.cache_read_tokens || 0);
  const outputTokens = Number(step.output_tokens || 0);
  const rawSum = inputTokens + cacheReadTokens;
  if (rawSum <= 0 && outputTokens <= 0) return prevTelemetry;

  const contextLimit = prevTelemetry?.contextLimit || (rawSum > DEFAULT_CONTEXT_LIMIT ? 512000 : DEFAULT_CONTEXT_LIMIT);
  const contextUsed = rawSum > 0 ? rawSum : (prevTelemetry?.contextUsed || 0);
  const contextPercent = contextUsed > 0
    ? Math.min(100, Number(((contextUsed / contextLimit) * 100).toFixed(1)))
    : (prevTelemetry?.contextPercent || 0);

  const createdMs = Date.parse(step.created_at || '') || invocationStartMs || 0;
  const elapsedSec = createdMs > 0 ? Math.max(0.2, (Date.now() - createdMs) / 1000) : 0;
  const overallTokPerSec =
    elapsedSec > 0.2 && elapsedSec < 600 && outputTokens > 0
      ? Number((outputTokens / elapsedSec).toFixed(1))
      : (prevTelemetry?.overallTokPerSec || 0);

  return {
    contextUsed,
    contextLimit,
    contextPercent,
    overallTokPerSec,
    streamTokPerSec: prevTelemetry?.streamTokPerSec || overallTokPerSec,
    ttftSec: prevTelemetry?.ttftSec || 0,
    outputTokens: outputTokens || prevTelemetry?.outputTokens || 0,
    inputTokens,
    cacheReadTokens,
    updatedAt: Date.now(),
  };
}

function readInitialTranscriptTelemetry(transcriptPath) {
  try {
    if (!transcriptPath || !fs.existsSync(transcriptPath)) return null;
    const stat = fs.statSync(transcriptPath);
    const tailLen = Math.min(stat.size, 131072);
    const buf = Buffer.alloc(tailLen);
    const fd = fs.openSync(transcriptPath, 'r');
    try {
      fs.readSync(fd, buf, 0, tailLen, stat.size - tailLen);
    } finally {
      fs.closeSync(fd);
    }
    const lines = buf.toString('utf8').split(/\r?\n/).filter((l) => l.trim().startsWith('{'));
    let checkpointStep = null;
    for (let i = lines.length - 1; i >= 0; i--) {
      try {
        const o = JSON.parse(lines[i]);
        const tp = String(o.type || '').toUpperCase();
        if (tp.includes('CHECKPOINT') && !checkpointStep) {
          checkpointStep = o;
          continue;
        }
        if (tp.includes('PLANNER_RESPONSE') && (o.input_tokens || o.cache_read_tokens || o.output_tokens)) {
          if (checkpointStep) {
            const priorRaw = Number(o.input_tokens || 0) + Number(o.cache_read_tokens || 0);
            const preInput = Number(checkpointStep.input_tokens || 0);
            const inferredLimit = (priorRaw > DEFAULT_CONTEXT_LIMIT || preInput > DEFAULT_CONTEXT_LIMIT) ? 512000 : DEFAULT_CONTEXT_LIMIT;
            return buildCompactedTelemetry(checkpointStep.output_tokens || 0, inferredLimit, preInput);
          }
          return buildTranscriptTelemetry(o, null, 0);
        }
      } catch { /* ignore */ }
    }
    if (checkpointStep) {
      return buildCompactedTelemetry(checkpointStep.output_tokens || 0, DEFAULT_CONTEXT_LIMIT, checkpointStep.input_tokens || 0);
    }
  } catch { /* ignore */ }
  return null;
}

module.exports = {
  DEFAULT_CONTEXT_LIMIT,
  decodeGenMetadataBlob,
  decodeStepMetadataBlob,
  readLatestDbTelemetry,
  buildTranscriptTelemetry,
  buildCompactedTelemetry,
  readInitialTranscriptTelemetry,
};
