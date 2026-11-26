/**
 * HTML overlay: scoreboard, timing meter, joystick mount, and shot controls.
 * Rendering stays in the stadium module. This file only paints the DOM.
 */

import { PERFECT_WINDOW } from './batting.js';
import { formatOvers, recentLabels } from './match.js';

const LINE_LABEL = { leg: 'LEG', middle: 'MIDDLE', off: 'OFF' };
const LENGTH_LABEL = { full: 'FULL', good: 'GOOD', short: 'SHORT' };

export function createUI(root) {
  root.innerHTML = `
    <header class="scoreboard">
      <div class="score-row">
        <div class="team-block">
          <p class="eyebrow"><span class="brand">Night Nets</span> · Lantern Yard</p>
          <h1 class="team" data-testid="team">Harbour Lanterns</h1>
          <p class="versus">batting against <span data-testid="opponent">Cedar Wharf</span></p>
        </div>
        <div class="totals">
          <p class="score" data-testid="score" data-runs="0" data-wickets="0">0/0</p>
          <p class="overs">Overs <span data-testid="overs">0.0</span> / 5</p>
        </div>
      </div>
      <div class="batters" data-testid="batsmen"></div>
      <div class="balls" data-testid="last-balls"></div>
    </header>
    <p class="callout" data-testid="callout" hidden></p>
    <p class="banner" data-testid="banner" hidden></p>
    <div class="endcard" data-testid="endcard" hidden>
      <p class="eyebrow">Night Nets</p>
      <h2 data-testid="end-title">Innings closed</h2>
      <p class="end-score" data-testid="end-score"></p>
      <button type="button" class="btn again" data-action="restart">Play again</button>
    </div>
    <footer class="dock">
      <div class="joy" data-testid="joystick" aria-label="Shot direction">
        <span class="joy-knob"></span>
        <span class="joy-hint">Aim</span>
      </div>
      <div class="center-readout">
        <p class="help" data-testid="help">Hold a direction, set your feet, and play as the bar crosses the green.</p>
        <div class="readout-row">
          <p class="delivery" data-testid="delivery">Waiting</p>
          <p class="shot-name" data-testid="shot-name"></p>
          <p class="timing" data-testid="timing" data-band="">—</p>
        </div>
        <div class="meter" data-testid="meter">
          <div class="meter-zone"></div>
          <div class="meter-needle"></div>
        </div>
        <div class="power" data-testid="power" hidden>
          <span>Power</span>
          <div class="power-track"><div class="power-fill"></div></div>
        </div>
      </div>
      <div class="actions">
        <div class="shots">
          <button type="button" class="btn shot" data-shot="push">Push <kbd>J</kbd></button>
          <button type="button" class="btn shot" data-shot="stroke">Stroke <kbd>K</kbd></button>
          <button type="button" class="btn shot loft" data-shot="loft">Loft <kbd>L</kbd></button>
        </div>
        <div class="feet">
          <button type="button" class="btn foot is-on" data-foot="front">Front Foot <kbd>1</kbd></button>
          <button type="button" class="btn foot" data-foot="back">Back Foot <kbd>2</kbd></button>
          <button type="button" class="btn foot" data-foot="advance">Advance <kbd>3</kbd></button>
          <button type="button" class="btn leave" data-action="leave">Leave <kbd>4</kbd></button>
        </div>
      </div>
    </footer>
  `;

  const scoreEl = root.querySelector('[data-testid="score"]');
  const oversEl = root.querySelector('[data-testid="overs"]');
  const teamEl = root.querySelector('[data-testid="team"]');
  const opponentEl = root.querySelector('[data-testid="opponent"]');
  const batsmenEl = root.querySelector('[data-testid="batsmen"]');
  const ballsEl = root.querySelector('[data-testid="last-balls"]');
  const calloutEl = root.querySelector('[data-testid="callout"]');
  const bannerEl = root.querySelector('[data-testid="banner"]');
  const endcardEl = root.querySelector('[data-testid="endcard"]');
  const endTitleEl = root.querySelector('[data-testid="end-title"]');
  const endScoreEl = root.querySelector('[data-testid="end-score"]');
  const helpEl = root.querySelector('[data-testid="help"]');
  const deliveryEl = root.querySelector('[data-testid="delivery"]');
  const shotNameEl = root.querySelector('[data-testid="shot-name"]');
  const timingEl = root.querySelector('[data-testid="timing"]');
  const meterEl = root.querySelector('[data-testid="meter"]');
  const zoneEl = root.querySelector('.meter-zone');
  const needleEl = root.querySelector('.meter-needle');
  const powerEl = root.querySelector('[data-testid="power"]');
  const powerFillEl = root.querySelector('.power-fill');
  const joystick = root.querySelector('[data-testid="joystick"]');
  const knob = root.querySelector('.joy-knob');
  const footButtons = [...root.querySelectorAll('[data-foot]')];

  zoneEl.style.left = `${(0.5 - PERFECT_WINDOW) * 100}%`;
  zoneEl.style.width = `${PERFECT_WINDOW * 2 * 100}%`;

  const api = {
    joystick,
    knob,
    onShot: () => {},
    onLoftStart: () => {},
    onLoftEnd: () => {},
    onFootwork: () => {},
    onLeave: () => {},
    onRestart: () => {},
  };

  root.addEventListener('contextmenu', (event) => event.preventDefault());

  root.addEventListener('pointerdown', (event) => {
    const shot = event.target.closest('[data-shot]');
    if (!shot) return;
    event.preventDefault();
    if (shot.dataset.shot === 'loft') {
      shot.setPointerCapture(event.pointerId);
      api.onLoftStart();
      return;
    }
    api.onShot(shot.dataset.shot);
  });

  root.addEventListener('pointerup', (event) => {
    const shot = event.target.closest('[data-shot="loft"]');
    if (!shot) return;
    api.onLoftEnd();
  });
  root.addEventListener('pointercancel', (event) => {
    if (event.target.closest('[data-shot="loft"]')) api.onLoftEnd();
  });

  root.addEventListener('click', (event) => {
    const foot = event.target.closest('[data-foot]');
    if (foot) {
      setFootwork(foot.dataset.foot);
      api.onFootwork(foot.dataset.foot);
      return;
    }
    if (event.target.closest('[data-action="leave"]')) api.onLeave();
    if (event.target.closest('[data-action="restart"]')) api.onRestart();
  });

  const held = new Set();
  window.addEventListener('keydown', (event) => {
    if (event.repeat || held.has(event.code)) return;
    if (event.code === 'KeyJ') api.onShot('push');
    else if (event.code === 'KeyK') api.onShot('stroke');
    else if (event.code === 'KeyL') {
      held.add(event.code);
      api.onLoftStart();
    } else if (event.code === 'Digit1') setFoot('front');
    else if (event.code === 'Digit2') setFoot('back');
    else if (event.code === 'Digit3') setFoot('advance');
    else if (event.code === 'Digit4') api.onLeave();
  });
  window.addEventListener('keyup', (event) => {
    held.delete(event.code);
    if (event.code === 'KeyL') api.onLoftEnd();
  });

  function setFoot(name) {
    setFootwork(name);
    api.onFootwork(name);
  }

  function setFootwork(name) {
    footButtons.forEach((button) => {
      button.classList.toggle('is-on', button.dataset.foot === name);
    });
  }

  let calloutTimer = 0;

  return {
    joystick,
    knob,
    handlers: api,
    setScoreboard(match) {
      teamEl.textContent = match.battingName;
      opponentEl.textContent = match.bowlingName;
      scoreEl.textContent = `${match.runs}/${match.wickets}`;
      scoreEl.dataset.runs = String(match.runs);
      scoreEl.dataset.wickets = String(match.wickets);
      scoreEl.classList.remove('flash');
      void scoreEl.offsetWidth;
      scoreEl.classList.add('flash');
      oversEl.textContent = formatOvers(match.legalBalls);
      const striker = match.batsmen[match.striker];
      const partner = match.batsmen[match.nonStriker];
      batsmenEl.replaceChildren(batterNode(striker, true), batterNode(partner, false));
      ballsEl.replaceChildren(
        ...recentLabels(match, 6).map((label) => {
          const pill = document.createElement('span');
          pill.className = `pill${label === 'W' ? ' out' : ''}${label === '4' || label === '6' ? ' boundary' : ''}`;
          pill.textContent = label;
          return pill;
        }),
      );
    },
    setDelivery(delivery) {
      if (!delivery) {
        deliveryEl.textContent = 'Waiting';
        return;
      }
      deliveryEl.textContent = `${LENGTH_LABEL[delivery.length]} · ${LINE_LABEL[delivery.line]}`;
    },
    setTiming(phase, band, locked) {
      needleEl.style.left = `${phase * 100}%`;
      timingEl.textContent = band ?? '—';
      timingEl.dataset.band = band ?? '';
      timingEl.classList.toggle('is-locked', Boolean(locked));
    },
    setShotName(name) {
      shotNameEl.textContent = name ?? '';
    },
    setPower(amount) {
      if (amount == null) {
        powerEl.hidden = true;
        return;
      }
      powerEl.hidden = false;
      powerFillEl.style.width = `${Math.round(amount * 100)}%`;
    },
    showCallout(text, kind) {
      calloutEl.hidden = false;
      calloutEl.textContent = text;
      calloutEl.className = `callout ${kind}`;
      calloutEl.style.animation = 'none';
      void calloutEl.offsetWidth;
      calloutEl.style.animation = '';
      window.clearTimeout(calloutTimer);
      calloutTimer = window.setTimeout(() => {
        calloutEl.hidden = true;
      }, 1100);
    },
    showBanner(text) {
      bannerEl.hidden = !text;
      bannerEl.textContent = text ?? '';
    },
    hideHelp() {
      helpEl.hidden = true;
    },
    showEnd(title, line) {
      endcardEl.hidden = false;
      endTitleEl.textContent = title;
      endScoreEl.textContent = line;
    },
    hideEnd() {
      endcardEl.hidden = true;
    },
    setLive(live) {
      root.classList.toggle('is-live', live);
    },
  };
}

function batterNode(batter, onStrike) {
  const el = document.createElement('p');
  el.className = `batter${onStrike && !batter.out ? ' on-strike' : ''}${batter.out ? ' is-out' : ''}`;
  const name = document.createElement('span');
  name.textContent = `${onStrike && !batter.out ? '* ' : ''}${batter.name}`;
  const score = document.createElement('span');
  score.textContent = batter.out ? `${batter.runs} out` : `${batter.runs} (${batter.balls})`;
  el.append(name, score);
  return el;
}
