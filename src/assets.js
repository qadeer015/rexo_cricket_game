/**
 * Procedural art for Night Nets. Later milestones can fetch files here
 * without changing the callers: loadGameAssets() stays the entry point.
 */

function canvas(width, height) {
  const el = document.createElement('canvas');
  el.width = width;
  el.height = height;
  return el;
}

function createGrassCanvas() {
  const c = canvas(256, 256);
  const g = c.getContext('2d');
  g.fillStyle = '#123d26';
  g.fillRect(0, 0, 256, 256);
  for (let i = 0; i < 2200; i += 1) {
    const x = Math.random() * 256;
    const y = Math.random() * 256;
    const tone = 30 + Math.random() * 80;
    g.fillStyle = `rgba(${12 + tone * 0.15}, ${78 + tone}, ${46}, 0.45)`;
    g.fillRect(x, y, 1 + Math.random() * 2, 4 + Math.random() * 6);
  }
  g.fillStyle = 'rgba(0, 0, 0, 0.06)';
  for (let y = 0; y < 256; y += 48) g.fillRect(0, y, 256, 20);
  return c;
}

function createPitchCanvas() {
  const c = canvas(128, 512);
  const g = c.getContext('2d');
  const gradient = g.createLinearGradient(0, 0, 128, 0);
  gradient.addColorStop(0, '#a78455');
  gradient.addColorStop(0.5, '#d7bf8d');
  gradient.addColorStop(1, '#a78455');
  g.fillStyle = gradient;
  g.fillRect(0, 0, 128, 512);
  g.fillStyle = 'rgba(120, 78, 40, 0.18)';
  g.fillRect(48, 0, 32, 512);
  for (let i = 0; i < 400; i += 1) {
    g.fillStyle = `rgba(90, 60, 30, ${Math.random() * 0.25})`;
    g.fillRect(Math.random() * 128, Math.random() * 512, 2, 3);
  }
  g.strokeStyle = 'rgba(255,255,255,0.8)';
  g.lineWidth = 3;
  g.strokeRect(8, 18, 112, 70);
  g.strokeRect(8, 424, 112, 70);
  return c;
}

function createBallCanvas() {
  const c = canvas(128, 128);
  const g = c.getContext('2d');
  g.fillStyle = '#c43131';
  g.fillRect(0, 0, 128, 128);
  g.strokeStyle = '#f6f1e4';
  g.lineWidth = 3;
  g.beginPath();
  g.arc(64, 64, 40, -0.9, 0.9);
  g.stroke();
  g.beginPath();
  g.arc(64, 64, 40, Math.PI - 0.9, Math.PI + 0.9);
  g.stroke();
  g.lineWidth = 2;
  for (let i = -4; i <= 4; i += 1) {
    g.beginPath();
    g.moveTo(64 + Math.sin(-0.5 + i * 0.05) * 40 - 6, 64 + i * 6);
    g.lineTo(64 + Math.sin(-0.5 + i * 0.05) * 40 + 4, 64 + i * 6 - 3);
    g.stroke();
  }
  return c;
}

function createSignCanvas() {
  const c = canvas(512, 160);
  const g = c.getContext('2d');
  g.fillStyle = '#101826';
  g.fillRect(0, 0, 512, 160);
  g.strokeStyle = '#f0b429';
  g.lineWidth = 8;
  g.strokeRect(8, 8, 496, 144);
  g.fillStyle = '#f0b429';
  g.font = '700 54px sans-serif';
  g.textAlign = 'center';
  g.textBaseline = 'middle';
  g.fillText('LANTERN YARD', 256, 62);
  g.fillStyle = '#d5deea';
  g.font = '600 28px sans-serif';
  g.fillText('NIGHT NETS', 256, 112);
  return c;
}

function createLightPoolCanvas() {
  const c = canvas(256, 256);
  const g = c.getContext('2d');
  const glow = g.createRadialGradient(128, 128, 20, 128, 128, 128);
  glow.addColorStop(0, 'rgba(255, 226, 170, 0.55)');
  glow.addColorStop(1, 'rgba(255, 226, 170, 0)');
  g.fillStyle = glow;
  g.fillRect(0, 0, 256, 256);
  return c;
}

function createNetCanvas() {
  const c = canvas(128, 128);
  const g = c.getContext('2d');
  g.clearRect(0, 0, 128, 128);
  g.strokeStyle = 'rgba(232, 236, 242, 0.9)';
  g.lineWidth = 3;
  for (let i = 0; i <= 128; i += 16) {
    g.beginPath();
    g.moveTo(i, 0);
    g.lineTo(i, 128);
    g.stroke();
    g.beginPath();
    g.moveTo(0, i);
    g.lineTo(128, i);
    g.stroke();
  }
  return c;
}

export function loadGameAssets() {
  return Promise.resolve({
    grass: createGrassCanvas(),
    pitch: createPitchCanvas(),
    ball: createBallCanvas(),
    sign: createSignCanvas(),
    lightPool: createLightPoolCanvas(),
    net: createNetCanvas(),
  });
}
