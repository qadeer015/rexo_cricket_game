# Architecture

Cricket Blitz is one Unity scene's worth of objects, created at runtime. The laws of the match do not depend on Unity, so they can be tested with `dotnet`.

## Layers

```
MenuFlow / ScoreboardUI / MatchInputUI     screens and touch
        |
MatchManager                               one ball at a time
        |
BattingController  BowlingController  FielderAI  BallPhysics
        |
ShotResolver  AiBrains  BallSim  InningsModel  DlsLite
        |
SaveSystem  GameManager                        profile, coins, energy
```

`Assets/CricketBlitz/Scripts/Sim` uses only `System`. `BallPhysics` is the Unity object that moves the mesh and calls `BallSim` in 8 ms slices, so a long frame cannot skip the stumps or the rope. `InningsModel.Commit` is the only place a ball changes the score.

## Match phases

`Intro → Toss → Walkout → Select batter → Setup → Run-up → Ball in play → Umpire → optional replay → next ball`

The second innings starts after the break. Nets stop after the first innings. The tutorial uses the same loop with a slow half-volley and gates the first ball until the aiming tasks are done.

## One delivery

1. The bowler, or `AiBrains`, fills a `DeliveryPlan`: speed, line, length, swing or cut, over or round.
2. `BallSim.Release` aims the release so a straight ball pitches on the marker. Swing and the pitch type move it in the air. Cutters kick sideways after the bounce. A hard pitch keeps more pace off the surface than a dry one.
3. The batter releases Push, Stroke or Loft. That moment is compared with the ball arriving at the crease. `ShotResolver` widens the perfect window with the Timing stat and the difficulty, and it punishes the wrong footwork for the length. Technique scales the speed of the shot. The aim angle names it: straight drive, cover drive, pull, sweep, and the rest.
4. After contact, fielders chase. A catch is a ball still in the air inside a fielder's reach. A throw at the stumps while a runner is short is a run-out.
5. `InningsModel` applies runs, the strike, the over, the free-hit flag and the wicket. A no-ball or a free hit cannot dismiss the batter except by run-out.

## Why the ground is built from primitives

Milestone 1 has to run on a low-end phone before any asset pack is imported. `StadiumBuilder` makes the oval, pitch, stumps, stands, sponsor boards and floodlights, and shares one material per colour. Shadows are off on the low setting. The crowd is a handful of capsules. Real meshes replace those objects later without changing the simulation: the pitch stays on Z, the striker's stumps stay at `z = 10.06`, and the rope stays at 56 metres.

## Save

`PlayerProfile` is JSON in `Application.persistentDataPath`. `SaveSystem` writes the same JSON to a second file and, on load, keeps the copy with more career progress. That second file is the stand-in for a cloud account. Ads and IAP are local stubs in `MonetizationStubs`; the buttons say the reward is a placeholder.

## Performance choices

- The ball is kinematic. PhysX is not in the hot path.
- No per-frame allocations in the ball step. The highlight buffer is capped.
- One directional light. Floodlights are point lights and only at dusk or night on medium graphics.
- UI is a few canvases. The scoreboard refreshes from the innings model instead of rebuilding widgets.
- `Application.targetFrameRate` is 60, with a 30 fps option in Settings.
- VSync is off so the frame cap is the one the game asked for.

## Fictional content

Teams: Harbor Kings, Ridge Falcons, Dockyard XI, Highline CC.
Grounds: Harbor Oval, North Wharf, Lantern Park, Redrock Ground.
Sponsors on the boards: Nimbus Cola, PeakForge, Lumen Bank, Vivid Tel, Orbit Grain, Northwind Air, Marble Mint, Cinder Rail, Harborline.
The coach is Jalen Crowe. None of these are real people, clubs or brands used as logos.
