# UI layouts

Milestone 1 builds every screen in code (`MenuFlow`, `ScoreboardUI`, `MatchInputUI`) so you can press Play with no prefabs. When you replace that with prefabs, use this layout. Canvas scaler: **Scale With Screen Size**, reference **1920×1080**, match **0.5**. Landscape only. Keep tap targets at least 64×64.

Colour: navy `#0E1824`, gold `#F2BD38`, ink `#F0F4F6`. One font. No real league marks.

Safe area: pad the root panel by `Screen.safeArea` so the joystick sits inside a notch.

## 1. Splash

Full-screen navy. Centered title **CRICKET BLITZ**, subtitle **Harborline Games**. No button. About 1.3 seconds, then the next required step.

## 2. Consent

Card in the middle.

- Title: Before you play
- Body: local save, optional personalized ads
- Toggle: Allow personalized ads
- Button: Privacy policy (opens the privacy card)
- Button: **OK** (required to continue; the toggle may be off)

## 3. Location

Optional. **Allow** and **Not now**. Allow calls the platform coarse-location permission on Android. The match never blocks on it.

## 4. Sign-in

- Short note that Google sign-in is not connected and play is offline
- Name field
- **Sign in with Google** (stores the typed name on device; there is no Google logo asset)
- **Play as guest**

## 5. Loading

Title, a horizontal progress bar, and one tip at a time. Tips include "Hard pitch offers more bounce." The bar is driven by the load timer in Milestone 1; later it can follow a real `AsyncOperation`.

## 6. Tutorial overlay (on the match)

- Top right: **Skip Tutorial**
- Numbered hint with a downward marker on the control for the task
- Coach line from Jalen Crowe
- After each task, a modal: task text, coin amount (60, 70, 80, 90), **Collect Reward**
- Last modal: **Ready for challenge mode?** Yes / No

Tasks: drag the joystick, tap Stroke, time a shot, tap RUN.

## 7. Main menu

**Top bar** (left to right): level, XP `0/100`, username, a coloured initial for the avatar, coins, tickets, energy.

**Tab bar:** Home, Modes, Tournaments, Nets, Profile, Store, Settings.

**Home tiles, two rows of three:**

1. Quick Match
2. Multiplayer
3. League
4. Custom Tournament
5. World Cup
6. Store / Blitz Pass

A daily-reward button sits on the right when 22 hours have passed.

Modes and tournament tiles explain the offline exhibition and do not pretend a server is online.

## 8. Team selection

Two columns, International then Club. Each card: name, category, Bat / Bowl / Field as percentages. Tapping a card sets your side. **Cycle opposition** picks the other side. **Next** opens match settings.

## 9. Match settings

One button per row, highlight the active value:

| Row | Values |
| --- | --- |
| Stadium | Harbor Oval (free), North Wharf (4,000), Lantern Park (8,000), Redrock Ground (15,000) |
| Difficulty | Easy, Medium, Hard, Expert, Hardcore, Custom |
| Overs | 2, 5, 10, 20, 50 (10 / 20 / 50 show a lock and a coin price) |
| Pitch | Hard, Dry, Green |
| Skip intro | Yes / No |
| Time of day | Morning, Afternoon, Evening, Dusk, Night |
| Rain | 0, 25, 50, 75, 100 |

**Play** spends 1 energy. Nets and the tutorial do not.

## 10. Toss, lineup, batter

- Banner: `Live from <stadium>`, both sides, pitch, weather, temperature, wind, rain chance
- Toss card: Heads / Tails, then Bat / Bowl if the call wins
- Walk-out: world-space jersey numbers on the eleven
- Batter table columns: **Name, Hand, Technique, Timing, Type**. Two taps pick the openers, then **Confirm**. A later wicket uses the same table for the next batter. Stats are 0–100 and feed `ShotResolver`.

## 11. Match HUD

**Top:** timing bar with a needle, the words EARLY / PERFECT / LATE / NOW, the shot name under it, badges for POWERPLAY, FREE HIT and RAIN. Top left: round field radar and the field name (Attacking 1–5, Balanced, Defensive). Top right: Cam, Pause, and Skip during the tutorial.

**Bottom bar:** team colour, short name, striker marked with `*` and `runs(balls)`, the other batter, center score `HBR 6-1`, overs `0.3`, innings number, bowler figures, six pips for the over, ball speed in km/h.

**Batting (right side):** Push, Stroke, Loft. Under them: Advance, Back Foot, Front Foot, Leave. A vertical power bar fills while Loft is held.

**After the shot:** those buttons hide. **RUN** and **RUN CANCEL** replace them.

**Bowling:** seven delivery icons (Straight, Out-swing, In-swing, Leg-cutter, Off-cutter, Slower, Reverse), Change Side, a ball icon you drag for spin, **OK Bowl**. The joystick moves the red pitch marker. A blue marker shows the arrival after swing. Drag the radar dots to move fielders while you are bowling.

**Pause:** Resume, Restart, Quit. The card also lists the editor keys.

**Umpire card:** FOUR, SIX, OUT, WIDE, NO BALL. A short replay follows a boundary or a wicket.

## 12. Result

Title (who won, and by how many), scorecard, player of the match, coins, XP, level-up line. Buttons: Menu, Rematch, Change setup.

## 13. Store, profile, settings, nets

- Store: practice reward, two coin packs, Blitz Pass, ground unlocks, over unlocks. Copy states that nothing is charged.
- Profile: level, record, best total, guest or local account.
- Settings: sound, haptics, low/medium graphics, 30/60 fps, joystick or swipe, replay tutorial.
- Nets: two free overs, batting only.

## Prefab names to create later

`Canvas_Menu`, `Panel_Consent`, `Panel_Loading`, `Panel_Home`, `Panel_TeamSelect`, `Panel_MatchSetup`, `Canvas_HUD`, `Widget_Joystick`, `Widget_Scorebar`, `Panel_BatterSelect`, `Panel_Pause`, `Panel_Result`.

Wire them to the same public methods the code UI calls: `MenuFlow.OpenQuickMatch`, `MatchManager.Launch`, `BattingController.Hold`, `BowlingController.MarkReady`.
