# Free and legal art

Use these as replacements for the primitive stadium. Do not drop in real team shirts, real player faces, or board logos. The names in the game stay the fictional ones.

Check each pack's license on the store page before you ship. "Free" on the Unity Asset Store is not always the same licence, and some packs forbid use in a template you resell.

## Unity Asset Store (free)

Search these titles and confirm the licence is commercial-friendly:

- **Simple Stadium** or **Low Poly Stadium Pack** — stands and a field. Scale the pitch to 20.12 m by 3.05 m and put the striker's stumps at `z = 10.06`.
- **Low Poly Sports Pack** or **Stylized Ball Pack** — a cricket ball, or recolour a generic ball red. Keep the collider off; `BallPhysics` moves it.
- **Floodlight / Street Lamp** free props — four poles. The code already places lights at dusk and night.
- **Flag Pack** free — plain colour flags. Paint Harbor Kings teal and Ridge Falcons rust. Do not use national flags that you treat as a real team.
- **Skybox Series Free** — one sky for afternoon. Night and dusk can stay on the solid camera colour the code already sets, which is cheaper.

Avoid any pack whose preview shows a real league, a real broadcaster's bug, or a real manufacturer's bat logo.

## Mixamo (free with an Adobe account, their standard licence)

Download **Without Skin** if you already have a humanoid, or **With Skin** for the first character, FBX for Unity, in place.

Use the same skeleton for every clip:

| Clip to search | Used for |
| --- | --- |
| Baseball Swing or Bat Swing | batting shot |
| Throwing | bowler release and fielder throw |
| Running | batsmen and the chase |
| Idle | the mark |
| Jump | keeper take |
| Clapping or Cheer | umpire and a wicket |
| Falling | a diving stop, later |

Retarget onto a humanoid Avatar. `PlayerAvatar` can stay as the fallback if the clip is missing: the code poses are what you see today.

## Audio

Record your own bat knock, or use a CC0 library such as **Freesound** (filter to CC0) for:

- bat on ball
- ball on pitch
- stumps
- a short crowd cheer and a looped murmur

Assign the clips to the fields on `MatchAudio`. Until you do, it generates short noises so the match is not silent.

## UI

Stay with Unity UI. A free font with an open licence (for example **Outfit** or **Source Sans**, check the file) replaces `LegacyRuntime`. Do not trace a real broadcast scoreboard. The bottom bar in `Docs/UI_LAYOUTS.md` is the layout.

## What not to import

Real international kits, real player scans, IPL/ICC/BBC/Star marks, manufacturer bat logos, or a Google logo asset. The sign-in button is text on purpose.
