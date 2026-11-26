# Night Nets

Mobile-first night cricket in the browser. Milestone 1 is a playable 5-over batting innings at the fictional Lantern Yard: Harbour Lanterns bat, Cedar Wharf bowl, and you time each shot.

The ground, kits, and names are original. Art is drawn in the browser. Nothing here uses a real team, player, or competition.

## Run

```bash
npm install
npm run dev
```

Open the local URL Vite prints, usually `http://127.0.0.1:5173`. Use a landscape window. On a phone, rotate to landscape.

## Play

- Left stick, or hold the arrow keys or WASD: shot direction. Up is down the ground, left is leg, right is off.
- Push, Stroke, or hold Loft. The power bar fills while Loft is held, and the shot plays when you let go.
- Footwork: Front Foot, Back Foot, or Advance. Leave shoulders arms. A leave on middle is bowled, a leave on leg is a wide, and a leave on off is a dot.
- The bar starts each ball on the left and sweeps right. Play in the green band. The label reads EARLY, PERFECT, or LATE.
- A middle ball you let go without a shot is bowled. A leave on leg is a wide, and a leave on off is a dot.
- Desktop keys: J push, K stroke, hold L to loft, 1 front foot, 2 back foot, 3 advance, 4 leave.

The scoreboard shows the batting side, wickets, overs, the two batters, and the last balls. The innings ends after 5 overs or when the side is all out. Play again starts another net.

```bash
npm run build
npm run preview
```

## Modules

`src/physics.js` moves the ball, `src/batting.js` times the shot, `src/match.js` keeps the innings, `src/ui.js` draws the overlay, `src/assets.js` builds the art, and `src/save.js` stores a snapshot in `localStorage`. Milestone 1 always starts a new innings.
