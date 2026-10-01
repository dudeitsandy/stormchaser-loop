// Usage: node webgl-presentation-probe.mjs <repo-root> <output-directory> [sample-seconds=30]
// Serves the existing build and injects diagnostics without changing build files.
import { createServer } from 'node:http';
import { readFile, writeFile, mkdir, mkdtemp } from 'node:fs/promises';
import { resolve, join, extname, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { tmpdir } from 'node:os';
import { spawn } from 'node:child_process';

const root = resolve(process.argv[2]);
const output = resolve(process.argv[3]);
const seconds = Number(process.argv[4] || 30);
const build = join(root, 'builds/webgl');
const here = dirname(fileURLToPath(import.meta.url));
await mkdir(output, { recursive: true });
const mime = { '.html':'text/html', '.js':'application/javascript', '.wasm':'application/wasm', '.data':'application/octet-stream', '.css':'text/css', '.png':'image/png' };
const server = createServer(async (request, response) => {
  try {
    const path = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
    const target = path === '/probe.js' ? join(here, 'webgl-probe.js') : resolve(build, '.' + (path === '/' ? '/index.html' : path));
    if (path !== '/probe.js' && !target.startsWith(build + '/'.replace('/', process.platform === 'win32' ? '\\' : '/')) && target !== join(build, 'index.html')) {
      response.writeHead(403).end(); return;
    }
    let content = await readFile(target);
    if (path === '/' || path === '/index.html') {
      content = Buffer.from(content.toString().replace('<head>', '<head><script src="/probe.js"></script>')
        .replace('.then((unityInstance) => {', '.then((unityInstance) => { window.__unity = unityInstance;'));
    }
    response.setHeader('Content-Type', mime[extname(target)] || 'application/octet-stream');
    response.end(content);
  } catch { response.writeHead(404).end(); }
});
await new Promise(done => server.listen(0, '127.0.0.1', done));
const port = server.address().port;
const profile = await mkdtemp(join(tmpdir(), 'stormchaser-webgl-probe-'));
const chrome = spawn('C:/Program Files/Google/Chrome/Application/chrome.exe', [
  '--headless=new', '--remote-debugging-port=0', `--user-data-dir=${profile}`,
  '--no-first-run', '--no-default-browser-check', '--autoplay-policy=no-user-gesture-required',
  '--disable-background-timer-throttling', '--disable-renderer-backgrounding',
  '--window-size=1100,750', 'about:blank',
], { windowsHide: true, stdio: ['ignore', 'ignore', 'pipe'] });
let ws, cdp, session;
const sleep = ms => new Promise(done => setTimeout(done, ms));
try {
  let devtoolsPort;
  for (let i = 0; i < 100; i++) {
    try { devtoolsPort = Number((await readFile(join(profile, 'DevToolsActivePort'), 'utf8')).split('\n')[0]); break; }
    catch { await sleep(200); }
  }
  if (!devtoolsPort) throw new Error('Chrome did not open its diagnostic port.');
  const browserInfo = await (await fetch(`http://127.0.0.1:${devtoolsPort}/json/version`)).json();
  ws = new WebSocket(browserInfo.webSocketDebuggerUrl);
  await new Promise((done, fail) => { ws.onopen = done; ws.onerror = fail; });
  let id = 0;
  const pending = new Map();
  const logs = [];
  const audioTrace = [];
  let traceFinished = false;
  ws.onmessage = event => {
    const message = JSON.parse(event.data);
    if (message.id) {
      const callback = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) callback?.reject(new Error(JSON.stringify(message.error)));
      else callback?.resolve(message.result);
    } else if (message.method === 'Tracing.dataCollected') audioTrace.push(...message.params.value);
    else if (message.method === 'Tracing.tracingComplete') traceFinished = true;
    else if (message.method === 'Runtime.consoleAPICalled')
      logs.push(message.params.args.map(arg => arg.value || arg.description).join(' '));
  };
  cdp = (method, params = {}, sessionId = session) => new Promise((resolve, reject) => {
    const requestId = ++id;
    pending.set(requestId, { resolve, reject });
    ws.send(JSON.stringify({ id: requestId, method, params, ...(sessionId ? { sessionId } : {}) }));
  });
  const { targetId } = await cdp('Target.createTarget', { url: 'about:blank' }, null);
  session = (await cdp('Target.attachToTarget', { targetId, flatten: true }, null)).sessionId;
  await cdp('Runtime.enable');
  await cdp('Page.enable');
  await cdp('Page.navigate', { url: `http://127.0.0.1:${port}/` });
  async function evaluate(expression) {
    const result = await cdp('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (result.exceptionDetails) throw new Error(JSON.stringify(result.exceptionDetails));
    return result.result.value;
  }
  for (let i = 0; i < 120; i++) {
    if (await evaluate('!!window.__unity')) break;
    if (i === 119) throw new Error('Unity player did not initialize within 60 seconds.');
    await sleep(500);
  }
  await sleep(1000);
  await cdp('Input.dispatchMouseEvent', { type: 'mousePressed', x: 500, y: 300, button: 'left', clickCount: 1 });
  await cdp('Input.dispatchMouseEvent', { type: 'mouseReleased', x: 500, y: 300, button: 'left', clickCount: 1 });
  await cdp('Input.dispatchKeyEvent', { type: 'keyDown', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13 });
  await cdp('Input.dispatchKeyEvent', { type: 'keyUp', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13 });
  await sleep(4000);
  await evaluate('__presentationProbe.start()');
  for (let attempt = 0; attempt < 10; attempt++) {
    await sleep(1000);
    if (await evaluate('__presentationProbe.frames.at(-1)?.drawCalls.camcorder > 0')) break;
    await cdp('Input.dispatchKeyEvent', { type: 'keyDown', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13 });
    await cdp('Input.dispatchKeyEvent', { type: 'keyUp', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13 });
    if (attempt === 9) throw new Error('Gameplay did not start; no camcorder pass observed.');
  }
  await cdp('Tracing.start', { categories: 'audio,disabled-by-default-audio,media', transferMode: 'ReportEvents' }, null);
  await evaluate('__presentationProbe.start()');
  console.log(`Sampling WebGL for ${seconds}s at 960×600 (renderer: ${await evaluate('__presentationProbe.gpu?.renderer')}).`);
  for (let elapsed = 0; elapsed < seconds; elapsed += 5) {
    await sleep(Math.min(5, seconds - elapsed) * 1000);
    const intervalCapture = await cdp('Page.captureScreenshot', { format: 'png' });
    await writeFile(join(output, `webgl-capture-${elapsed + 5}s.png`), Buffer.from(intervalCapture.data, 'base64'));
    // Drive a few seconds, then turn. No shots: keep the audio-loop baseline repeatable.
    if (process.env.PRESENTATION_PROBE_STATIONARY === '1') {
      // Keep the starting view for funnel capture instead of driving away.
    } else if (elapsed % 10 === 0) {
      await cdp('Input.dispatchKeyEvent', { type: 'keyDown', key: 'w', code: 'KeyW', windowsVirtualKeyCode: 87 });
      await cdp('Input.dispatchKeyEvent', { type: 'keyDown', key: 'd', code: 'KeyD', windowsVirtualKeyCode: 68 });
    } else {
      await cdp('Input.dispatchKeyEvent', { type: 'keyUp', key: 'd', code: 'KeyD', windowsVirtualKeyCode: 68 });
    }
    console.log(`Captured ${await evaluate('__presentationProbe.frames.length')} frames.`);
    if (await evaluate('!__presentationProbe.frames.at(-1)?.drawCalls.camcorder')) {
      await cdp('Input.dispatchKeyEvent', { type: 'keyDown', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13 });
      await cdp('Input.dispatchKeyEvent', { type: 'keyUp', key: 'Enter', code: 'Enter', windowsVirtualKeyCode: 13 });
    }
  }
  await evaluate('__presentationProbe.stop()');
  await cdp('Tracing.end', {}, null);
  for (let i = 0; i < 30 && !traceFinished; i++) await sleep(100);
  await sleep(1000);
  await evaluate('__presentationProbe.contexts.forEach(c => c.poll())');
  const data = await evaluate(`({gpu:__presentationProbe.gpu, frames:__presentationProbe.frames,
    programs:__presentationProbe.programs, errors:__presentationProbe.errors,
    audioSources:__presentationProbe.audioSources, maxAudioSources:__presentationProbe.maxAudioSources,
    missingMaterialBuffer:__presentationProbe.missingMaterialBuffer,
    materialColors:__presentationProbe.materialColors,
    skippedQueries:__presentationProbe.skippedQueries, canvas:{width:document.querySelector('canvas').width,height:document.querySelector('canvas').height},
    browser:navigator.userAgent})`);
  data.console = logs;
  data.audioTrace = audioTrace;
  const screenshot = await cdp('Page.captureScreenshot', { format: 'png' });
  await writeFile(join(output, 'webgl-capture.png'), Buffer.from(screenshot.data, 'base64'));
  await writeFile(join(output, 'webgl-raw.json'), JSON.stringify(data, null, 2));
  const mean = values => values.length ? values.reduce((sum,v) => sum + v, 0) / values.length : null;
  const p95 = values => values.length ? [...values].sort((a,b) => a-b)[Math.floor((values.length - 1) * 0.95)] : null;
  const summary = { gpu:data.gpu, canvas:data.canvas, browser:data.browser, frames:data.frames.length,
    durationSeconds:seconds, maxAudioSources:data.maxAudioSources, skippedQueries:data.skippedQueries, categories:{} };
  const running = data.frames.filter(frame => frame.drawCalls.camcorder > 0);
  summary.runningFrames = running.length;
  for (const category of ['pipCamera','camcorder','tornadoCards','windCards','cardsUnclassified','worldAndUI']) {
    const gpuFrames = running.filter(frame => Object.keys(frame.gpuMs).length);
    const gpuValues = gpuFrames.map(frame => frame.gpuMs[category] || 0);
    const cpuValues = running.map(frame => frame.submitMs[category] || 0);
    summary.categories[category] = { gpuMeanMs:mean(gpuValues), gpuP95Ms:p95(gpuValues), gpuFrames:gpuFrames.length,
      glSubmitMeanMs:mean(cpuValues), glSubmitP95Ms:p95(cpuValues), drawCallsMean:mean(running.map(f => f.drawCalls[category] || 0)),
      drawCallsMax:Math.max(...running.map(f => f.drawCalls[category] || 0)) };
  }
  summary.audioSubmitMeanMs = mean(running.map(frame => frame.audioSubmitMs));
  summary.audioSubmitP95Ms = p95(running.map(frame => frame.audioSubmitMs));
  summary.callbackMeanMs = mean(data.frames.map(frame => frame.callbackMs));
  summary.callbackP95Ms = p95(data.frames.map(frame => frame.callbackMs));
  summary.frameIntervalMeanMs = mean(data.frames.slice(1).map((frame,i) => frame.timestamp - data.frames[i].timestamp));
  const sampled = running.filter(frame => Object.keys(frame.gpuMs).length);
  const subtotal = sampled.map(frame => ['pipCamera','camcorder','tornadoCards','windCards','cardsUnclassified']
    .reduce((sum,category) => sum + (frame.gpuMs[category] || 0) + (frame.submitMs[category] || 0), frame.audioSubmitMs));
  summary.measuredSubtotalMeanMs = mean(subtotal);
  summary.measuredSubtotalP95Ms = p95(subtotal);
  const audioEvents = {};
  for (const event of audioTrace) {
    if (event.ph !== 'X' || !event.dur || !/audio|render|process/i.test(event.name)) continue;
    const stats = audioEvents[event.name] ||= { calls:0, totalMs:0 };
    stats.calls++; stats.totalMs += event.dur / 1000;
  }
  summary.audioTraceEvents = audioEvents;
  await writeFile(join(output, 'webgl-summary.json'), JSON.stringify(summary, null, 2));
  console.log(JSON.stringify(summary, null, 2));
} finally {
  if (cdp) { try { await cdp('Browser.close', {}, null); } catch {} }
  ws?.close();
  chrome.kill();
  server.close();
}
