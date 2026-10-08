// Browser-only diagnostic injection. Never imported by Unity or shipped in the game.
(() => {
  const probe = window.__presentationProbe = {
    frames: [], programs: [], contexts: [], errors: [], enabled: false,
    audioDiagnostic: !!window.__presentationAudioDiagnostic, frame: null, audioSources: 0, maxAudioSources: 0, skippedQueries: 0,
  };
  addEventListener('error', e => probe.errors.push(e.message));
  const audioEdges = new WeakMap();
  const audioNodes = [];
  const nativeConnect = AudioNode.prototype.connect;
  AudioNode.prototype.connect = function(destination, ...args) {
    if (probe.audioDiagnostic) audioEdges.set(this, destination);
    return nativeConnect.call(this, destination, ...args);
  };
  let lastAudioSample = 0;
  function audioSnapshot() {
    if (!probe.audioDiagnostic || performance.now() - lastAudioSample < 100) return;
    lastAudioSample = performance.now();
    const nodes = audioNodes.map(item => {
      const gains = [];
      let node = audioEdges.get(item.node);
      for (let i = 0; node && i < 8; i++, node = audioEdges.get(node)) if (node.gain) gains.push(node.gain.value);
      return {id:item.id, kind:item.kind, active:item.active, gains, playbackRate:item.node.playbackRate?.value};
    });
    (probe.audioSnapshots ||= []).push({time:lastAudioSample,nodes});
  }
  const nativeRaf = window.requestAnimationFrame.bind(window);
  let frameStamp = -1;
  window.requestAnimationFrame = callback => nativeRaf(timestamp => {
    const collecting = probe.enabled;
    if (collecting && timestamp !== frameStamp) {
      frameStamp = timestamp;
      probe.frame = { timestamp, callbackMs: 0, audioSubmitMs: 0, drawCalls: {}, submitMs: {}, gpuMs: {} };
      probe.frames.push(probe.frame);
    }
    const start = performance.now();
    try { callback(timestamp); }
    finally {
      if (collecting) {
        for (const ctx of probe.contexts) ctx.end();
        probe.frame.callbackMs += performance.now() - start;
        audioSnapshot();
      }
    }
  });

  const originalGetContext = HTMLCanvasElement.prototype.getContext;
  HTMLCanvasElement.prototype.getContext = function(type, ...args) {
    const gl = originalGetContext.call(this, type, ...args);
    if (gl && type === 'webgl2' && !gl.__presentationInstrumented) instrument(gl);
    return gl;
  };
  function instrument(gl) {
    gl.__presentationInstrumented = true;
    const originals = {};
    for (let proto = Object.getPrototypeOf(gl); proto; proto = Object.getPrototypeOf(proto)) {
      for (const name of Object.getOwnPropertyNames(proto)) {
        const descriptor = Object.getOwnPropertyDescriptor(proto, name);
        if (name !== 'constructor' && typeof descriptor?.value === 'function' && !originals[name])
          originals[name] = descriptor.value.bind(gl);
      }
    }
    const timer = originals.getExtension('EXT_disjoint_timer_query_webgl2');
    const debug = originals.getExtension('WEBGL_debug_renderer_info');
    const info = {
      timerQueries: !!timer,
      renderer: debug ? originals.getParameter(debug.UNMASKED_RENDERER_WEBGL) : originals.getParameter(gl.RENDERER),
      vendor: debug ? originals.getParameter(debug.UNMASKED_VENDOR_WEBGL) : originals.getParameter(gl.VENDOR),
      version: originals.getParameter(gl.VERSION),
    };
    probe.gpu = info;
    const textures = new WeakMap(), fbos = new WeakMap(), programs = new WeakMap();
    const shaders = new WeakMap(), attached = new WeakMap();
    const buffers = new WeakMap(), uniformSlots = new Map(), boundBuffers = new Map();
    let uniformBuffer = null;
    let framebuffer = null, texture = null, program = null, active = null;
    const pending = [];
    function end() {
      if (!active) return;
      originals.endQuery(timer.TIME_ELAPSED_EXT);
      pending.push(active);
      active = null;
    }
    function poll() {
      if (!timer || !pending.length) return;
      const disjoint = originals.getParameter(timer.GPU_DISJOINT_EXT);
      for (let i = pending.length - 1; i >= 0; i--) {
        const item = pending[i];
        if (!originals.getQueryParameter(item.query, gl.QUERY_RESULT_AVAILABLE)) continue;
        if (disjoint) probe.skippedQueries++;
        else {
          const ms = originals.getQueryParameter(item.query, gl.QUERY_RESULT) / 1e6;
          item.frame.gpuMs[item.category] = (item.frame.gpuMs[item.category] || 0) + ms;
        }
        originals.deleteQuery(item.query);
        pending.splice(i, 1);
      }
    }
    function classify(drawCount = 0) {
      let shader = program && programs.get(program);
      if (program && !shader) {
        const uniformCount = originals.getProgramParameter(program, gl.ACTIVE_UNIFORMS);
        const uniforms = [];
        for (let i = 0; i < uniformCount; i++) uniforms.push(originals.getActiveUniform(program, i).name);
        const sources = (attached.get(program) || []).map(s => shaders.get(s) || '').join('\n');
        const names = uniforms.join(' ');
        let kind = names.includes('_ChromaPixels') && names.includes('_BarrelStrength') ? 'camcorder' : 'other';
        if (names.includes('_BaseColor') && names.includes('_BaseMap') && names.includes('unity_FogColor')
          && !/_ShadowTint|_RimColor|_MainLightColor|_WorldSpaceCameraPos|_Cutoff/.test(names)) kind = 'cards';
        if (names.includes('_HorizonBlend') && names.includes('_BaseMap')) kind = 'farField';
        shader = { id: probe.programs.length, uniforms, sources, kind };
        if (kind === 'cards') {
          const indices = originals.getUniformIndices(program, ['_BaseColor']);
          shader.colorOffset = originals.getActiveUniforms(program, indices, gl.UNIFORM_OFFSET)[0];
          shader.colorBlock = originals.getActiveUniforms(program, indices, gl.UNIFORM_BLOCK_INDEX)[0];
          shader.colorSlot = originals.getActiveUniformBlockParameter(program, shader.colorBlock, gl.UNIFORM_BLOCK_BINDING);
        }
        programs.set(program, shader);
        probe.programs.push(shader);
      }
      if (shader?.kind === 'camcorder') return 'camcorder';
      if (shader?.kind === 'farField') return drawCount === 384 ? 'stormSkyDeck' : 'farFieldCards';
      if (shader?.kind === 'cards') {
        // Source-verified geometry signatures for this snapshot; log histograms for auditing.
        // Rain = (240 streaks + 16 curtains)*6; shipped funnel = 12 segments*6 indices.
        if (drawCount === 1536) return 'rainCards';
        if (drawCount === 72) return 'funnelMass';
        const slot = uniformSlots.get(shader.colorSlot);
        const bytes = slot && buffers.get(slot.buffer);
        const offset = (slot?.offset || 0) + shader.colorOffset;
        if (bytes && offset >= 0 && offset + 12 <= bytes.length) {
          const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
          // Unity uploads material colors in linear space in this URP build.
          const toSrgb = value => value <= 0.0031308 ? value * 12.92 : 1.055 * Math.pow(value, 1 / 2.4) - 0.055;
          const r = toSrgb(view.getFloat32(offset, true)), g = toSrgb(view.getFloat32(offset + 4, true));
          if ((Math.abs(r - 0.62) < 0.005 && Math.abs(g - 0.72) < 0.005)
            || (Math.abs(r - 0.72) < 0.005 && Math.abs(g - 0.57) < 0.005)
            || (Math.abs(r - 0.56) < 0.005 && Math.abs(g - 0.34) < 0.005)) return 'tornadoCards';
          if ((Math.abs(r - 0.86) < 0.005 && Math.abs(g - 0.95) < 0.005)
            || (Math.abs(r - 0.74) < 0.005 && Math.abs(g - 0.6) < 0.005)
            || (Math.abs(r - 0.5) < 0.005 && Math.abs(g - 0.32) < 0.005)) return 'windCards';
          probe.materialColors ||= {};
          probe.materialColors[`${r.toFixed(4)},${g.toFixed(4)}`] = true;
          return 'cardsUnclassified';
        }
        probe.missingMaterialBuffer ||= { slot: shader.colorSlot, offset, hasSlot: !!slot,
          bytes: bytes?.length, slots: [...uniformSlots.keys()], targets: [...boundBuffers.keys()] };
        return 'cardsUnclassified';
      }
      const target = framebuffer && fbos.get(framebuffer);
      const size = target?.texture && textures.get(target.texture);
      if (size?.width === 320 && size?.height === 240) return 'pipCamera';
      return 'worldAndUI';
    }
    function begin(category) {
      if (!timer || !probe.frame || probe.frames.length % 5 !== 0) return;
      if (active?.category === category && active.frame === probe.frame) return;
      end();
      const query = originals.createQuery();
      originals.beginQuery(timer.TIME_ELAPSED_EXT, query);
      active = { query, category, frame: probe.frame };
    }
    for (const [name, native] of Object.entries(originals)) {
      if (/^(get|is|create|delete|check)/.test(name)) continue;
      gl[name] = function(...args) {
        if (name === 'shaderSource') shaders.set(args[0], args[1]);
        if (name === 'attachShader') {
          const list = attached.get(args[0]) || [];
          list.push(args[1]); attached.set(args[0], list);
        }
        if (name === 'useProgram') program = args[0];
        if (name === 'uniformBlockBinding') {
          const info = programs.get(args[0]);
          if (info?.kind === 'cards' && info.colorBlock === args[1]) info.colorSlot = args[2];
        }
        if (name === 'bindBuffer') {
          boundBuffers.set(args[0], args[1]);
          if (args[0] === gl.UNIFORM_BUFFER) uniformBuffer = args[1];
        }
        if (name === 'bindBufferBase' && args[0] === gl.UNIFORM_BUFFER) {
          uniformBuffer = args[2];
          boundBuffers.set(gl.UNIFORM_BUFFER, uniformBuffer);
          uniformSlots.set(args[1], { buffer: args[2], offset: 0 });
        }
        if (name === 'bindBufferRange' && args[0] === gl.UNIFORM_BUFFER) {
          uniformBuffer = args[2];
          boundBuffers.set(gl.UNIFORM_BUFFER, uniformBuffer);
          uniformSlots.set(args[1], { buffer: args[2], offset: args[3] });
        }
        if (name === 'bufferData' && boundBuffers.get(args[0])) {
          const source = args[1];
          // Emscripten passes the entire WASM heap plus srcOffset/length in WebGL 2.
          // Track the uploaded slice, rather than mistaking the heap size for this UBO.
          const offset = typeof source === 'number' ? 0 : (args[3] || 0) * source.BYTES_PER_ELEMENT;
          const length = typeof source === 'number' ? source : args[4]
            ? args[4] * source.BYTES_PER_ELEMENT : source.byteLength - offset;
          if (length <= 1048576) {
            const bytes = new Uint8Array(length);
            if (typeof source !== 'number') bytes.set(new Uint8Array(source.buffer, source.byteOffset + offset, length));
            buffers.set(boundBuffers.get(args[0]), bytes);
          }
        }
        if (name === 'bufferSubData' && boundBuffers.get(args[0])) {
          const bytes = buffers.get(boundBuffers.get(args[0])), source = args[2];
          if (bytes && source?.buffer) {
            const offset = (args[3] || 0) * source.BYTES_PER_ELEMENT;
            const length = args[4] ? args[4] * source.BYTES_PER_ELEMENT : source.byteLength - offset;
            bytes.set(new Uint8Array(source.buffer, source.byteOffset + offset, length), args[1]);
          }
        }
        if (name === 'bindFramebuffer' && args[0] !== gl.READ_FRAMEBUFFER) {
          end(); framebuffer = args[1];
        }
        if (name === 'bindTexture' && args[0] === gl.TEXTURE_2D) texture = args[1];
        if (name === 'texStorage2D' && texture) textures.set(texture, { width: args[3], height: args[4] });
        if (name === 'texImage2D' && texture && args.length >= 9) textures.set(texture, { width: args[3], height: args[4] });
        if (name === 'framebufferTexture2D' && framebuffer && args[1] === gl.COLOR_ATTACHMENT0)
          fbos.set(framebuffer, { texture: args[3] });
        const frame = probe.enabled ? probe.frame : null;
        const draw = /^draw(Arrays|Elements)/.test(name);
        const count = draw ? (/^drawArrays/.test(name) ? args[2] : args[1]) : 0;
        const category = frame || (draw && (probe.hideFarField || probe.hideNearCards)) ? classify(count) : null;
        if (frame && draw) {
          const signature = `${category}:${count}`;
          probe.drawSignatures ||= {};
          probe.drawSignatures[signature] = (probe.drawSignatures[signature] || 0) + 1;
        }
        if (draw && category === 'farFieldCards' && (probe.farFieldSamples?.length || 0) < 5) {
          const index = originals.getUniformIndices(program, ['hlslcc_mtx4x4unity_ObjectToWorld[0]'])[0];
          const block = originals.getActiveUniforms(program, [index], gl.UNIFORM_BLOCK_INDEX)[0];
          const offset = originals.getActiveUniforms(program, [index], gl.UNIFORM_OFFSET)[0];
          const binding = originals.getActiveUniformBlockParameter(program, block, gl.UNIFORM_BLOCK_BINDING);
          const slot = uniformSlots.get(binding), bytes = slot && buffers.get(slot.buffer);
          if (bytes && (slot.offset || 0) + offset + 64 <= bytes.length) {
            const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
            const matrix = Array.from({length:16}, (_, i) => view.getFloat32((slot.offset || 0) + offset + i * 4, true));
            const vp = Array.from({length:4}, (_, i) => Array.from(originals.getUniform(program,
              originals.getUniformLocation(program, `hlslcc_mtx4x4unity_MatrixVP[${i}]`)) || [])).flat();
            if (vp?.length === 16) {
              const transform = (m, v) => Array.from({length:4}, (_, row) => v.reduce((sum, value, col) => sum + m[col * 4 + row] * value, 0));
              const world = transform(matrix, [0,0,0,1]);
              const clip = transform(vp, world);
              (probe.farFieldSamples ||= []).push({worldPosition:world.slice(0,3), ndcOrigin:clip.slice(0,3).map(v => v / clip[3])});
            }
          }
        }
        if (draw && probe.hideFarField && category === 'farFieldCards') return;
        if (draw && probe.hideNearCards && ['tornadoCards','windCards','cardsUnclassified'].includes(category)) return;
        if (frame && draw) {
          begin(category);
          frame.drawCalls[category] = (frame.drawCalls[category] || 0) + 1;
        }
        const start = frame ? performance.now() : 0;
        const result = native(...args);
        if (frame) frame.submitMs[category] = (frame.submitMs[category] || 0) + performance.now() - start;
        if (name === 'flush') { end(); poll(); }
        return result;
      };
    }
    probe.contexts.push({ end, poll });
  }

  // AudioParam control submission is measurable on the main thread. This is not DSP time.
  const descriptor = Object.getOwnPropertyDescriptor(AudioParam.prototype, 'value');
  if (descriptor?.set) Object.defineProperty(AudioParam.prototype, 'value', {
    ...descriptor,
    set(value) {
      const start = performance.now();
      descriptor.set.call(this, value);
      if (probe.enabled && probe.frame) probe.frame.audioSubmitMs += performance.now() - start;
    },
  });
  for (const name of ['setValueAtTime', 'setTargetAtTime', 'linearRampToValueAtTime']) {
    const original = AudioParam.prototype[name];
    AudioParam.prototype[name] = function(...args) {
      const start = performance.now();
      const result = original.apply(this, args);
      if (probe.enabled && probe.frame) probe.frame.audioSubmitMs += performance.now() - start;
      return result;
    };
  }
  const sourceStart = AudioBufferSourceNode.prototype.start;
  AudioBufferSourceNode.prototype.start = function(...args) {
    if (probe.audioDiagnostic) {
    let kind = 'other';
    const buffer = this.buffer;
    if (buffer && Math.abs(buffer.duration - 0.25) < 0.001) kind = 'landing-duration';
    if (buffer && Math.abs(buffer.duration - 1) < 0.001) {
      const samples = buffer.getChannelData(0);
      const energy = frequency => {
        let a=0,b=0;
        for(let i=0;i<Math.min(samples.length,4096);i++) {
          const phase=2*Math.PI*frequency*i/buffer.sampleRate;
          a+=samples[i]*Math.sin(phase); b+=samples[i]*Math.cos(phase);
        }
        return a*a+b*b;
      };
      const tones = [55,80,880].map(f=>energy(f));
      kind = ['engine-tone','boost-tone','skid-tone'][tones.indexOf(Math.max(...tones))];
    }
    if (audioNodes.length < 256) {
      const item = {node:this,id:audioNodes.length,kind,active:true}; audioNodes.push(item);
      this.addEventListener('ended',()=>item.active=false,{once:true});
    }
    (probe.audioStarts ||= []).push({time:performance.now(),duration:buffer?.duration,kind});
    }
    probe.audioSources++;
    probe.maxAudioSources = Math.max(probe.maxAudioSources, probe.audioSources);
    this.addEventListener('ended', () => probe.audioSources--, { once: true });
    return sourceStart.apply(this, args);
  };
  probe.start = () => { probe.frames.length = 0; frameStamp = -1; probe.enabled = true; };
  probe.stop = () => {
    probe.enabled = false;
    for (const ctx of probe.contexts) { ctx.end(); ctx.poll(); }
  };
})();
