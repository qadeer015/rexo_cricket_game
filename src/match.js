/**
 * One 5-over batting innings. Bowling, field plans, and tournaments
 * belong in later milestones; they should consume snapshot() rather than
 * reach into this state.
 */

export const OVERS = 5;

export const TEAMS = {
  batting: {
    id: 'harbour-lanterns',
    name: 'Harbour Lanterns',
    short: 'Lanterns',
    color: '#f0b429',
    players: [
      'Mira Solen',
      'Joss Hale',
      'Nia Kestrel',
      'Arun Veld',
      'Theo Marsh',
      'Lila Quen',
      'Bram Idris',
      'Sera Quinn',
      'Otto Voss',
      'Hen Calder',
      'Pip Renner',
    ],
  },
  bowling: {
    id: 'cedar-wharf',
    name: 'Cedar Wharf',
    short: 'Wharf',
    color: '#2f9e8a',
    players: [
      'Cal Mercer',
      'Ned Okonkwo',
      'Priya Sant',
      'Hugo Belk',
      'Amira Foss',
      'Soren Peck',
      'Willa Trent',
      'Omar Quill',
      'Bex Harlow',
      'Ida Sen',
      'Tomas Greer',
    ],
  },
};

const LINE_BAG = ['middle', 'middle', 'off', 'leg', 'middle', 'off', 'leg'];
const LENGTH_BAG = ['good', 'good', 'full', 'short', 'good'];

export function createMatch() {
  return {
    battingName: TEAMS.batting.name,
    battingShort: TEAMS.batting.short,
    bowlingName: TEAMS.bowling.name,
    oversLimit: OVERS,
    runs: 0,
    wickets: 0,
    legalBalls: 0,
    batsmen: TEAMS.batting.players.map((name) => ({
      name,
      runs: 0,
      balls: 0,
      out: false,
    })),
    striker: 0,
    nonStriker: 1,
    nextIn: 2,
    events: [],
    inningsOver: false,
    runsAtOverStart: 0,
  };
}

export function formatOvers(legalBalls) {
  return `${Math.floor(legalBalls / 6)}.${legalBalls % 6}`;
}

export function bowlDelivery(random = Math.random) {
  const line = LINE_BAG[Math.floor(random() * LINE_BAG.length)];
  const length = LENGTH_BAG[Math.floor(random() * LENGTH_BAG.length)];
  const lineX = line === 'leg' ? -0.8 : line === 'off' ? 0.8 : 0;
  return { line, length, lineX };
}

export function recentLabels(match, count = 6) {
  return match.events.slice(-count).map((event) => event.label);
}

export function snapshot(match) {
  return {
    v: 1,
    batting: match.battingName,
    bowling: match.bowlingName,
    runs: match.runs,
    wickets: match.wickets,
    legalBalls: match.legalBalls,
    overs: formatOvers(match.legalBalls),
    striker: match.batsmen[match.striker]?.name ?? null,
    nonStriker: match.batsmen[match.nonStriker]?.name ?? null,
    lastBalls: recentLabels(match, 6),
    inningsOver: match.inningsOver,
  };
}

/**
 * Applies one judged ball. Returns over-end info for the banner.
 */
export function applyOutcome(match, outcome) {
  if (match.inningsOver) return { overEnded: false, overRuns: 0 };

  if (outcome.wide) {
    match.runs += outcome.runs;
    match.events.push({ label: 'Wd', runs: outcome.runs, wicket: false, wide: true });
    trimEvents(match);
    return { overEnded: false, overRuns: 0 };
  }

  match.legalBalls += 1;
  const striker = match.batsmen[match.striker];
  striker.balls += 1;

  if (outcome.wicket) {
    match.wickets += 1;
    striker.out = true;
    striker.dismissal = outcome.dismissal;
    match.events.push({ label: 'W', runs: 0, wicket: true, wide: false });
    if (match.wickets >= 10) {
      match.inningsOver = true;
    } else {
      match.striker = match.nextIn;
      match.nextIn += 1;
    }
  } else {
    striker.runs += outcome.runs;
    match.runs += outcome.runs;
    match.events.push({
      label: outcome.runs === 0 ? '•' : String(outcome.runs),
      runs: outcome.runs,
      wicket: false,
      wide: false,
    });
    if (outcome.runs % 2 === 1) swapStrike(match);
  }

  trimEvents(match);

  let overEnded = false;
  let overRuns = 0;
  if (match.legalBalls % 6 === 0) {
    overEnded = true;
    overRuns = match.runs - match.runsAtOverStart;
    match.runsAtOverStart = match.runs;
    if (!match.inningsOver) swapStrike(match);
  }

  if (match.legalBalls >= match.oversLimit * 6) match.inningsOver = true;
  return { overEnded, overRuns };
}

export function resultLine(match) {
  return `${match.battingName} ${match.runs}/${match.wickets} (${formatOvers(match.legalBalls)})`;
}

function swapStrike(match) {
  const swap = match.striker;
  match.striker = match.nonStriker;
  match.nonStriker = swap;
}

function trimEvents(match) {
  if (match.events.length > 36) match.events.splice(0, match.events.length - 36);
}
