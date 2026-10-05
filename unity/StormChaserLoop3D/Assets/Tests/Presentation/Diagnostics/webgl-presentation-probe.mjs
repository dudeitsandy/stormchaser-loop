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
const titleSeconds = Number(process.env.PRESENTATION_PROBE_TITLE_SECONDS || 0);
const [canvasWidth, canvasHeight] = (process.env.PRESENTATION_PROBE_CANVAS || '960x600').split('x').map(Number);
const categories = ['pipCamera','camcorder','funnelMass','rainCards','stormSkyDeck','tornadoCards','windCards','cardsUnclassified','farFieldCards','worldAndUI'];
const build = resolve(root, process.env.PRESENTATION_PROBE_BUILD || 'builds/webgl');
const query = new URLSearchParams(process.env.PRESENTATION_PROBE_QUERY || '').toString();
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
      content = Buffer.from(content.toString().replace('<head>', `<head><script>window.__presentationAudioDiagnostic=${process.env.PRESENTATION_PROBE_FEEDBACK === '1'}</script><script src="/probe.js"></script>`)
        .replace('.then((unityInstance) => {', '.then((unityInstance) => { window.__unity = unityInstance;')
        .replaceAll('width="960"', `width="${canvasWidth}"`).replaceAll('height="600"', `height="${canvasHeight}"`)
        .replaceAll('"960px"', `"${canvasWidth}px"`).replaceAll('"600px"', `"${canvasHeight}px"`));
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
  `--window-size=${canvasWidth + 140},${canvasHeight + 150}`, 'about:blank',
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
  await cdp('Page.navigate', { url: `http://127.0.0.1:${port}/${query ? '?' + query : ''}` });
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
  await evaluate('document.querySelector("canvas").focus()');
  let titleFrames = [];
  if (titleSeconds > 0) {
    await evaluate('__presentationProbe.start()');
    console.log(`Sampling title for ${titleSeconds}s.`);
    for (let elapsed = 0; elapsed < titleSeconds; elapsed += 5) await sleep(Math.min(5,titleSeconds-elapsed)*1000);
    await evaluate('__presentationProbe.stop()');
    await sleep(200);
    titleFrames = await evaluate('__presentationProbe.frames');
    const titleShot = await cdp('Page.captureScreenshot', {format:'png'});
    await writeFile(join(output,'webgl-title.png'),Buffer.from(titleShot.data,'base64'));
  }
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
  console.log(`Sampling WebGL for ${seconds}s at ${canvasWidth}x${canvasHeight} (renderer: ${await evaluate('__presentationProbe.gpu?.renderer')}).`);
  if (process.env.PRESENTATION_PROBE_FEEDBACK === '1') {
    await evaluate('__presentationProbe.audioDiagnostic=true');
    async function input(key,code,vk,type) { await cdp('Input.dispatchKeyEvent',{type,key,code,windowsVirtualKeyCode:vk}); }
    async function capture(label) {
      await evaluate(`(__presentationProbe.feedbackMarkers ||= []).push({label:${JSON.stringify(label)},time:performance.now()})`);
      const shot = await cdp('Page.captureScreenshot',{format:'png'});
      await writeFile(join(output,`feedback-${label}.png`),Buffer.from(shot.data,'base64'));
    }
    // Start with two isolated flat-road jumps, then accelerate/e-brake, counter-steer and coast.
    for(let jump=1;jump<=2;jump++) {
      await input(' ','Space',32,'keyDown'); await sleep(100); await input(' ','Space',32,'keyUp');
      await sleep(350); await capture(`jump${jump}-air`);
      await sleep(350); await capture(`jump${jump}-landing`);
      await sleep(1000); await capture(`jump${jump}-settled`);
    }
    await input('w','KeyW',87,'keyDown'); await sleep(3000);
    await input('d','KeyD',68,'keyDown'); await input('Control','ControlLeft',17,'keyDown');
    await sleep(1000); await capture('slide');
    await input('Control','ControlLeft',17,'keyUp'); await input('d','KeyD',68,'keyUp');
    await input('a','KeyA',65,'keyDown'); await sleep(500); await input('a','KeyA',65,'keyUp');
    await input('w','KeyW',87,'keyUp'); await sleep(2000); await capture('coast');
    await sleep(3000); await capture('settled');
    await evaluate('__presentationProbe.audioDiagnostic=false');
  }
  let lastDirection = null;
  for (let elapsed = 0; elapsed < seconds; elapsed++) {
    if (process.env.PRESENTATION_PROBE_DRIVE === '1') {
      await cdp('Input.dispatchKeyEvent', {type:'keyDown',key:'w',code:'KeyW',windowsVirtualKeyCode:87});
      const direction = Math.floor(elapsed / 2) % 2 ? 'a' : 'd';
      if (direction !== lastDirection) {
        if (lastDirection) await cdp('Input.dispatchKeyEvent', {type:'keyUp',key:lastDirection,code:lastDirection==='a'?'KeyA':'KeyD',windowsVirtualKeyCode:lastDirection==='a'?65:68});
        await cdp('Input.dispatchKeyEvent', {type:'keyDown',key:direction,code:direction==='a'?'KeyA':'KeyD',windowsVirtualKeyCode:direction==='a'?65:68});
        lastDirection = direction;
      }
      if (elapsed % 12 === 0) await cdp('Input.dispatchKeyEvent', {type:'keyDown',key:' ',code:'Space',windowsVirtualKeyCode:32});
      if (elapsed % 12 === 1) await cdp('Input.dispatchKeyEvent', {type:'keyUp',key:' ',code:'Space',windowsVirtualKeyCode:32});
    }
    await sleep(Math.min(1,seconds-elapsed)*1000);
    if ((elapsed + 1) % 5 !== 0) continue;
    const intervalCapture = await cdp('Page.captureScreenshot', {format:'png'});
    await writeFile(join(output, `webgl-capture-${elapsed + 1}s.png`),Buffer.from(intervalCapture.data,'base64'));
    console.log(`Captured ${await evaluate('__presentationProbe.frames.length')} frames at ${elapsed+1}s.`);
    if (process.env.PRESENTATION_PROBE_STATIONARY !== '1' && process.env.PRESENTATION_PROBE_DRIVE !== '1') {
      await cdp('Input.dispatchKeyEvent', {type:'keyDown',key:'w',code:'KeyW',windowsVirtualKeyCode:87});
      await cdp('Input.dispatchKeyEvent', {type:(elapsed+1)%10===5?'keyDown':'keyUp',key:'d',code:'KeyD',windowsVirtualKeyCode:68});
    }
    // Keep drive captures a single run; results are observed rather than silently starting another run.
    if (process.env.PRESENTATION_PROBE_DRIVE !== '1' && await evaluate('!__presentationProbe.frames.at(-1)?.drawCalls.camcorder')) {
      await cdp('Input.dispatchKeyEvent', {type:'keyDown',key:'Enter',code:'Enter',windowsVirtualKeyCode:13});
      await cdp('Input.dispatchKeyEvent', {type:'keyUp',key:'Enter',code:'Enter',windowsVirtualKeyCode:13});
    }
  }
  if (process.env.PRESENTATION_PROBE_BRIGHTNESS === '1') {
    async function key(key, code, vk) {
      await cdp('Input.dispatchKeyEvent', {type:'keyDown', key, code, windowsVirtualKeyCode:vk});
      await sleep(100);
      await cdp('Input.dispatchKeyEvent', {type:'keyUp', key, code, windowsVirtualKeyCode:vk});
      await sleep(150);
    }
    async function send(method, value) {
      const firstLog = logs.length;
      await evaluate(`__unity.SendMessage('SessionManager', ${JSON.stringify(method)}${value === undefined ? '' : ',' + value})`);
      await sleep(100);
      const missing = logs.slice(firstLog).find(line => /does not have receiver|has no receiver/.test(line));
      if (missing) {
        await writeFile(join(output, 'brightness-prerequisite-failure.json'), JSON.stringify({build, query, method, error:missing}, null, 2));
        throw new Error(`Brightness capture requires a rebuilt Settings candidate: ${missing}`);
      }
    }
    for (const [label, direction, steps] of [['minus50',-1,5], ['zero',1,5], ['plus50',1,5]]) {
      await key('Escape','Escape',27);
      await send('OnPauseClick',1);
      await send('OnSettingsHover',3);
      for(let i=0;i<steps;i++) await send('ChangeSetting',direction);
      const settings = await cdp('Page.captureScreenshot', {format:'png'});
      await writeFile(join(output, `brightness-${label}-settings.png`),Buffer.from(settings.data,'base64'));
      await send('CloseSettings');
      await send('Resume');
      await sleep(300);
      const shot = await cdp('Page.captureScreenshot', {format:'png'});
      await writeFile(join(output, `brightness-${label}.png`),Buffer.from(shot.data,'base64'));
      console.log(`Captured brightness ${label}.`);
    }
  }
  await evaluate('__presentationProbe.stop()');
  await cdp('Tracing.end', {}, null);
  for (let i = 0; i < 30 && !traceFinished; i++) await sleep(100);
  await sleep(1000);
  await evaluate('__presentationProbe.contexts.forEach(c => c.poll())');
  const data = await evaluate(`({gpu:__presentationProbe.gpu, frames:__presentationProbe.frames,
    feedbackMarkers:__presentationProbe.feedbackMarkers, audioSnapshots:__presentationProbe.audioSnapshots, audioStarts:__presentationProbe.audioStarts, drawSignatures:__presentationProbe.drawSignatures, programs:__presentationProbe.programs, errors:__presentationProbe.errors,
    audioSources:__presentationProbe.audioSources, maxAudioSources:__presentationProbe.maxAudioSources,
    missingMaterialBuffer:__presentationProbe.missingMaterialBuffer,
    materialColors:__presentationProbe.materialColors,
    farFieldSamples:__presentationProbe.farFieldSamples,
    skippedQueries:__presentationProbe.skippedQueries, canvas:{width:document.querySelector('canvas').width,height:document.querySelector('canvas').height},
    browser:navigator.userAgent})`);
  data.titleFrames = titleFrames;
  data.console = logs;
  data.audioTrace = audioTrace;
  const screenshot = await cdp('Page.captureScreenshot', { format: 'png' });
  await writeFile(join(output, 'webgl-capture.png'), Buffer.from(screenshot.data, 'base64'));
  if (process.env.PRESENTATION_PROBE_FAR_COMPARISON === '1') {
    await evaluate('__presentationProbe.hideFarField = true');
    await sleep(200);
    const hidden = await cdp('Page.captureScreenshot', { format: 'png' });
    await writeFile(join(output, 'webgl-far-hidden.png'), Buffer.from(hidden.data, 'base64'));
    await evaluate('__presentationProbe.hideFarField = false');
  }
  if (process.env.PRESENTATION_PROBE_EMPTY_SKY === '1') {
    // GPU draw-only comparison: keep gameplay, both cameras, UI, post and culling unchanged.
    // This is not an empty-scene CPU baseline, and suppresses all near VFX cards, not just tornadoes.
    await evaluate('__presentationProbe.hideNearCards = true; __presentationProbe.start()');
    await sleep(5000);
    await evaluate('__presentationProbe.stop()');
    data.emptySkyDrawFrames = await evaluate('__presentationProbe.frames');
    const emptySky = await cdp('Page.captureScreenshot', { format: 'png' });
    await writeFile(join(output, 'webgl-empty-sky.png'), Buffer.from(emptySky.data, 'base64'));
    await evaluate('__presentationProbe.hideNearCards = false');
  }
  await writeFile(join(output, 'webgl-raw.json'), JSON.stringify(data, null, 2));
  const mean = values => values.length ? values.reduce((sum,v) => sum + v, 0) / values.length : null;
  const p95 = values => values.length ? [...values].sort((a,b) => a-b)[Math.floor((values.length - 1) * 0.95)] : null;
  const summary = { gpu:data.gpu, canvas:data.canvas, browser:data.browser, frames:data.frames.length,
    build, query, farFieldSamples:data.farFieldSamples, durationSeconds:seconds, maxAudioSources:data.maxAudioSources, skippedQueries:data.skippedQueries, categories:{} };
  const running = data.frames.filter(frame => frame.drawCalls.camcorder > 0);
  summary.feedback = process.env.PRESENTATION_PROBE_FEEDBACK === '1';
  summary.runningFrames = running.length;
  summary.drawSignatures = data.drawSignatures;
  const intervals = frames => frames.slice(1).map((frame,i) => frame.timestamp - frames[i].timestamp);
  const observations = values => ({frames:values.length, meanMs:mean(values), p95Ms:p95(values),
    maxMs:values.length ? Math.max(...values) : null, over33_3:values.filter(v=>v>33.3).length, over50:values.filter(v=>v>50).length});
  summary.titleFrameIntervals = observations(intervals(titleFrames));
  summary.runFrameIntervals = observations(intervals(data.frames));
  summary.measurementScope = 'Instrumented browser GPU/GL/audio controls. Not per-component Unity CPU or separate siren/radio DSP. Frame intervals include probe overhead and screenshot work; Cursor owns uninstrumented M1 frame capture.';
  const totalDraws = frames => frames.map(frame => Object.values(frame.drawCalls).reduce((sum, value) => sum + value, 0));
  summary.totalDrawCallsMean = mean(totalDraws(running));
  if (data.emptySkyDrawFrames) {
    const emptyFrames = data.emptySkyDrawFrames.filter(frame => frame.drawCalls.camcorder > 0);
    summary.emptySkyDrawComparison = { frames:emptyFrames.length, drawCallsMean:mean(totalDraws(emptyFrames)),
      extraDrawsMean:summary.totalDrawCallsMean - mean(totalDraws(emptyFrames)),
      scope:'Browser suppresses near VFX card draws only; cameras/UI/post/gameplay/culling remain active. Not a CPU baseline.' };
  }
  for (const category of categories) {
    const gpuFrames = running.filter(frame => Object.keys(frame.gpuMs).length);
    const gpuValues = gpuFrames.map(frame => frame.gpuMs[category] || 0);
    const cpuValues = running.map(frame => frame.submitMs[category] || 0);
    summary.categories[category] = { gpuMeanMs:mean(gpuValues), gpuP95Ms:p95(gpuValues), gpuFrames:gpuFrames.length,
      glSubmitMeanMs:mean(cpuValues), glSubmitP95Ms:p95(cpuValues), drawCallsMean:mean(running.map(f => f.drawCalls[category] || 0)),
      drawCallsMax:Math.max(...running.map(f => f.drawCalls[category] || 0)) };
  }
  const titleSamples = titleFrames.filter(frame => Object.keys(frame.gpuMs).length);
  summary.titleCategories = {};
  for (const category of categories) {
    summary.titleCategories[category] = {
      gpuMeanMs:mean(titleSamples.map(frame => frame.gpuMs[category] || 0)),
      gpuP95Ms:p95(titleSamples.map(frame => frame.gpuMs[category] || 0)),
      glSubmitMeanMs:mean(titleFrames.map(frame => frame.submitMs[category] || 0)),
      drawCallsMean:mean(titleFrames.map(frame => frame.drawCalls[category] || 0))
    };
  }
  summary.titleAudioSubmitMeanMs = mean(titleFrames.map(frame => frame.audioSubmitMs));
  summary.audioSubmitMeanMs = mean(running.map(frame => frame.audioSubmitMs));
  summary.audioSubmitP95Ms = p95(running.map(frame => frame.audioSubmitMs));
  summary.callbackMeanMs = mean(data.frames.map(frame => frame.callbackMs));
  summary.callbackP95Ms = p95(data.frames.map(frame => frame.callbackMs));
  summary.frameIntervalMeanMs = mean(data.frames.slice(1).map((frame,i) => frame.timestamp - data.frames[i].timestamp));
  const sampled = running.filter(frame => Object.keys(frame.gpuMs).length);
  const subtotal = sampled.map(frame => ['pipCamera','camcorder','funnelMass','rainCards','stormSkyDeck','tornadoCards','windCards','cardsUnclassified','farFieldCards']
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
