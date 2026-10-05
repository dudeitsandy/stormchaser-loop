#!/usr/bin/env node
// WebGL frame-time capture for the Unity build. Emits observations only; budgets are applied by the reader.
// Usage: node tools/perf/webgl-frametime.mjs [options]   (see --help or tools/perf/README.md)
import { createServer } from 'node:http';
import { readFile, writeFile, mkdir, readdir, stat, cp, rm, access } from 'node:fs/promises';
import { resolve, join, extname, relative, sep, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import os from 'node:os';
import { chromium } from 'playwright-core';

const here = dirname(fileURLToPath(import.meta.url));
const repo = resolve(here, '..', '..');
const exec = promisify(execFile);
const sleep = ms => new Promise(done => setTimeout(done, ms));
const round = (v, d = 2) => (v == null || Number.isNaN(v) ? null : Number(v.toFixed(d)));

const HELP = `WebGL frame-time capture (observations only, no verdicts)

  --seconds N          run-phase length in seconds (default 190; physProbe reports at ~185 s)
  --title-seconds N    title / attract-mode recording before the run (default 20, 0 = skip)
  --seed N             storm seed, sent as ?seed=N (default 554)
  --no-phys            do not add &physProbe=1 (a normal run then ends at 90 s)
  --build DIR          build folder to snapshot and serve (default builds/webgl)
  --url URL            measure an already-served build instead (no snapshot)
  --version V          version label when using --url (default: read from the page)
  --out DIR            report folder (default production/qa/perf)
  --width N --height N viewport and canvas size (default 1280 x 800)
  --screenshot-every N seconds between screenshots (default 30, 0 = off)
  --headed             show the browser window (default headless)
  --force              snapshot even if the build changed in the last 60 s
`;

function parseArgs(argv) {
  const opts = {
    seconds: 190, titleSeconds: 20, seed: 554, phys: true, build: 'builds/webgl', url: null, version: null,
    out: 'production/qa/perf', width: 1280, height: 800, screenshotEvery: 30, headed: false, force: false,
  };
  for (let i = 0; i < argv.length; i++) {
    const flag = argv[i];
    const value = () => {
      const v = argv[++i];
      if (v === undefined) throw new Error(`${flag} needs a value`);
      return v;
    };
    const number = () => {
      const v = Number(value());
      if (!Number.isFinite(v) || v < 0) throw new Error(`${flag} needs a non-negative number`);
      return v;
    };
    switch (flag) {
      case '--seconds': opts.seconds = number(); break;
      case '--title-seconds': opts.titleSeconds = number(); break;
      case '--seed': opts.seed = Math.trunc(number()); break;
      case '--no-phys': opts.phys = false; break;
      case '--build': opts.build = value(); break;
      case '--url': opts.url = value(); break;
      case '--version': opts.version = value(); break;
      case '--out': opts.out = value(); break;
      case '--width': opts.width = Math.trunc(number()); break;
      case '--height': opts.height = Math.trunc(number()); break;
      case '--screenshot-every': opts.screenshotEvery = number(); break;
      case '--headed': opts.headed = true; break;
      case '--force': opts.force = true; break;
      case '--help': case '-h': opts.help = true; break;
      default: throw new Error(`Unknown option ${flag} (try --help)`);
    }
  }
  return opts;
}

// ---------- machine observations ----------

async function unityProcesses() {
  if (process.platform !== 'win32') return null;
  try {
    const { stdout } = await exec('tasklist', ['/FI', 'IMAGENAME eq Unity.exe', '/FO', 'CSV', '/NH']);
    return stdout.split(/\r?\n/).filter(line => line.startsWith('"Unity.exe"')).map(line => {
      const cells = line.slice(1, -1).split('","');
      return { pid: Number(cells[1]), memory: cells[4] };
    });
  } catch {
    return null;
  }
}

const cpuTimes = () => os.cpus().map(c => c.times);
function busyPercent(before, after) {
  let idle = 0, total = 0;
  after.forEach((t, i) => {
    for (const key of Object.keys(t)) total += t[key] - before[i][key];
    idle += t.idle - before[i].idle;
  });
  return total > 0 ? 100 * (1 - idle / total) : null;
}

class CpuSampler {
  constructor() { this.samples = []; this.last = cpuTimes(); }
  sample() {
    const now = cpuTimes();
    const busy = busyPercent(this.last, now);
    this.last = now;
    if (busy != null) this.samples.push(busy);
  }
  summary() {
    if (!this.samples.length) return null;
    const mean = this.samples.reduce((a, b) => a + b, 0) / this.samples.length;
    return { meanPct: round(mean, 1), maxPct: round(Math.max(...this.samples), 1), samples: this.samples.length };
  }
}

// ---------- build snapshot ----------

async function listFiles(dir, base = dir) {
  const out = [];
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...await listFiles(full, base));
    else {
      const info = await stat(full);
      out.push({ path: relative(base, full).split(sep).join('/'), size: info.size, mtimeMs: Math.round(info.mtimeMs) });
    }
  }
  return out.sort((a, b) => a.path.localeCompare(b.path));
}

const sameFiles = (a, b, withTimes = true) => a.length === b.length && a.every((f, i) =>
  f.path === b[i].path && f.size === b[i].size && (!withTimes || f.mtimeMs === b[i].mtimeMs));

async function prepareBuild(opts, log) {
  const source = resolve(repo, opts.build);
  const html = await readFile(join(source, 'index.html'), 'utf8');
  const version = html.match(/productVersion:\s*"([^"]+)"/)?.[1] ?? 'unknown';
  for (const name of html.matchAll(/buildUrl \+ "\/([^"]+)"/g)) {
    await access(join(source, 'Build', name[1])).catch(() => {
      throw new Error(`Build is incomplete: Build/${name[1]} is referenced by index.html but missing.`);
    });
  }

  const snapshotsRoot = resolve(repo, 'builds', 'perf-snapshot');
  if (source.startsWith(snapshotsRoot + sep)) {
    log(`Serving existing snapshot ${relative(repo, source)} as is.`);
    return {
      version, source: relative(repo, source).split(sep).join('/'), snapshot: relative(repo, source).split(sep).join('/'),
      reusedSnapshot: true, forced: false, unityProcessesAtSnapshot: await unityProcesses(), root: source,
    };
  }

  const first = await listFiles(source);
  await sleep(5000);
  const second = await listFiles(source);
  const stable = sameFiles(first, second);
  const newestAgeS = (Date.now() - Math.max(...second.map(f => f.mtimeMs))) / 1000;
  const unity = await unityProcesses();

  const snapshotDir = resolve(repo, 'builds', 'perf-snapshot', version);
  const marker = join(snapshotDir, '.snapshot.json');
  let existing = null;
  try { existing = JSON.parse(await readFile(marker, 'utf8')); } catch { /* no snapshot yet */ }

  const info = {
    version, source: relative(repo, source).split(sep).join('/'), snapshot: relative(repo, snapshotDir).split(sep).join('/'),
    sourceNewestFileAgeS: round(newestAgeS, 0), sourceStableOver5s: stable, unityProcessesAtSnapshot: unity,
    reusedSnapshot: false, forced: false,
  };
  if (existing && sameFiles(existing.files, second)) {
    info.reusedSnapshot = true;
    info.snapshotCopiedAt = existing.copiedAt;
    log(`Reusing snapshot ${info.snapshot} (matches ${info.source}).`);
    return { ...info, root: snapshotDir };
  }
  if (!stable || newestAgeS < 60) {
    const why = !stable ? 'files changed during a 5 s check' : `newest file is only ${Math.round(newestAgeS)} s old`;
    if (!opts.force) throw new Error(`${info.source} looks mid-build (${why}). Wait for the build to finish, or pass --force.`);
    info.forced = true;
    log(`Warning: ${why}; snapshotting anyway (--force).`);
  }
  log(`Copying ${info.source} to ${info.snapshot} ...`);
  await rm(snapshotDir, { recursive: true, force: true });
  await cp(source, snapshotDir, { recursive: true, preserveTimestamps: true });
  const copied = (await listFiles(snapshotDir)).filter(f => f.path !== '.snapshot.json');
  if (!sameFiles(second, copied, false)) throw new Error('Snapshot copy does not match the source file list and sizes.');
  info.snapshotCopiedAt = new Date().toISOString();
  await writeFile(marker, JSON.stringify({ version, source: info.source, copiedAt: info.snapshotCopiedAt, files: second }, null, 2));
  return { ...info, root: snapshotDir };
}

// ---------- page instrumentation ----------

// Runs in the page before the Unity loader. Records rAF deltas per phase, long tasks and Web Audio activity.
const PAGE_PROBE = `(() => {
  const P = window.__perf = { phase: null, phases: {}, audio: { contexts: [], started: 0, active: 0, maxActive: 0 } };
  let last = null;
  const tick = t => {
    if (P.phase) {
      if (last !== null) P.phases[P.phase].deltas.push(Math.round((t - last) * 1000) / 1000);
      last = t;
    }
    requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
  P.begin = name => {
    P.phases[name] = { deltas: [], longTasks: [], startedAudio: P.audio.started };
    P.audio.maxActive = P.audio.active;
    P.phase = name;
    last = performance.now();
  };
  P.maxFrameOver = ms => new Promise(done => {
    let prev = null, worst = 0;
    const end = performance.now() + ms;
    const step = t => {
      if (prev !== null) worst = Math.max(worst, t - prev);
      prev = t;
      if (t < end) requestAnimationFrame(step); else done(worst);
    };
    requestAnimationFrame(step);
  });
  P.end = () => {
    const ph = P.phases[P.phase];
    if (ph) { ph.audioSourcesStarted = P.audio.started - ph.startedAudio; ph.audioMaxActive = P.audio.maxActive; }
    P.phase = null;
  };
  try {
    new PerformanceObserver(list => {
      if (!P.phase) return;
      for (const e of list.getEntries()) P.phases[P.phase].longTasks.push(Math.round(e.duration * 10) / 10);
    }).observe({ type: 'longtask' });
    P.longTaskSupported = true;
  } catch { P.longTaskSupported = false; }
  const Base = window.AudioContext || window.webkitAudioContext;
  if (Base) {
    const Tracked = class extends Base { constructor(...a) { super(...a); P.audio.contexts.push(this); } };
    window.AudioContext = Tracked;
    if (window.webkitAudioContext) window.webkitAudioContext = Tracked;
    const start = AudioBufferSourceNode.prototype.start;
    AudioBufferSourceNode.prototype.start = function (...a) {
      P.audio.started++;
      P.audio.active++;
      P.audio.maxActive = Math.max(P.audio.maxActive, P.audio.active);
      this.addEventListener('ended', () => { P.audio.active--; }, { once: true });
      return start.apply(this, a);
    };
  }
  P.gpu = () => {
    const gl = document.createElement('canvas').getContext('webgl2') || document.createElement('canvas').getContext('webgl');
    if (!gl) return null;
    const ext = gl.getExtension('WEBGL_debug_renderer_info');
    return {
      vendor: ext ? gl.getParameter(ext.UNMASKED_VENDOR_WEBGL) : gl.getParameter(gl.VENDOR),
      renderer: ext ? gl.getParameter(ext.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER),
      version: gl.getParameter(gl.VERSION),
    };
  };
})();`;

function pageStyle(width, height) {
  // The template pins the desktop canvas at 960x600 inline; !important beats inline styles without editing the build.
  return `<style>
html, body { margin: 0 !important; overflow: hidden !important; }
#unity-container { position: fixed !important; left: 0 !important; top: 0 !important; transform: none !important; }
#unity-fullscreen-container { width: ${width}px !important; height: ${height}px !important; }
#unity-canvas { width: ${width}px !important; height: ${height}px !important; }
#unity-footer { display: none !important; }
</style>`;
}

function instrumentHtml(html, opts) {
  const hooks = { head: html.includes('<head>'), instance: html.includes('.then((unityInstance) => {') };
  const patched = html
    .replace('<head>', `<head>${pageStyle(opts.width, opts.height)}<script>${PAGE_PROBE}</script>`)
    .replace('.then((unityInstance) => {', '.then((unityInstance) => { window.__unity = unityInstance;');
  return { patched, hooks };
}

function serve(root, opts) {
  const mime = {
    '.html': 'text/html', '.js': 'application/javascript', '.wasm': 'application/wasm', '.css': 'text/css',
    '.png': 'image/png', '.ico': 'image/x-icon', '.json': 'application/json',
  };
  const state = { hooks: null };
  const server = createServer(async (request, response) => {
    try {
      let path = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
      if (path === '/') path = '/index.html';
      const target = resolve(root, '.' + path);
      if (!target.startsWith(root + sep)) { response.writeHead(403).end(); return; }
      let body = await readFile(target);
      if (path === '/index.html') {
        const { patched, hooks } = instrumentHtml(body.toString(), opts);
        state.hooks = hooks;
        body = Buffer.from(patched);
      }
      response.writeHead(200, { 'Content-Type': mime[extname(target)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
      response.end(body);
    } catch {
      response.writeHead(404).end();
    }
  });
  return new Promise(done => server.listen(0, '127.0.0.1', () => done({ server, state, port: server.address().port })));
}

// ---------- statistics ----------

function frameStats(deltas) {
  if (!deltas.length) return null;
  const sorted = [...deltas].sort((a, b) => a - b);
  const pct = p => sorted[Math.min(sorted.length - 1, Math.max(0, Math.ceil((p / 100) * sorted.length) - 1))];
  const mean = deltas.reduce((a, b) => a + b, 0) / deltas.length;
  let elapsed = 0;
  const timed = deltas.map(d => { elapsed += d; return { ms: d, atS: elapsed / 1000 }; });
  return {
    frames: deltas.length,
    durationS: round(elapsed / 1000, 1),
    meanMs: round(mean), p50Ms: round(pct(50)), p95Ms: round(pct(95)), p99Ms: round(pct(99)), maxMs: round(sorted.at(-1)),
    approxFps: round(1000 / mean, 1),
    framesOver33_3Ms: deltas.filter(d => d > 33.3).length,
    framesOver50Ms: deltas.filter(d => d > 50).length,
    worstFrames: timed.sort((a, b) => b.ms - a.ms).slice(0, 5).map(f => ({ ms: round(f.ms), atS: round(f.atS, 1) })),
  };
}

function longTaskStats(tasks) {
  return {
    count: tasks.length,
    totalMs: round(tasks.reduce((a, b) => a + b, 0), 1),
    maxMs: tasks.length ? round(Math.max(...tasks), 1) : null,
  };
}

// ---------- main ----------

async function main() {
  const opts = parseArgs(process.argv.slice(2));
  if (opts.help) { console.log(HELP); return; }
  const log = message => console.log(`[perf] ${message}`);
  const generatedAt = new Date();

  const machine = {
    os: `${os.type()} ${os.release()}`, cpu: os.cpus()[0]?.model?.trim(), logicalCores: os.cpus().length,
    memoryGB: round(os.totalmem() / 2 ** 30, 1), node: process.version,
  };

  let build = null, root = null, server = null, serverState = null, baseUrl;
  if (opts.url) {
    baseUrl = opts.url;
  } else {
    build = await prepareBuild(opts, log);
    root = build.root;
    ({ server, state: serverState, port: build.port } = await serve(root, opts));
    baseUrl = `http://127.0.0.1:${build.port}/`;
  }
  const url = new URL(baseUrl);
  url.searchParams.set('seed', String(opts.seed));
  if (opts.phys) url.searchParams.set('physProbe', '1');

  log('Sampling machine load for 3 s before launch ...');
  const preSampler = new CpuSampler();
  await sleep(3000);
  preSampler.sample();
  machine.cpuBusyBeforeLaunchPct = round(preSampler.samples[0], 1);
  machine.unityProcessesBefore = await unityProcesses();
  let unityMaxDuring = machine.unityProcessesBefore?.length ?? null;

  const t0 = Date.now();
  const clock = () => round((Date.now() - t0) / 1000, 1);
  const consoleLines = [], errors = [], phys = [], events = [], screenshots = [];
  let physStarts = 0, physResult = null;

  const browser = await chromium.launch({
    channel: 'chrome',
    headless: !opts.headed,
    args: [
      '--autoplay-policy=no-user-gesture-required', '--ignore-gpu-blocklist',
      '--disable-background-timer-throttling', '--disable-renderer-backgrounding', '--disable-backgrounding-occluded-windows',
    ],
  });
  const stem = await reportStem(opts, null);
  try {
    const context = await browser.newContext({ viewport: { width: opts.width, height: opts.height }, deviceScaleFactor: 1 });
    const page = await context.newPage();
    page.on('console', message => {
      const entry = { t: clock(), type: message.type(), text: message.text() };
      consoleLines.push(entry);
      if (/\[PHYS(-RESULT)?\]/.test(entry.text)) {
        phys.push(entry);
        if (/\[PHYS\] start/.test(entry.text)) {
          physStarts++;
          if (physStarts > 1) events.push({ t: entry.t, what: 'Physics probe started a second time (scene reloaded, new run).' });
        }
        if (/\[PHYS-RESULT\]/.test(entry.text)) {
          physResult = entry;
          if (/PARTIAL/.test(entry.text)) events.push({ t: entry.t, what: 'Physics probe reported early (PARTIAL): the scene was destroyed mid-window, e.g. wreck or retry.' });
        }
      }
      if (entry.type === 'error') errors.push(entry);
    });
    page.on('pageerror', error => errors.push({ t: clock(), type: 'pageerror', text: String(error) }));
    page.on('crash', () => events.push({ t: clock(), what: 'Browser tab crashed.' }));

    log(`Opening ${url.href} in Chrome (${opts.headed ? 'headed' : 'headless'}) at ${opts.width}x${opts.height} ...`);
    await page.goto(url.href, { waitUntil: 'load' });
    if (serverState?.hooks && !serverState.hooks.instance) {
      throw new Error('index.html no longer matches the Unity template hook (.then((unityInstance) => {); update instrumentHtml.');
    }
    await page.waitForFunction(() => !!window.__unity, null, { timeout: 120_000 }).catch(() => {
      throw new Error('Unity player did not initialize within 120 s (snapshot may be broken).');
    });
    const playerReadyAtS = clock();
    log(`Unity player ready after ${playerReadyAtS} s.`);
    await page.evaluate(() => document.querySelector('#unity-canvas').focus());
    const canvas = await page.evaluate(() => {
      const c = document.querySelector('#unity-canvas');
      return { width: c.width, height: c.height, focused: document.activeElement === c };
    });
    // Unity stalls the main thread for seconds right after load; start recording once frames are steady.
    let steadyWindows = 0, settleWorstMs = null;
    const settleBegin = Date.now();
    while (steadyWindows < 2 && Date.now() - settleBegin < 30_000) {
      settleWorstMs = await page.evaluate(() => window.__perf.maxFrameOver(1000));
      steadyWindows = settleWorstMs < 100 ? steadyWindows + 1 : 0;
    }
    const settledAfterS = round((Date.now() - settleBegin) / 1000, 1);
    if (steadyWindows < 2) events.push({ t: clock(), what: `Frames never settled below 100 ms within 30 s after load (last worst ${round(settleWorstMs)} ms).` });
    log(`Player settled after ${settledAfterS} s.`);

    const captureDir = resolve(repo, 'builds', 'perf-captures', stem);
    await mkdir(captureDir, { recursive: true });
    const shoot = async label => {
      const file = join(captureDir, `${label}.png`);
      await page.screenshot({ path: file }).then(() => screenshots.push(relative(repo, file).split(sep).join('/'))).catch(() => {});
    };
    const watchUnity = async () => {
      const unity = await unityProcesses();
      if (unity && unityMaxDuring != null && unity.length > unityMaxDuring) {
        events.push({ t: clock(), what: `Unity.exe process count rose to ${unity.length} during the capture.` });
      }
      if (unity) unityMaxDuring = Math.max(unityMaxDuring ?? 0, unity.length);
    };

    const phaseCpu = {};
    const recordPhase = async (name, seconds, step) => {
      const sampler = new CpuSampler();
      const begin = Date.now();
      let nextCpu = 5, nextShot = 0, nextUnity = 30;
      await page.evaluate(n => window.__perf.begin(n), name);
      while ((Date.now() - begin) / 1000 < seconds()) {
        const elapsed = (Date.now() - begin) / 1000;
        if (step) await step(elapsed);
        if (elapsed >= nextCpu) { sampler.sample(); nextCpu += 5; }
        if (opts.screenshotEvery > 0 && elapsed >= nextShot) { await shoot(`${name}-${String(Math.round(elapsed)).padStart(3, '0')}s`); nextShot += opts.screenshotEvery; }
        if (elapsed >= nextUnity) { await watchUnity(); nextUnity += 30; }
        await sleep(100);
      }
      await page.evaluate(() => window.__perf.end());
      phaseCpu[name] = sampler.summary();
    };

    if (opts.titleSeconds > 0) {
      log(`Recording title / attract mode for ${opts.titleSeconds} s ...`);
      await recordPhase('title', () => opts.titleSeconds);
    }

    log('Pressing Enter to start the run ...');
    let enterPresses = 0, runStartConfirmed = null;
    if (opts.phys) {
      runStartConfirmed = false;
      for (let attempt = 0; attempt < 5 && !runStartConfirmed; attempt++) {
        await page.keyboard.press('Enter');
        enterPresses++;
        for (let i = 0; i < 40 && physStarts === 0; i++) await sleep(100);
        runStartConfirmed = physStarts > 0;
      }
      if (!runStartConfirmed) throw new Error('Run did not start: no "[PHYS] start" line after 5 Enter presses.');
    } else {
      await page.keyboard.press('Enter');
      enterPresses++;
      await sleep(1000);
    }
    const runStartedAtS = clock();

    log(`Driving for ${opts.seconds} s (seed ${opts.seed}) ...`);
    const held = new Set();
    const down = async key => { if (!held.has(key)) { held.add(key); await page.keyboard.down(key); } };
    const up = async key => { if (held.has(key)) { held.delete(key); await page.keyboard.up(key); } };
    // Short steering taps, not held full lock: a constant hard weave at speed rolls the truck.
    // Three left taps per right one makes the truck loop instead of driving off the edge of the map.
    const steerPattern = ['KeyA', 'KeyA', 'KeyA', 'KeyD'];
    let steer = null, steerIndex = 0, nextSteer = 2, steerUntil = -1, nextJump = 10, nextReverse = 20, reverseUntil = -1;
    const drive = async elapsed => {
      if (elapsed >= nextReverse) {
        // Backing up periodically frees the truck if it has wedged against scenery.
        await up('KeyW'); await down('KeyS');
        reverseUntil = elapsed + 1.5; nextReverse += 20;
      }
      if (reverseUntil >= 0 && elapsed >= reverseUntil) { await up('KeyS'); reverseUntil = -1; }
      if (reverseUntil < 0) await down('KeyW');
      if (elapsed >= nextSteer) {
        steer = steerPattern[steerIndex++ % steerPattern.length];
        await down(steer);
        steerUntil = elapsed + 0.6; nextSteer += 2;
      }
      if (steerUntil >= 0 && elapsed >= steerUntil) { await up(steer); steerUntil = -1; }
      if (elapsed >= nextJump) { await page.keyboard.press('Space', { delay: 150 }); nextJump += 10; }
    };
    let resultGraceUntil = null;
    await recordPhase('run', () => {
      // With physProbe, keep driving up to 15 s past --seconds until the full result line lands.
      if (!opts.phys || (physResult && !/PARTIAL/.test(physResult.text))) return opts.seconds;
      resultGraceUntil ??= opts.seconds + 15;
      return resultGraceUntil;
    }, drive);
    for (const key of [...held]) await up(key);
    await shoot('run-end');

    const pageData = await page.evaluate(() => ({
      phases: window.__perf.phases, longTaskSupported: window.__perf.longTaskSupported, gpu: window.__perf.gpu(),
      audioContextStates: window.__perf.audio.contexts.map(c => c.state), userAgent: navigator.userAgent,
    }));
    machine.unityProcessesAfter = await unityProcesses();
    machine.unityProcessMaxDuring = unityMaxDuring;

    const report = buildReport({
      opts, generatedAt, machine, build, url, canvas, pageData, phaseCpu, consoleLines, errors, phys, physResult,
      events, screenshots, enterPresses, runStartConfirmed, playerReadyAtS, settledAfterS, runStartedAtS,
    });
    await writeReports(opts, stem, report, log);
  } finally {
    await browser.close().catch(() => {});
    server?.close();
  }
}

async function reportStem(opts) {
  const now = new Date();
  const date = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
  let version = opts.version;
  if (!version && !opts.url) {
    const html = await readFile(join(resolve(repo, opts.build), 'index.html'), 'utf8').catch(() => '');
    version = html.match(/productVersion:\s*"([^"]+)"/)?.[1];
  }
  const base = `frametime-${version ?? 'unknown'}-seed${opts.seed}-${date}`;
  const outDir = resolve(repo, opts.out);
  for (let n = 1; ; n++) {
    const stem = n === 1 ? base : `${base}-${n}`;
    const taken = await access(join(outDir, `${stem}.json`)).then(() => true, () => false);
    if (!taken) return stem;
  }
}

function buildReport(d) {
  const { opts, pageData } = d;
  const phases = {};
  for (const [name, phase] of Object.entries(pageData.phases)) {
    phases[name] = {
      stats: frameStats(phase.deltas),
      longTasks: pageData.longTaskSupported ? longTaskStats(phase.longTasks) : null,
      machineCpuBusy: d.phaseCpu[name] ?? null,
      audioSourcesStarted: phase.audioSourcesStarted ?? null,
      audioMaxActiveSources: phase.audioMaxActive ?? null,
    };
  }
  const renderer = pageData.gpu?.renderer ?? '';
  const softwareRenderer = /SwiftShader|llvmpipe|Software|Basic Render/i.test(renderer);
  let physicsResult = null;
  if (d.physResult) {
    try { physicsResult = JSON.parse(d.physResult.text.slice(d.physResult.text.indexOf('{'))); } catch { /* keep raw only */ }
  }

  const notes = [];
  if (softwareRenderer) notes.push(`GPU string "${renderer}" is a software renderer; frame times do not reflect real GPU cost.`);
  if (d.canvas.width !== opts.width || d.canvas.height !== opts.height) notes.push(`Canvas rendered at ${d.canvas.width}x${d.canvas.height}, not the requested ${opts.width}x${opts.height}.`);
  if (!d.canvas.focused) notes.push('Canvas did not hold focus after canvas.focus().');
  const unityBefore = d.machine.unityProcessesBefore?.length ?? 0;
  if (unityBefore > 0 || d.machine.unityProcessMaxDuring > 0) notes.push(`Unity.exe was running on this machine (${unityBefore} before launch, up to ${d.machine.unityProcessMaxDuring} during); another agent's editor or build may have competed for CPU/GPU.`);
  if (d.machine.cpuBusyBeforeLaunchPct > 25) notes.push(`Machine CPU was ${d.machine.cpuBusyBeforeLaunchPct}% busy before the browser launched.`);
  if (d.build?.forced) notes.push('Build was snapshotted with --force while it looked mid-build.');
  if (pageData.audioContextStates.length === 0) notes.push('No Web Audio context was created; audio cost is not represented.');
  else if (pageData.audioContextStates.some(s => s !== 'running')) notes.push(`Web Audio context state at end: ${pageData.audioContextStates.join(', ')}; audio may not have played.`);
  if (!pageData.longTaskSupported) notes.push('Long-task observer unsupported in this browser.');
  if (opts.phys && !d.physResult) notes.push('No [PHYS-RESULT] line arrived before the capture ended.');
  if (!opts.phys) notes.push('Run without physProbe: a normal run ends at 90 s, so later run-phase frames may be the results screen.');
  for (const e of d.events) notes.push(`t=${e.t}s: ${e.what}`);

  return {
    tool: 'tools/perf/webgl-frametime.mjs',
    generatedAt: d.generatedAt.toISOString(),
    version: d.build?.version ?? opts.version ?? 'unknown',
    seed: opts.seed,
    physProbe: opts.phys,
    options: { seconds: opts.seconds, titleSeconds: opts.titleSeconds, width: opts.width, height: opts.height, headed: opts.headed },
    url: d.url.href.replace(/127\.0\.0\.1:\d+/, '127.0.0.1:<port>'),
    build: d.build ? { ...d.build, root: undefined, port: undefined } : null,
    machine: d.machine,
    browser: { userAgent: pageData.userAgent, gpu: pageData.gpu, softwareRenderer, canvas: d.canvas, longTaskSupported: pageData.longTaskSupported },
    audio: { contextStates: pageData.audioContextStates },
    timeline: { playerReadyAtS: d.playerReadyAtS, settledAfterS: d.settledAfterS, runStartedAtS: d.runStartedAtS, enterPresses: d.enterPresses, runStartConfirmed: d.runStartConfirmed },
    phases,
    physics: { lines: d.phys, result: physicsResult, resultRaw: d.physResult?.text ?? null },
    console: { lines: d.consoleLines.length, errorCount: d.errors.length, errors: d.errors.slice(0, 100) },
    notes,
    screenshots: d.screenshots,
    raw: Object.fromEntries(Object.entries(pageData.phases).map(([name, p]) => [name, { frameDeltasMs: p.deltas, longTasksMs: p.longTasks }])),
  };
}

function markdown(r) {
  const row = (label, f) => `| ${label} | ${['title', 'run'].map(p => (r.phases[p] ? f(r.phases[p]) ?? '–' : '–')).join(' | ')} |`;
  const s = key => p => p.stats?.[key];
  const lines = [
    `# WebGL frame time: ${r.version}, seed ${r.seed}`,
    '',
    `Generated ${r.generatedAt} by \`${r.tool}\`. Observations only; budgets are applied by the reader.`,
    '',
    '## Setup',
    '',
    `- Build: ${!r.build ? r.url : r.build.source === r.build.snapshot ? `\`${r.build.snapshot}\` (existing snapshot, served as is)`
      : `\`${r.build.snapshot}\` (copied from \`${r.build.source}\`${r.build.reusedSnapshot ? ', reused' : ''})`}`,
    `- URL: \`${r.url}\``,
    `- Browser: ${r.browser.userAgent}`,
    `- GPU: ${r.browser.gpu ? `${r.browser.gpu.vendor} / ${r.browser.gpu.renderer}` : 'unavailable'}${r.browser.softwareRenderer ? ' **(software renderer)**' : ''}`,
    `- Canvas: ${r.browser.canvas.width}x${r.browser.canvas.height}, ${r.options.headed ? 'headed' : 'headless'}`,
    `- Machine: ${r.machine.cpu}, ${r.machine.logicalCores} logical cores, ${r.machine.memoryGB} GB, ${r.machine.os}`,
    `- Unity.exe processes: ${r.machine.unityProcessesBefore?.length ?? 'unknown'} before, up to ${r.machine.unityProcessMaxDuring ?? 'unknown'} during, ${r.machine.unityProcessesAfter?.length ?? 'unknown'} after`,
    `- Machine CPU busy before launch: ${r.machine.cpuBusyBeforeLaunchPct ?? '–'}%`,
    '',
    '## Frame times (requestAnimationFrame deltas)',
    '',
    '| | Title | Run |',
    '|---|---|---|',
    row('Duration (s)', s('durationS')),
    row('Frames', s('frames')),
    row('Mean (ms)', s('meanMs')),
    row('p50 (ms)', s('p50Ms')),
    row('p95 (ms)', s('p95Ms')),
    row('p99 (ms)', s('p99Ms')),
    row('Max (ms)', s('maxMs')),
    row('Frames > 33.3 ms', s('framesOver33_3Ms')),
    row('Frames > 50 ms', s('framesOver50Ms')),
    row('Long tasks (count / total ms)', p => (p.longTasks ? `${p.longTasks.count} / ${p.longTasks.totalMs}` : null)),
    row('Machine CPU busy (mean / max %)', p => (p.machineCpuBusy ? `${p.machineCpuBusy.meanPct} / ${p.machineCpuBusy.maxPct}` : null)),
    row('Audio sources started / max active', p => (p.audioSourcesStarted == null ? null : `${p.audioSourcesStarted} / ${p.audioMaxActiveSources}`)),
    '',
    'Worst run frames: ' + (r.phases.run?.stats?.worstFrames.map(f => `${f.ms} ms at ${f.atS} s`).join(', ') || '–'),
    '',
    '## Physics probe',
    '',
    r.physics.resultRaw ? '```\n' + r.physics.resultRaw + '\n```' : (r.physProbe ? 'No `[PHYS-RESULT]` line arrived.' : 'Not requested (`--no-phys`).'),
    '',
    'The verdict field inside this line is printed by the game\'s `PhysicsBudgetProbe`, not by this tool.',
    '',
    '## Console',
    '',
    `${r.console.lines} lines, ${r.console.errorCount} errors.`,
    ...r.console.errors.slice(0, 10).map(e => `- t=${e.t}s ${e.type}: ${e.text.split('\n')[0].slice(0, 200)}`),
    '',
    '## Notes',
    '',
    ...(r.notes.length ? r.notes.map(n => `- ${n}`) : ['- None.']),
    '',
    '## Files',
    '',
    '- Raw frame deltas, long tasks and every `[PHYS]` line are in the matching `.json`.',
    `- Screenshots (local only, gitignored): ${r.screenshots.length ? `\`${dirname(r.screenshots[0])}/\`` : 'none'}`,
    '',
  ];
  return lines.join('\n');
}

async function writeReports(opts, stem, report, log) {
  const outDir = resolve(repo, opts.out);
  await mkdir(outDir, { recursive: true });
  const jsonPath = join(outDir, `${stem}.json`);
  const mdPath = join(outDir, `${stem}.md`);
  await writeFile(jsonPath, JSON.stringify(report, null, 1));
  await writeFile(mdPath, markdown(report));
  log(`Wrote ${relative(repo, jsonPath)} and ${relative(repo, mdPath)}`);
  const run = report.phases.run?.stats;
  if (run) log(`Run: ${run.frames} frames, p95 ${run.p95Ms} ms, max ${run.maxMs} ms, >50 ms: ${run.framesOver50Ms}`);
  for (const note of report.notes) log(`Note: ${note}`);
}

main().catch(error => {
  console.error(`[perf] ${error.message}`);
  process.exitCode = 1;
});
