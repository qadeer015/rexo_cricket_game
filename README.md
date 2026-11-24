# Cricket Blitz

Original 3D mobile cricket game for Android. Landscape, 60 fps target, built to stay light on low-end phones. Every team, player, ground and sponsor in the game is fictional.

Milestone 1 is playable from an empty Unity scene: one ground, four fictional sides, a full innings with batting, bowling, fielding and a scoreboard.

## Play it

1. Install **Unity 2022.3 LTS** with the Android Build Support module.
2. Add this folder as a project (`rexo_cricket_game`).
3. Press **Play**. You do not need to place anything in a scene. `BootLoader` builds the menus and the match.

The first launch walks through splash, a consent popup, an optional location prompt, sign-in (guest or a local display name) and a loading screen with tips. A coach then runs a four-step tutorial. After that, **Quick Match** is the match.

### In the editor

| Action | Keys |
| --- | --- |
| Aim a shot | WASD or arrows, or the left joystick |
| Push / Stroke / Loft | J / K / L (hold L to charge) |
| Leave | X |
| Footwork | the buttons, or F to cycle |
| Run / cancel | R / C |
| Delivery type | 1–7 |
| Change side | Q |
| Bowl | Enter |
| Pause | Esc |

On a phone the same actions are the left joystick, the shot buttons, and the bowling pad. Settings can switch aiming to a swipe.

## What Milestone 1 includes

- Splash, consent and privacy note, optional coarse location, guest or local sign-in, loading tips
- Tutorial with numbered tasks, a finger hint, Skip, and a 60–90 coin reward after each task
- Home, Modes, Tournaments, Nets, Profile, Store, Settings
- Team pick (International and Club), ratings, and match settings: ground, difficulty, overs, pitch, skip intro, time of day, rain
- Toss, conditions, lineup names and numbers, batter table
- 2 to 50 overs (2 and 5 open; longer overs and extra grounds unlock with coins)
- Batting, bowling, a field radar, running, run-outs, boundaries, wides, no-balls, free hits, powerplay, a light rain rule
- Scoreboard, umpire calls, a short replay, result, XP and coins
- Local save plus a second file that stands in for cloud save

Multiplayer, leagues and the tournament bracket are described in the menus and are not simulated yet. See `Docs/BUILD_PLAN.md`.

## Project map

```
Assets/CricketBlitz/
  Scripts/Sim/        ball, shots, innings, DLS, AI  (no Unity types)
  Scripts/Match/      MatchManager, BallPhysics
  Scripts/Batting/    BattingController
  Scripts/Bowling/    BowlingController
  Scripts/Fielding/   FielderAI
  Scripts/UI/         MenuFlow, ScoreboardUI, MatchInputUI
  Scripts/Tutorial/   TutorialManager
  Scripts/Save/       SaveSystem, PlayerProfile
  Scripts/World/      stadium, players, camera
  Scripts/Core/       GameManager, BootLoader
  Editor/             Android landscape defaults
Tests/                standalone laws tests (dotnet run)
Docs/                 architecture, UI layouts, milestones, assets
```

The stadium, players and every menu are built in code so the match runs before any art is imported. `Docs/UI_LAYOUTS.md` is the prefab version of those screens for when you replace the generated UI.

## Check the laws without Unity

```
dotnet run --project Tests/CricketBlitz.SimTests.csproj
```

That covers scoring, strike, wides, no-balls, free hits, maidens, chases, the ball's pitch and bounce, and shot timing.

## Phone build

Menu: **Cricket Blitz → Apply Mobile Settings**, then **File → Build Settings → Android**.

- Landscape left, ARM64, IL2CPP, minimum API 25
- Package id `com.harborline.cricketblitz`
- Target frame rate 60, or 30 from Settings
- Low graphics turns shadows and extra lights off

No third-party ad, billing or sign-in SDK is included. The store buttons grant a local practice reward and say so.
