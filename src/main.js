/**
 * Night Nets milestone 1. One batting innings at Lantern Yard.
 * Phases: between balls, live delivery, shot flight, innings over.
 * Later milestones should add modes beside these modules, not inside them.
 */

import { loadGameAssets } from './assets.js';
import {
  judgeLeave,
  judgeNoShot,
  judgeShot,
  readTiming,
  tickMeter,
} from './batting.js';
import { createInput } from './input.js';
import {
  applyOutcome,
  bowlDelivery,
  createMatch,
  resultLine,
  snapshot,
} from './match.js';
import {
  beginDelivery,
  bounceMarker,
  createBall,
  launchShot,
  stepDelivery,
  stepShot,
} from './physics.js';
import { saveMatch } from './save.js';
import { createStadium } from './stadium.js';
import { createUI } from './ui.js';

const POWER_TIME = 0.8;
const view = document.querySelector('#view');
const hud = document.querySelector('#hud');

async function boot() {
  const assets = await loadGameAssets();
  const stadium = createStadium(view, assets);
  const ui = createUI(hud);
  const input = createInput(ui.joystick, ui.knob);

  let match = createMatch();
  let footwork = 'front';
  let status = 'between';
  let between = 0.45;
  const ball = createBall();
  let delivery = null;
  let committed = null;
  let commitU = 0;
  let aim = { x: 0, y: 0 };
  let lofting = false;
  let loftTime = 0;
  let meter = 0;
  let meterDir = 1;
  let resolveTime = 0;
  let bannerTime = 0;

  ui.handlers.onFootwork = (name) => {
    footwork = name;
  };
  ui.handlers.onShot = (shot) => commitShot(shot, 0);
  ui.handlers.onLoftStart = () => {
    if (status !== 'live' || committed || lofting) return;
    lofting = true;
    loftTime = 0;
    ui.setShotName('Loft');
  };
  ui.handlers.onLoftEnd = () => {
    if (!lofting) return;
    const power = Math.min(1, loftTime / POWER_TIME);
    lofting = false;
    ui.setPower(null);
    commitShot('loft', power);
  };
  ui.handlers.onLeave = () => {
    if (status !== 'live' || committed || !delivery) return;
    finishCommit(judgeLeave(delivery));
  };
  ui.handlers.onRestart = () => startMatch();

  function startMatch() {
    match = createMatch();
    saveMatch(snapshot(match));
    delivery = null;
    committed = null;
    lofting = false;
    status = 'between';
    between = 0.4;
    ui.hideEnd();
    ui.showBanner('');
    ui.setShotName('');
    ui.setDelivery(null);
    ui.setPower(null);
    ui.setScoreboard(match);
    ui.setLive(false);
    stadium.resetStumps();
    stadium.setMarker(null);
    stadium.setBat(0);
    stadium.setBowler(0);
  }

  function beginBall() {
    delivery = bowlDelivery();
    beginDelivery(ball, delivery);
    committed = null;
    commitU = 0;
    aim = { x: 0, y: 0 };
    lofting = false;
    loftTime = 0;
    meter = Math.random() * 0.12;
    meterDir = 1;
    status = 'live';
    stadium.resetStumps();
    stadium.setBowler(0);
    stadium.setBat(0);
    ui.setLive(true);
    ui.setDelivery(delivery);
    ui.setShotName('');
    ui.setPower(null);
    ui.showBanner('');
    ui.setTiming(meter, readTiming(meter).band, false);
  }

  function commitShot(shot, power) {
    if (status !== 'live' || committed || !delivery) return;
    const outcome = judgeShot({
      shot,
      footwork,
      timing: readTiming(meter),
      direction: input.getDirection(),
      power,
      delivery,
    });
    finishCommit(outcome);
  }

  function finishCommit(outcome) {
    committed = outcome;
    commitU = Math.min(ball.u, 0.89);
    aim = input.getDirection();
    lofting = false;
    ui.setPower(null);
    ui.setShotName(outcome.shotName);
    ui.setTiming(meter, outcome.band, true);
    ui.hideHelp();
    if (ball.u >= 0.9) resolveContact();
  }

  function resolveContact() {
    if (status !== 'live' || !committed) return;
    const info = applyOutcome(match, committed);
    saveMatch(snapshot(match));
    ui.setScoreboard(match);
    const [text, kind] = calloutFor(committed);
    ui.showCallout(text, kind);
    if (committed.shot) {
      launchShot(ball, {
        runs: committed.runs,
        shot: committed.shot,
        dirX: aim.x,
        dirY: aim.y,
        wicket: committed.wicket,
        dismissal: committed.dismissal,
      });
    }
    if (committed.wicket && committed.dismissal === 'bowled' && committed.shot) {
      stadium.dropStumps();
    }
    status = 'resolve';
    resolveTime = 0;
    ui.setLive(false);
    stadium.setMarker(null);
    if (info.overEnded && !match.inningsOver) {
      bannerTime = 1.6;
      ui.showBanner(`End of over ${Math.floor(match.legalBalls / 6)} · ${info.overRuns} runs`);
    }
  }

  function update(dt) {
    let steppedDelivery = false;

    if (status === 'between') {
      between -= dt;
      stadium.setBowler(0);
      stadium.setBat(0);
      if (between <= 0) beginBall();
    }

    if (status === 'live') {
      const moved = tickMeter(meter, meterDir, dt);
      if (!committed) {
        meter = moved.phase;
        meterDir = moved.direction;
        ui.setTiming(meter, readTiming(meter).band, false);
      } else {
        ui.setTiming(meter, committed.band, true);
      }

      if (lofting) {
        loftTime += dt;
        ui.setPower(Math.min(1, loftTime / POWER_TIME));
        ui.setShotName('Loft');
      }

      stadium.setBowler(Math.min(1, ball.elapsed / 0.5));
      stepDelivery(ball, dt);
      steppedDelivery = true;
      stadium.setMarker(bounceMarker(delivery));
      stadium.setBat(committed ? swingAmount(ball.u, commitU) : 0);

      if (lofting && ball.u >= 0.9) {
        const power = Math.min(1, loftTime / POWER_TIME);
        lofting = false;
        ui.setPower(null);
        commitShot('loft', power);
      } else if (!committed && ball.u >= 0.995) {
        finishCommit(judgeNoShot(delivery));
      } else if (committed && ball.u >= 0.9) {
        resolveContact();
      }
    }

    if (status === 'resolve') {
      resolveTime += dt;
      if (ball.mode === 'shot') stepShot(ball, dt);
      else if (!steppedDelivery) stepDelivery(ball, dt);
      if (committed?.wicket && committed.dismissal === 'bowled' && !committed.shot && ball.u > 0.97) {
        stadium.dropStumps();
      }
      stadium.setBat(Math.max(0, 1 - resolveTime / 0.4));
      if (bannerTime > 0) {
        bannerTime -= dt;
        if (bannerTime <= 0) ui.showBanner('');
      }
      const wait = match.inningsOver ? 1.3 : bannerTime > 0 ? 1.45 : 1.05;
      if (resolveTime >= wait) {
        if (match.inningsOver) {
          status = 'over';
          ui.showEnd(match.wickets >= 10 ? 'All out' : 'Innings closed', resultLine(match));
          ui.setDelivery(null);
          ui.setLive(false);
        } else {
          status = 'between';
          between = 0.35;
        }
      }
    }

    stadium.setBall(ball.pos.x, ball.pos.y, ball.pos.z);
    stadium.setAim(status === 'live' && !committed ? input.getDirection() : aim);
    hud.dataset.status = status;
  }

  let last = performance.now();
  function frame(now) {
    const dt = Math.min(0.033, (now - last) / 1000);
    last = now;
    if (status !== 'over') update(dt);
    else stadium.setAim(input.getDirection());
    stadium.render();
    requestAnimationFrame(frame);
  }

  startMatch();
  document.body.dataset.ready = 'true';
  requestAnimationFrame(frame);
}

function swingAmount(u, from) {
  const span = Math.max(0.05, 0.9 - from);
  return Math.max(0, Math.min(1, (u - from) / span));
}

function calloutFor(outcome) {
  if (outcome.wicket) return [outcome.dismissal === 'caught' ? 'CAUGHT' : 'BOWLED', 'out'];
  if (outcome.wide) return ['WIDE', 'wide'];
  if (outcome.runs >= 6) return ['SIX', 'six'];
  if (outcome.runs === 4) return ['FOUR', 'four'];
  if (outcome.runs === 0) return ['DOT', 'dot'];
  return [String(outcome.runs), 'run'];
}

boot().catch((error) => {
  console.error(error);
  const message = document.createElement('p');
  message.className = 'boot-error';
  message.textContent = 'Night Nets could not start the pitch in this browser.';
  hud.replaceChildren(message);
});
