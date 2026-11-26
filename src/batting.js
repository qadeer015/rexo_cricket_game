/**
 * Timing meter and shot outcomes for the batting net.
 * Pure functions: no DOM and no rendering. A later bowling mode can feed
 * the same delivery shape without taking over this module.
 */

export const PERFECT_WINDOW = 0.11;
export const METER_SPEED = 0.7;

const FIT = {
  front: { full: 1, good: 0.92, short: 0.58 },
  back: { full: 0.58, good: 0.92, short: 1 },
  advance: { full: 1, good: 0.74, short: 0.42 },
};

const LINE_VALUE = { leg: -1, middle: 0, off: 1 };
const SHOT_NAME = { push: 'Push', stroke: 'Stroke', loft: 'Loft' };

function clamp01(n) {
  return Math.max(0, Math.min(1, n));
}

export function tickMeter(phase, direction, dt) {
  let next = phase + direction * METER_SPEED * dt;
  let dir = direction;
  if (next >= 1) {
    next = 1;
    dir = -1;
  } else if (next <= 0) {
    next = 0;
    dir = 1;
  }
  return { phase: next, direction: dir };
}

export function readTiming(phase) {
  const delta = phase - 0.5;
  const abs = Math.abs(delta);
  const band = abs <= PERFECT_WINDOW ? 'PERFECT' : delta < 0 ? 'EARLY' : 'LATE';
  const contact = Math.max(0, 1 - abs / 0.5);
  return { band, contact, phase };
}

export function judgeLeave(delivery) {
  if (delivery.line === 'middle') {
    return result({
      runs: 0,
      wicket: true,
      dismissal: 'bowled',
      shotName: 'Leave',
      band: null,
    });
  }
  if (delivery.line === 'leg') {
    return result({
      runs: 1,
      wide: true,
      shotName: 'Leave',
      band: null,
    });
  }
  return result({ runs: 0, shotName: 'Leave', band: null });
}

export function judgeNoShot(delivery) {
  if (delivery.line === 'middle') {
    return result({
      runs: 0,
      wicket: true,
      dismissal: 'bowled',
      shotName: 'No shot',
      band: null,
    });
  }
  return result({ runs: 0, shotName: 'No shot', band: null });
}

/**
 * @param {{
 *   shot: 'push'|'stroke'|'loft',
 *   footwork: 'advance'|'back'|'front',
 *   timing: { band: string, contact: number },
 *   direction: { x: number, y: number },
 *   power?: number,
 *   delivery: { line: string, length: string }
 * }} input
 */
export function judgeShot(input) {
  const { shot, footwork, timing, direction, delivery } = input;
  const power = clamp01(input.power ?? 0);
  const fit = FIT[footwork][delivery.length];
  const alignment = 1 - Math.min(1, Math.abs(direction.x - LINE_VALUE[delivery.line]) / 1.4);
  const quality = clamp01(timing.contact * (0.35 + 0.65 * fit));
  const straight = direction.y >= 0 ? 1 : 1 + direction.y;

  let wicket = false;
  let dismissal = null;
  if (timing.band !== 'PERFECT') {
    if (shot === 'loft' && power > 0.5 && quality < 0.42) {
      wicket = true;
      dismissal = 'caught';
    } else if (shot === 'stroke' && quality < 0.22) {
      wicket = true;
      dismissal = alignment < 0.35 ? 'bowled' : 'caught';
    } else if (shot === 'push' && quality < 0.18 && delivery.line === 'middle') {
      wicket = true;
      dismissal = 'bowled';
    } else if (footwork === 'advance' && delivery.length !== 'full' && quality < 0.34) {
      wicket = true;
      dismissal = 'bowled';
    }
  }

  let runs = 0;
  if (!wicket) {
    if (shot === 'push') {
      if (quality > 0.8 && alignment > 0.78 && straight > 0.5) runs = 4;
      else if (quality > 0.68) runs = 2;
      else if (quality > 0.4 || timing.band === 'PERFECT') runs = 1;
    } else if (shot === 'stroke') {
      if (quality > 0.78 && alignment > 0.42) runs = 4;
      else if (quality > 0.55) runs = 2;
      else if (quality > 0.38 || timing.band === 'PERFECT') runs = 1;
    } else if (quality > 0.76 && power >= 0.62 && alignment > 0.28) {
      runs = 6;
    } else if (quality > 0.64 && power >= 0.35) {
      runs = 4;
    } else if (quality > 0.42 || timing.band === 'PERFECT') {
      runs = power >= 0.45 ? 2 : 1;
    }

    if (
      footwork === 'advance' &&
      delivery.length === 'full' &&
      quality > 0.8 &&
      runs > 0 &&
      runs < 4 &&
      shot !== 'push'
    ) {
      runs = 4;
    }
  }

  return result({
    runs,
    wicket,
    dismissal,
    shotName: SHOT_NAME[shot],
    shot,
    band: timing.band,
    quality,
    power,
  });
}

function result(partial) {
  return {
    runs: partial.runs ?? 0,
    wicket: Boolean(partial.wicket),
    wide: Boolean(partial.wide),
    dismissal: partial.dismissal ?? null,
    shotName: partial.shotName ?? '',
    shot: partial.shot ?? null,
    band: partial.band ?? null,
    quality: partial.quality ?? 0,
    power: partial.power ?? 0,
  };
}
