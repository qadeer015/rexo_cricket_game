# Build plan

Each milestone leaves a game you can play. Do not start the next one by breaking the previous match.

## Milestone 1 — this project

One ground built from primitives, four fictional sides, a 5-over exhibition (2, 10, 20 and 50 selectable), batting, bowling, fielding, running, the scoreboard and the front-end flow.

Done when:

- Press Play on an empty scene and reach a match without importing art
- A timed shot on a full ball can be a four or a six, and a miss on the stumps is bowled
- The innings ends on overs, all out, or the target
- Coins and XP survive a restart
- `dotnet run --project Tests/CricketBlitz.SimTests.csproj` passes

## Milestone 2 — feel

Replace primitives with the free packs in `Docs/ASSETS.md`.

- Mixamo batting, bowling, throw, catch, appeal and run clips on the same avatar bones
- Stump break as a short animation, umpire signals as clips
- Bat, ball, stump and crowd audio files, hooked to the methods already on `MatchAudio`
- A real timing bar graphic and shot trails
- LBW: a pad volume in front of the stumps, only when the ball pitched in line and was not left alone outside off
- Byes already exist; add leg-byes off the pad
- Running on wides
- Commentary barks as short audio clips, chosen by the same keys `CommentaryBank` already uses

Keep `BallSim` and `InningsModel`. The new art only follows the transforms.

## Milestone 3 — depth

- Bowler select at the change of over, with the no-consecutive-overs rule already implied by `NextBowler`
- Per-ball field editor saved as named plans
- Batting AI that plays a full second innings with partnerships you can read on the scorecard (the scorecard text is already built)
- Difficulty-specific field plans and slower-ball tricks are in `AiBrains`; tune them against the tests rather than rewriting them
- Daily reward calendar, kit colours that tint `TeamSheet.Color`, and three bats with different timing windows
- A 30-side local knockout that only calls `MatchManager.Launch` and stores winners in the profile
- Replay camera that can be a `RenderTexture` on the big screen when graphics are set to medium

## Milestone 4 — phone services

- Replace `MonetizationStubs` with a real ads and billing package. Keep the practice-reward path for offline.
- Replace the local cloud file in `SaveSystem` with an account save. The profile class stays the payload.
- Google sign-in writes the same `PlayerProfile.userName` the guest path uses.
- Coarse location fills a "nearby clubs" list. Still optional.
- Addressables for the extra grounds so a low-end install does not carry every mesh
- On-device frame-time capture at 60 and 30 on a low-end Android handset. Budget: the ball step, the HUD and one directional light. If a frame spikes, it will be the highlight buffer or the rain particles, both of which already have caps.

## Explicitly out of Milestone 1

Online multiplayer, a league ladder, and the World Cup bracket. The menus describe them and send the player to the exhibition instead of faking a server.
