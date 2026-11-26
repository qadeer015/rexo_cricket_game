/**
 * Shot direction. The left stick uses pointer events. Desktop uses
 * arrow keys or WASD. Aim is held briefly after release so a thumb
 * can come off the stick and still play the shot.
 */

const DIRECTION_CODES = new Set([
  'ArrowUp',
  'ArrowDown',
  'ArrowLeft',
  'ArrowRight',
  'KeyW',
  'KeyA',
  'KeyS',
  'KeyD',
]);

export function createInput(pad, knob) {
  const keys = new Set();
  let pointerId = null;
  let stick = { x: 0, y: 0 };
  let latched = { x: 0, y: 0 };
  let latchUntil = 0;

  function keyboardDir() {
    let x = 0;
    let y = 0;
    if (keys.has('ArrowLeft') || keys.has('KeyA')) x -= 1;
    if (keys.has('ArrowRight') || keys.has('KeyD')) x += 1;
    if (keys.has('ArrowUp') || keys.has('KeyW')) y += 1;
    if (keys.has('ArrowDown') || keys.has('KeyS')) y -= 1;
    return clampStick(x, y);
  }

  function draw() {
    const travel = pad.clientWidth * 0.3;
    knob.style.transform = `translate(calc(-50% + ${stick.x * travel}px), calc(-50% + ${-stick.y * travel}px))`;
  }

  function fromEvent(event) {
    const rect = pad.getBoundingClientRect();
    const dx = (event.clientX - (rect.left + rect.width / 2)) / (rect.width / 2);
    const dy = ((rect.top + rect.height / 2) - event.clientY) / (rect.height / 2);
    stick = clampStick(dx, dy);
    draw();
  }

  function endPointer(event) {
    if (pointerId !== null && event.pointerId !== pointerId) return;
    if (stick.x !== 0 || stick.y !== 0) {
      latched = stick;
      latchUntil = performance.now() + 450;
    }
    pointerId = null;
    stick = { x: 0, y: 0 };
    draw();
  }

  pad.addEventListener('pointerdown', (event) => {
    pointerId = event.pointerId;
    pad.setPointerCapture(event.pointerId);
    fromEvent(event);
    event.preventDefault();
  });
  pad.addEventListener('pointermove', (event) => {
    if (event.pointerId !== pointerId) return;
    fromEvent(event);
  });
  pad.addEventListener('pointerup', endPointer);
  pad.addEventListener('pointercancel', endPointer);

  window.addEventListener('keydown', (event) => {
    if (!DIRECTION_CODES.has(event.code)) return;
    event.preventDefault();
    keys.add(event.code);
  });
  window.addEventListener('keyup', (event) => {
    keys.delete(event.code);
  });
  window.addEventListener('blur', () => {
    keys.clear();
    pointerId = null;
    stick = { x: 0, y: 0 };
    draw();
  });

  return {
    getDirection() {
      const keyed = keyboardDir();
      if (keyed.x !== 0 || keyed.y !== 0) return keyed;
      if (stick.x !== 0 || stick.y !== 0) return { ...stick };
      if (performance.now() < latchUntil) return { ...latched };
      return { x: 0, y: 0 };
    },
  };
}

function clampStick(x, y) {
  const mag = Math.hypot(x, y);
  if (mag <= 1) return { x, y };
  return { x: x / mag, y: y / mag };
}
