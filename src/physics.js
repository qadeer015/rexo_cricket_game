/**
 * Ball motion for one delivery and the shot that follows.
 * The bowl is a shaped flight so the ball arrives at a hittable height.
 * After contact, the ball is a simple gravity body. Later milestones can
 * replace the shaped bowl with a full simulation behind the same functions.
 */

export const DELIVERY_TIME = 1.5;
const GRAVITY = 9.8;
const START_Z = -7.8;
const END_Z = 9.15;

export function createBall() {
  return {
    pos: { x: 0, y: 0.05, z: START_Z },
    vel: { x: 0, y: 0, z: 0 },
    u: 0,
    elapsed: 0,
    duration: DELIVERY_TIME,
    mode: 'hold',
    spec: null,
    settled: false,
  };
}

export function beginDelivery(ball, spec) {
  ball.spec = spec;
  ball.elapsed = 0;
  ball.u = 0;
  ball.mode = 'delivery';
  ball.settled = false;
  ball.vel.x = 0;
  ball.vel.y = 0;
  ball.vel.z = 0;
  const p = deliveryPoint(0, spec);
  ball.pos.x = p.x;
  ball.pos.y = p.y;
  ball.pos.z = p.z;
}

export function deliveryPoint(u, spec) {
  const z = START_Z + (END_Z - START_Z) * u;
  const x = spec.lineX * Math.min(1, u / 0.55);
  const bounceU = spec.length === 'full' ? 0.8 : spec.length === 'short' ? 0.5 : 0.66;
  let y;
  if (u < bounceU) {
    const t = u / bounceU;
    y = 2 + (0.05 - 2) * t + Math.sin(t * Math.PI) * 0.22;
  } else {
    const contactY = spec.length === 'short' ? 1.05 : spec.length === 'full' ? 0.42 : 0.74;
    const t = Math.min(1, (u - bounceU) / (0.9 - bounceU));
    const eased = 1 - (1 - t) * (1 - t);
    y = 0.05 + (contactY - 0.05) * eased;
    if (u > 0.9) {
      const k = (u - 0.9) / 0.1;
      y = contactY + (0.32 - contactY) * k;
    }
  }
  return { x, y, z };
}

export function bounceMarker(spec) {
  const bounceU = spec.length === 'full' ? 0.8 : spec.length === 'short' ? 0.5 : 0.66;
  const point = deliveryPoint(bounceU, spec);
  return { x: spec.lineX, z: point.z };
}

/** @returns {boolean} true when the bowl has reached the stumps */
export function stepDelivery(ball, dt) {
  if (ball.mode !== 'delivery') return false;
  ball.elapsed += dt;
  ball.u = Math.min(1, ball.elapsed / ball.duration);
  const p = deliveryPoint(ball.u, ball.spec);
  ball.pos.x = p.x;
  ball.pos.y = p.y;
  ball.pos.z = p.z;
  return ball.u >= 1;
}

export function launchShot(ball, { runs, shot, dirX, dirY, wicket, dismissal }) {
  ball.mode = 'shot';
  ball.settled = false;
  const straight = dirY >= 0 ? 1 : 1 + dirY;
  const lateral = dirX;
  const zDir = -(0.4 + 0.6 * straight);

  if (wicket && dismissal === 'bowled') {
    ball.vel.x = -ball.pos.x * 3;
    ball.vel.y = 0.45;
    ball.vel.z = 11;
    ball.pos.y = Math.max(ball.pos.y, 0.35);
    return;
  }

  let speed = 8;
  let lift = 0.8;
  if (wicket && dismissal === 'caught') {
    speed = 9;
    lift = 7.5;
  } else if (runs >= 6) {
    speed = 18;
    lift = 9.2;
  } else if (runs === 4 && shot === 'loft') {
    speed = 20;
    lift = 6.4;
  } else if (runs === 4) {
    speed = 24;
    lift = 1.7;
  } else if (runs === 2) {
    speed = 15;
    lift = 1.3;
  } else if (runs === 1) {
    speed = 10;
    lift = 1;
  }

  const len = Math.hypot(lateral, zDir) || 1;
  ball.vel.x = (lateral / len) * speed;
  ball.vel.z = (zDir / len) * speed;
  ball.vel.y = lift;
  ball.pos.y = Math.max(ball.pos.y, shot === 'loft' ? 0.85 : 0.5);
}

export function stepShot(ball, dt) {
  if (ball.mode !== 'shot' || ball.settled) return;
  ball.vel.y -= GRAVITY * dt;
  ball.pos.x += ball.vel.x * dt;
  ball.pos.y += ball.vel.y * dt;
  ball.pos.z += ball.vel.z * dt;

  if (ball.pos.y < 0.045) {
    ball.pos.y = 0.045;
    if (Math.abs(ball.vel.y) < 0.85) ball.vel.y = 0;
    else ball.vel.y *= -0.4;
    ball.vel.x *= 0.78;
    ball.vel.z *= 0.82;
  }

  const radial = Math.hypot(ball.pos.x, ball.pos.z);
  if (radial > 27 && ball.pos.y <= 1.25) {
    const scale = 27 / radial;
    ball.pos.x *= scale;
    ball.pos.z *= scale;
    ball.vel.x = 0;
    ball.vel.z = 0;
    ball.vel.y = Math.min(0, ball.vel.y);
    ball.settled = true;
  }
}
