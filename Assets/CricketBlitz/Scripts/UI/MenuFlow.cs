using System.Collections;
using System.Collections.Generic;
using CricketBlitz.Core;
using CricketBlitz.Data;
using CricketBlitz.Match;
using CricketBlitz.Save;
using CricketBlitz.Services;
using CricketBlitz.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace CricketBlitz.UI
{
    /// <summary>
    /// Every screen before and after a match. Panels are built in code so the
    /// game is playable before any prefabs are wired. See Docs/UI_LAYOUTS.md
    /// for the prefab version of each screen.
    /// </summary>
    public sealed class MenuFlow : MonoBehaviour
    {
        public static MenuFlow Instance { get; private set; }

        enum AppScreen
        {
            Splash,
            Consent,
            Location,
            SignIn,
            Loading,
            Main,
            Modes,
            Tournaments,
            Profile,
            Store,
            Settings,
            Nets,
            Team,
            MatchSetup,
            Result
        }

        static readonly string[] Tips =
        {
            "Hard pitch offers more bounce.",
            "A green top helps the quicks swing it.",
            "Dry pitches are where cutters grip.",
            "Loft over the infield in the powerplay.",
            "Perfect timing beats raw power.",
            "Yorkers are the hardest ball to loft.",
            "Short balls sit up for the pull.",
            "Leave the wide ones.",
            "Call the run, then watch the throw.",
            "Night games are played under the lights."
        };

        Canvas canvas;
        RectTransform content;
        Text status;
        Image loadingFill;
        AppScreen screen;
        float timer;
        int tipIndex;
        MatchConfig draft;
        MatchResult lastResult;
        bool loading;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            canvas = UiKit.CreateCanvas("MenuCanvas", 10);
            canvas.transform.SetParent(transform, false);
            RectTransform background = UiKit.Box(canvas.transform, "Background", new Color(0.05f, 0.08f, 0.12f, 1f));
            UiKit.Stretch(background, Vector2.zero, Vector2.one);
            content = UiKit.Box(canvas.transform, "Content", new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(content, new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.86f));
            content.GetComponent<Image>().raycastTarget = false;
        }

        void Start()
        {
            ShowSplash();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (screen == AppScreen.Splash && timer > 1.35f)
            {
                RouteBoot();
            }

            if (loading)
            {
                float progress = Mathf.Clamp01(timer / 2.6f);
                if (loadingFill != null)
                {
                    loadingFill.rectTransform.anchorMax = new Vector2(progress, 1f);
                }

                if (timer > 0.2f && Mathf.FloorToInt(timer / 0.7f) != tipIndex)
                {
                    tipIndex = Mathf.FloorToInt(timer / 0.7f);
                    if (status != null)
                    {
                        status.text = Tips[tipIndex % Tips.Length];
                    }
                }

                if (timer > 2.6f)
                {
                    loading = false;
                    FinishLoading();
                }
            }
        }

        public void HideForMatch()
        {
            canvas.enabled = false;
        }

        public void ReturnToMenu()
        {
            canvas.enabled = true;
            ShowMain();
        }

        public void OpenQuickMatch()
        {
            canvas.enabled = true;
            draft = NewDraft();
            ShowTeams();
        }

        public void ShowResult(MatchResult result)
        {
            canvas.enabled = true;
            lastResult = result;
            screen = AppScreen.Result;
            Clear();
            PanelTitle(result.Title);
            Text body = UiKit.Label(content, result.Summary + "\n\n" + result.Scorecard + "\nPlayer of the match: " + result.PlayerOfTheMatch
                + "\n+" + result.Coins + " coins    +" + result.Xp + " XP"
                + (result.LevelsGained > 0 ? "\nLevel up. You are level " + result.Level + "." : string.Empty),
                24, TextAnchor.UpperLeft, UiKit.Ink);
            UiKit.Stretch(body.rectTransform, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.82f));
            Place(UiKit.Button(content, "Menu", ShowMain, 26), 0.08f, 0.04f, 0.28f, 0.16f);
            Place(UiKit.Button(content, "Rematch", () => Launch(result.Config), 26), 0.36f, 0.04f, 0.6f, 0.16f);
            Place(UiKit.Button(content, "Change setup", OpenQuickMatch, 26), 0.68f, 0.04f, 0.92f, 0.16f);
        }

        void RouteBoot()
        {
            PlayerProfile profile = GameManager.Instance.Profile;
            if (!profile.consentAccepted)
            {
                ShowConsent();
            }
            else if (!profile.locationAsked)
            {
                ShowLocation();
            }
            else if (!profile.accountChosen)
            {
                ShowSignIn();
            }
            else
            {
                ShowLoading();
            }
        }

        void FinishLoading()
        {
            PlayerProfile profile = GameManager.Instance.Profile;
            if (!profile.tutorialComplete)
            {
                MatchConfig tutorial = NewDraft();
                tutorial.IsTutorial = true;
                tutorial.Overs = 2;
                tutorial.Difficulty = Difficulty.Easy;
                tutorial.SkipIntro = true;
                tutorial.RainPercent = 0;
                Launch(tutorial);
                return;
            }

            ShowMain();
        }

        void ShowSplash()
        {
            screen = AppScreen.Splash;
            timer = 0f;
            Clear();
            Text title = UiKit.Label(content, "CRICKET BLITZ", 72, TextAnchor.MiddleCenter, UiKit.Gold);
            UiKit.Stretch(title.rectTransform, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.75f));
            Text sub = UiKit.Label(content, "Harborline Games", 28, TextAnchor.MiddleCenter, UiKit.Muted);
            UiKit.Stretch(sub.rectTransform, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.42f));
        }

        void ShowConsent()
        {
            screen = AppScreen.Consent;
            Clear();
            PanelTitle("Before you play");
            Text body = UiKit.Label(content,
                "Cricket Blitz stores your career on this device. Optional ads can be personalized. You can play either way. Read the privacy note, then press OK.",
                26, TextAnchor.UpperLeft, UiKit.Ink);
            UiKit.Stretch(body.rectTransform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.78f));
            Toggle toggle = MakeToggle("Allow personalized ads");
            Place(toggle.GetComponent<RectTransform>(), 0.08f, 0.28f, 0.7f, 0.4f);
            Place(UiKit.Button(content, "Privacy policy", ShowPrivacy, 24), 0.08f, 0.14f, 0.4f, 0.26f);
            Place(UiKit.Button(content, "OK", () =>
            {
                GameManager.Instance.Profile.consentAccepted = true;
                GameManager.Instance.Profile.adsPersonalized = toggle.isOn;
                GameManager.Instance.Save();
                ShowLocation();
            }, 28), 0.58f, 0.08f, 0.9f, 0.24f);
        }

        void ShowPrivacy()
        {
            Clear();
            PanelTitle("Privacy");
            Text body = UiKit.Label(content,
                "Harborline Games keeps your display name, coins, matches and settings on this device, with a local mirror that stands in for cloud save. Personalized ads, if you allow them, would use a coarse interest profile. Location, if you allow it, is only for a future nearby-club list. Cricket Blitz does not ask for contacts, photos or precise GPS. This is an original game. It is not affiliated with any real board, league or player.",
                24, TextAnchor.UpperLeft, UiKit.Ink);
            UiKit.Stretch(body.rectTransform, new Vector2(0.06f, 0.2f), new Vector2(0.94f, 0.82f));
            Place(UiKit.Button(content, "Back", ShowConsent, 26), 0.35f, 0.04f, 0.65f, 0.16f);
        }

        void ShowLocation()
        {
            screen = AppScreen.Location;
            Clear();
            PanelTitle("Location");
            Text body = UiKit.Label(content,
                "Share coarse location so a later update can show nearby club events. This is optional. The match does not need it.",
                26, TextAnchor.UpperLeft, UiKit.Ink);
            UiKit.Stretch(body.rectTransform, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.75f));
            Place(UiKit.Button(content, "Allow", () =>
            {
                LocationPrompt.Ask(granted =>
                {
                    GameManager.Instance.Profile.locationAsked = true;
                    GameManager.Instance.Profile.locationGranted = granted;
                    GameManager.Instance.Save();
                    ShowSignIn();
                });
            }, 26), 0.1f, 0.12f, 0.42f, 0.28f);
            Place(UiKit.Button(content, "Not now", () =>
            {
                GameManager.Instance.Profile.locationAsked = true;
                GameManager.Instance.Profile.locationGranted = false;
                GameManager.Instance.Save();
                ShowSignIn();
            }, 26), 0.52f, 0.12f, 0.88f, 0.28f);
        }

        void ShowSignIn()
        {
            screen = AppScreen.SignIn;
            Clear();
            PanelTitle("Sign in");
            Text body = UiKit.Label(content,
                "Google sign-in is not connected yet. Choose a display name and it stays on this device, or play as a guest. The game is fully playable offline.",
                24, TextAnchor.UpperLeft, UiKit.Ink);
            UiKit.Stretch(body.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.84f));
            InputField field = MakeField("Your name");
            Place(field.GetComponent<RectTransform>(), 0.18f, 0.42f, 0.82f, 0.56f);
            Place(UiKit.Button(content, "Sign in with Google", () =>
            {
                string name = string.IsNullOrEmpty(field.text) ? "Alex Rivera" : field.text.Trim();
                AcceptAccount(name, false);
            }, 24), 0.1f, 0.16f, 0.48f, 0.32f);
            Place(UiKit.Button(content, "Play as guest", () =>
            {
                AcceptAccount("Guest " + Random.Range(1000, 9999), true);
            }, 24), 0.52f, 0.16f, 0.9f, 0.32f);
        }

        void AcceptAccount(string name, bool guest)
        {
            PlayerProfile profile = GameManager.Instance.Profile;
            profile.userName = name;
            profile.isGuest = guest;
            profile.accountChosen = true;
            GameManager.Instance.Save();
            ShowLoading();
        }

        void ShowLoading()
        {
            screen = AppScreen.Loading;
            timer = 0f;
            tipIndex = -1;
            loading = true;
            Clear();
            PanelTitle("Loading Harbor Oval");
            RectTransform bar = UiKit.Box(content, "Bar", new Color(1f, 1f, 1f, 0.12f));
            UiKit.Stretch(bar, new Vector2(0.12f, 0.42f), new Vector2(0.88f, 0.5f));
            loadingFill = UiKit.Box(bar, "Fill", UiKit.Gold).GetComponent<Image>();
            loadingFill.rectTransform.anchorMin = Vector2.zero;
            loadingFill.rectTransform.anchorMax = new Vector2(0.02f, 1f);
            loadingFill.rectTransform.offsetMin = Vector2.zero;
            loadingFill.rectTransform.offsetMax = Vector2.zero;
            status = UiKit.Label(content, Tips[0], 28, TextAnchor.MiddleCenter, UiKit.Ink);
            UiKit.Stretch(status.rectTransform, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.36f));
        }

        void ShowMain()
        {
            screen = AppScreen.Main;
            loading = false;
            Clear();
            DrawTopBar();
            DrawTabs();
            PlayerProfile profile = GameManager.Instance.Profile;
            if (GameManager.Instance.DailyReady())
            {
                Place(UiKit.Button(content, "Daily reward 100 coins", () =>
                {
                    GameManager.Instance.ClaimDaily();
                    ShowMain();
                }, 22), 0.62f, 0.84f, 0.98f, 0.98f);
            }

            Place(UiKit.Button(content, "Quick Match", OpenQuickMatch, 28), 0.04f, 0.52f, 0.34f, 0.8f);
            Place(UiKit.Button(content, "Multiplayer", ShowModes, 24), 0.36f, 0.52f, 0.64f, 0.8f);
            Place(UiKit.Button(content, "League", ShowModes, 24), 0.66f, 0.52f, 0.96f, 0.8f);
            Place(UiKit.Button(content, "Custom Tournament", ShowTournaments, 22), 0.04f, 0.28f, 0.34f, 0.48f);
            Place(UiKit.Button(content, "World Cup", ShowTournaments, 22), 0.36f, 0.28f, 0.64f, 0.48f);
            Place(UiKit.Button(content, "Store  ·  Blitz Pass", ShowStore, 22), 0.66f, 0.28f, 0.96f, 0.48f);
            Text hello = UiKit.Label(content, "Welcome, " + profile.userName + ". Energy " + profile.energy + "/" + profile.energyMax, 22, TextAnchor.MiddleLeft, UiKit.Muted);
            UiKit.Stretch(hello.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.7f, 0.24f));
        }

        void ShowModes()
        {
            screen = AppScreen.Modes;
            Clear();
            DrawTabs();
            PanelTitle("Modes");
            Text body = UiKit.Label(content,
                "Quick Match is the live exhibition: one ground, two fictional sides, two to fifty overs.\n\nMultiplayer and League ladders are the next season. Nothing here connects to a server. Challenge a side offline in Quick Match.",
                24, TextAnchor.UpperLeft, UiKit.Ink);
            UiKit.Stretch(body.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.78f));
            Place(UiKit.Button(content, "Quick Match", OpenQuickMatch, 26), 0.3f, 0.08f, 0.7f, 0.22f);
        }

        void ShowTournaments()
        {
            screen = AppScreen.Tournaments;
            Clear();
            DrawTabs();
            PanelTitle("Tournaments");
            Text body = UiKit.Label(content,
                "Custom Tournament will let you invite fictional club sides into a knockout you design.\n\nWorld Cup, played here as the Crown Series, will be a group stage plus a final. No real boards, trophies or kits.\n\nBoth use the same 5-over match you can play now.",
                24, TextAnchor.UpperLeft, UiKit.Ink);
            UiKit.Stretch(body.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.78f));
            Place(UiKit.Button(content, "Play an exhibition", OpenQuickMatch, 26), 0.28f, 0.08f, 0.72f, 0.22f);
        }

        void ShowProfile()
        {
            screen = AppScreen.Profile;
            Clear();
            DrawTabs();
            PlayerProfile profile = GameManager.Instance.Profile;
            PanelTitle(profile.userName);
            Text body = UiKit.Label(content,
                "Level " + profile.level + "    XP " + profile.xpIntoLevel + "/100\n"
                + "Matches " + profile.matchesPlayed + "    Wins " + profile.wins + "\n"
                + "Best batting total " + profile.bestScore + "\n"
                + "Coins " + profile.coins + "    Tickets " + profile.tickets + "\n"
                + (profile.blitzPass ? "Blitz Pass active. Coin rewards +10%.\n" : "No Blitz Pass.\n")
                + (profile.isGuest ? "Guest account, stored on this device." : "Local account, stored on this device."),
                26, TextAnchor.UpperLeft, UiKit.Ink);
            UiKit.Stretch(body.rectTransform, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.78f));
        }

        void ShowStore()
        {
            screen = AppScreen.Store;
            Clear();
            DrawTabs();
            PanelTitle("Store");
            Text note = UiKit.Label(content, "Purchases are local placeholders. No store is contacted.", 20, TextAnchor.MiddleLeft, UiKit.Muted);
            UiKit.Stretch(note.rectTransform, new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.96f));
            Place(UiKit.Button(content, "Practice reward +50", () => StartCoroutine(PracticeCoins()), 22), 0.04f, 0.66f, 0.48f, 0.82f);
            Place(UiKit.Button(content, "Coin pouch +500", () => StartCoroutine(PracticePurchase(MonetizationStubs.Coins500, 500, false)), 22), 0.52f, 0.66f, 0.96f, 0.82f);
            Place(UiKit.Button(content, "Coin crate +2000", () => StartCoroutine(PracticePurchase(MonetizationStubs.Coins2000, 2000, false)), 22), 0.04f, 0.46f, 0.48f, 0.62f);
            Place(UiKit.Button(content, "Blitz Pass", () => StartCoroutine(PracticePurchase(MonetizationStubs.Pass, 0, true)), 22), 0.52f, 0.46f, 0.96f, 0.62f);
            Place(UiKit.Button(content, GroundLabel(StadiumDatabase.Get("north_wharf")), () => BuyGround("north_wharf"), 20), 0.04f, 0.26f, 0.48f, 0.42f);
            Place(UiKit.Button(content, GroundLabel(StadiumDatabase.Get("lantern_park")), () => BuyGround("lantern_park"), 20), 0.52f, 0.26f, 0.96f, 0.42f);
            Place(UiKit.Button(content, GroundLabel(StadiumDatabase.Get("redrock_ground")), () => BuyGround("redrock_ground"), 20), 0.04f, 0.06f, 0.48f, 0.22f);
            Place(UiKit.Button(content, "10 overs  2000", () => BuyOvers(10, 2000), 20), 0.52f, 0.06f, 0.74f, 0.22f);
            Place(UiKit.Button(content, "20 / 50", () => BuyOvers(20, 8000), 20), 0.76f, 0.06f, 0.96f, 0.22f);
        }

        void ShowSettings()
        {
            screen = AppScreen.Settings;
            Clear();
            DrawTabs();
            PanelTitle("Settings");
            PlayerProfile profile = GameManager.Instance.Profile;
            Place(UiKit.Button(content, "Sound " + Mathf.RoundToInt(profile.settings.sfx * 100f), () =>
            {
                profile.settings.sfx = profile.settings.sfx > 0.2f ? 0f : 0.9f;
                profile.settings.music = profile.settings.sfx > 0f ? 0.55f : 0f;
                GameManager.Instance.ApplySettings();
                GameManager.Instance.Save();
                ShowSettings();
            }, 22), 0.04f, 0.7f, 0.48f, 0.86f);
            Place(UiKit.Button(content, profile.settings.haptics ? "Haptics on" : "Haptics off", () =>
            {
                profile.settings.haptics = !profile.settings.haptics;
                GameManager.Instance.ApplySettings();
                GameManager.Instance.Save();
                ShowSettings();
            }, 22), 0.52f, 0.7f, 0.96f, 0.86f);
            Place(UiKit.Button(content, profile.settings.quality <= 0 ? "Graphics low" : "Graphics medium", () =>
            {
                profile.settings.quality = profile.settings.quality <= 0 ? 1 : 0;
                GameManager.Instance.ApplySettings();
                GameManager.Instance.Save();
                ShowSettings();
            }, 22), 0.04f, 0.5f, 0.48f, 0.66f);
            Place(UiKit.Button(content, profile.settings.frameCap + " fps", () =>
            {
                profile.settings.frameCap = profile.settings.frameCap == 60 ? 30 : 60;
                GameManager.Instance.ApplySettings();
                GameManager.Instance.Save();
                ShowSettings();
            }, 22), 0.52f, 0.5f, 0.96f, 0.66f);
            Place(UiKit.Button(content, profile.settings.controlScheme == 0 ? "Controls: joystick" : "Controls: swipe", () =>
            {
                profile.settings.controlScheme = profile.settings.controlScheme == 0 ? 1 : 0;
                GameManager.Instance.Save();
                ShowSettings();
            }, 22), 0.04f, 0.3f, 0.48f, 0.46f);
            Place(UiKit.Button(content, "Replay tutorial", () =>
            {
                profile.tutorialComplete = false;
                GameManager.Instance.Save();
                MatchConfig tutorial = NewDraft();
                tutorial.IsTutorial = true;
                tutorial.Overs = 2;
                tutorial.Difficulty = Difficulty.Easy;
                tutorial.SkipIntro = true;
                Launch(tutorial);
            }, 22), 0.52f, 0.3f, 0.96f, 0.46f);
            Text keys = UiKit.Label(content, "In a match: joystick or WASD to aim. J push, K stroke, L loft, X leave, R run, C cancel. Bowling: 1-7 delivery, Q side, Enter to bowl, Esc pause.", 20, TextAnchor.UpperLeft, UiKit.Muted);
            UiKit.Stretch(keys.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.26f));
        }

        void ShowNets()
        {
            screen = AppScreen.Nets;
            Clear();
            DrawTabs();
            PanelTitle("Nets");
            Text body = UiKit.Label(content, "Two free overs in the middle. No energy, no crowd pressure. The bowler serves it up so you can try the timing bar.", 24, TextAnchor.UpperLeft, UiKit.Ink);
            UiKit.Stretch(body.rectTransform, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.75f));
            Place(UiKit.Button(content, "Start nets", () =>
            {
                MatchConfig nets = NewDraft();
                nets.IsNets = true;
                nets.Overs = 2;
                nets.Difficulty = Difficulty.Easy;
                nets.SkipIntro = true;
                nets.RainPercent = 0;
                Launch(nets);
            }, 28), 0.3f, 0.14f, 0.7f, 0.32f);
        }

        void ShowTeams()
        {
            screen = AppScreen.Team;
            Clear();
            PanelTitle("Teams");
            Text hint = UiKit.Label(content, "You: " + TeamDatabase.Get(draft.UserTeamId).Name + "    Opposition: " + TeamDatabase.Get(draft.OppTeamId).Name, 22, TextAnchor.MiddleLeft, UiKit.Gold);
            UiKit.Stretch(hint.rectTransform, new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.98f));
            List<TeamSheet> sides = new List<TeamSheet>(TeamDatabase.All());
            for (int i = 0; i < sides.Count; i++)
            {
                TeamSheet side = sides[i];
                float column = i % 2 == 0 ? 0.04f : 0.52f;
                float row = i < 2 ? 0.48f : 0.16f;
                string label = side.Category + "\n" + side.Name + "\nBat " + side.BatPercent + "  Bowl " + side.BowlPercent + "  Field " + side.FieldPercent;
                Place(UiKit.Button(content, label, () =>
                {
                    if (draft.OppTeamId == side.Id)
                    {
                        string swap = draft.UserTeamId;
                        draft.UserTeamId = side.Id;
                        draft.OppTeamId = swap;
                    }
                    else
                    {
                        draft.UserTeamId = side.Id;
                    }

                    ShowTeams();
                }, 20), column, row, column + 0.44f, row + 0.28f);
            }

            // The button above is a bit tangled. Offer two explicit cycles so the choice is reliable.
            Place(UiKit.Button(content, "Cycle your team", () =>
            {
                draft.UserTeamId = CycleTeam(draft.UserTeamId, draft.OppTeamId);
                ShowTeams();
            }, 20), 0.04f, 0.02f, 0.32f, 0.14f);
            Place(UiKit.Button(content, "Cycle opposition", () =>
            {
                draft.OppTeamId = CycleTeam(draft.OppTeamId, draft.UserTeamId);
                ShowTeams();
            }, 20), 0.34f, 0.02f, 0.64f, 0.14f);
            Place(UiKit.Button(content, "Next", ShowMatchSetup, 24), 0.68f, 0.02f, 0.96f, 0.14f);
        }

        void ShowMatchSetup()
        {
            screen = AppScreen.MatchSetup;
            Clear();
            StadiumInfo ground = StadiumDatabase.Get(draft.StadiumId);
            bool owned = GameManager.Instance.Profile.HasStadium(ground.Id);
            PanelTitle("Match settings");
            Place(UiKit.Button(content, (owned ? string.Empty : "Locked ") + ground.Name, CycleStadium, 20), 0.04f, 0.78f, 0.48f, 0.94f);
            Place(UiKit.Button(content, "Difficulty " + draft.Difficulty, () =>
            {
                draft.Difficulty = (Difficulty)(((int)draft.Difficulty + 1) % 6);
                ShowMatchSetup();
            }, 20), 0.52f, 0.78f, 0.96f, 0.94f);
            Place(UiKit.Button(content, "Overs " + draft.Overs, CycleOvers, 20), 0.04f, 0.6f, 0.3f, 0.74f);
            Place(UiKit.Button(content, "Pitch " + draft.Pitch, () =>
            {
                draft.Pitch = (PitchType)(((int)draft.Pitch + 1) % 3);
                ShowMatchSetup();
            }, 20), 0.32f, 0.6f, 0.62f, 0.74f);
            Place(UiKit.Button(content, draft.SkipIntro ? "Skip intro: yes" : "Skip intro: no", () =>
            {
                draft.SkipIntro = !draft.SkipIntro;
                ShowMatchSetup();
            }, 20), 0.64f, 0.6f, 0.96f, 0.74f);
            Place(UiKit.Button(content, "Time " + draft.TimeOfDay, () =>
            {
                draft.TimeOfDay = (TimeOfDay)(((int)draft.TimeOfDay + 1) % 5);
                ShowMatchSetup();
            }, 20), 0.04f, 0.42f, 0.34f, 0.56f);
            Place(UiKit.Button(content, "Rain " + draft.RainPercent + "%", () =>
            {
                draft.RainPercent = (draft.RainPercent + 25) % 125;
                ShowMatchSetup();
            }, 20), 0.36f, 0.42f, 0.62f, 0.56f);
            Place(UiKit.Button(content, TeamDatabase.Get(draft.UserTeamId).ShortName + " vs " + TeamDatabase.Get(draft.OppTeamId).ShortName, ShowTeams, 18), 0.64f, 0.42f, 0.96f, 0.56f);
            Text detail = UiKit.Label(content, SetupDetail(), 20, TextAnchor.UpperLeft, UiKit.Muted);
            UiKit.Stretch(detail.rectTransform, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.4f));
            Place(UiKit.Button(content, "Play", () => Launch(draft), 30), 0.34f, 0.02f, 0.66f, 0.16f);
        }

        string SetupDetail()
        {
            string line = "Hard bounce, dry spin, green seam. Easy widens your timing window. Hardcore bowls yorkers and short balls.";
            if (draft.Difficulty == Difficulty.Custom)
            {
                line = "Custom uses a wider line and a timing window you can feel closer to medium. Fine for a first exhibition.";
                draft.CustomSpray = 0.3f;
                draft.CustomTimingScale = 1f;
            }

            int cost = OversCost(draft.Overs);
            if (cost > 0 && !GameManager.Instance.Profile.HasOvers(draft.Overs))
            {
                line = draft.Overs + " overs unlocks for " + cost + " coins. 2 and 5 are open.";
            }

            return line;
        }

        void CycleStadium()
        {
            StadiumInfo[] all = StadiumDatabase.All();
            int index = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Id == draft.StadiumId)
                {
                    index = i;
                }
            }

            StadiumInfo next = all[(index + 1) % all.Length];
            if (!GameManager.Instance.Profile.HasStadium(next.Id))
            {
                if (next.UnlockCost > 0 && GameManager.Instance.TrySpend(next.UnlockCost))
                {
                    GameManager.Instance.Profile.UnlockStadium(next.Id);
                    GameManager.Instance.Save();
                    draft.StadiumId = next.Id;
                }
            }
            else
            {
                draft.StadiumId = next.Id;
            }

            ShowMatchSetup();
        }

        void CycleOvers()
        {
            int[] options = { 2, 5, 10, 20, 50 };
            int index = 0;
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == draft.Overs)
                {
                    index = i;
                }
            }

            int next = options[(index + 1) % options.Length];
            int cost = OversCost(next);
            if (GameManager.Instance.Profile.HasOvers(next))
            {
                draft.Overs = next;
            }
            else if (cost > 0 && GameManager.Instance.TrySpend(cost))
            {
                GameManager.Instance.Profile.UnlockOvers(next);
                GameManager.Instance.Save();
                draft.Overs = next;
            }

            ShowMatchSetup();
        }

        void Launch(MatchConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (!config.IsTutorial && !config.IsNets && !GameManager.Instance.TrySpendEnergy(1))
            {
                screen = AppScreen.Main;
                Clear();
                PanelTitle("Out of energy");
                Text body = UiKit.Label(content, "Energy returns one point every 8 minutes. A practice reward refills two.", 24, TextAnchor.MiddleCenter, UiKit.Ink);
                UiKit.Stretch(body.rectTransform, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.7f));
                Place(UiKit.Button(content, "Practice reward", () => StartCoroutine(RefillEnergy()), 24), 0.15f, 0.16f, 0.48f, 0.32f);
                Place(UiKit.Button(content, "Back", ShowMain, 24), 0.52f, 0.16f, 0.85f, 0.32f);
                return;
            }

            if (!GameManager.Instance.Profile.HasStadium(config.StadiumId))
            {
                config.StadiumId = "harbor_oval";
            }

            if (!GameManager.Instance.Profile.HasOvers(config.Overs))
            {
                config.Overs = 5;
            }

            HideForMatch();
            MatchManager.Launch(config);
        }

        IEnumerator PracticeCoins()
        {
            yield return MonetizationStubs.PracticeReward(ok =>
            {
                if (ok)
                {
                    GameManager.Instance.AddCoins(50);
                    GameManager.Instance.Save();
                }

                ShowStore();
            });
        }

        IEnumerator PracticePurchase(string product, int coins, bool pass)
        {
            yield return MonetizationStubs.PracticePurchase(product, ok =>
            {
                if (ok)
                {
                    GameManager.Instance.AddCoins(coins);
                    if (pass)
                    {
                        GameManager.Instance.Profile.blitzPass = true;
                    }

                    GameManager.Instance.Save();
                }

                ShowStore();
            });
        }

        IEnumerator RefillEnergy()
        {
            yield return MonetizationStubs.PracticeReward(ok =>
            {
                if (ok)
                {
                    GameManager.Instance.Profile.energy = Mathf.Min(GameManager.Instance.Profile.energyMax, GameManager.Instance.Profile.energy + 2);
                    GameManager.Instance.Save();
                }

                ShowMain();
            });
        }

        void BuyGround(string id)
        {
            StadiumInfo info = StadiumDatabase.Get(id);
            if (GameManager.Instance.Profile.HasStadium(id))
            {
                ShowStore();
                return;
            }

            if (GameManager.Instance.TrySpend(info.UnlockCost))
            {
                GameManager.Instance.Profile.UnlockStadium(id);
                GameManager.Instance.Save();
            }

            ShowStore();
        }

        void BuyOvers(int overs, int cost)
        {
            if (GameManager.Instance.Profile.HasOvers(overs))
            {
                if (overs == 20 && !GameManager.Instance.Profile.HasOvers(50) && GameManager.Instance.TrySpend(20000))
                {
                    GameManager.Instance.Profile.UnlockOvers(50);
                    GameManager.Instance.Save();
                }

                ShowStore();
                return;
            }

            if (GameManager.Instance.TrySpend(cost))
            {
                GameManager.Instance.Profile.UnlockOvers(overs);
                GameManager.Instance.Save();
            }

            ShowStore();
        }

        void DrawTopBar()
        {
            PlayerProfile profile = GameManager.Instance.Profile;
            Text bar = UiKit.Label(canvas.transform, "Lv " + profile.level + "   " + profile.xpIntoLevel + "/100 XP    " + profile.userName
                + "    " + profile.coins + " coins    " + profile.tickets + " tickets    " + profile.energy + " energy",
                22, TextAnchor.MiddleLeft, UiKit.Ink);
            UiKit.Stretch(bar.rectTransform, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.98f));
        }

        void DrawTabs()
        {
            Place(UiKit.Button(canvas.transform, "Home", ShowMain, 18), 0.02f, 0.02f, 0.14f, 0.1f);
            Place(UiKit.Button(canvas.transform, "Modes", ShowModes, 18), 0.15f, 0.02f, 0.27f, 0.1f);
            Place(UiKit.Button(canvas.transform, "Cups", ShowTournaments, 18), 0.28f, 0.02f, 0.4f, 0.1f);
            Place(UiKit.Button(canvas.transform, "Nets", ShowNets, 18), 0.41f, 0.02f, 0.53f, 0.1f);
            Place(UiKit.Button(canvas.transform, "Profile", ShowProfile, 18), 0.54f, 0.02f, 0.68f, 0.1f);
            Place(UiKit.Button(canvas.transform, "Store", ShowStore, 18), 0.69f, 0.02f, 0.81f, 0.1f);
            Place(UiKit.Button(canvas.transform, "Settings", ShowSettings, 18), 0.82f, 0.02f, 0.98f, 0.1f);
        }

        void PanelTitle(string title)
        {
            Text text = UiKit.Label(content, title, 36, TextAnchor.MiddleLeft, UiKit.Gold);
            UiKit.Stretch(text.rectTransform, new Vector2(0.02f, 0.86f), new Vector2(0.7f, 0.99f));
        }

        void Clear()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }

            // Tabs and top bar are parented to the canvas, not content. Clear those too except background and content.
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child == content || child.name == "Background" || child.name == "Content")
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        Toggle MakeToggle(string label)
        {
            RectTransform rect = UiKit.Box(content, "Toggle", new Color(0f, 0f, 0f, 0f));
            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            RectTransform box = UiKit.Box(rect, "Box", Color.white);
            UiKit.Place(box, new Vector2(0f, 0.5f), new Vector2(36f, 36f), new Vector2(0f, 0.5f));
            box.anchoredPosition = new Vector2(8f, 0f);
            RectTransform mark = UiKit.Box(box, "Mark", UiKit.Gold);
            UiKit.Stretch(mark, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = mark.GetComponent<Image>();
            toggle.isOn = false;
            Text text = UiKit.Label(rect, label, 24, TextAnchor.MiddleLeft, UiKit.Ink);
            text.rectTransform.offsetMin = new Vector2(56f, 0f);
            return toggle;
        }

        InputField MakeField(string placeholder)
        {
            RectTransform rect = UiKit.Box(content, "Name", new Color(1f, 1f, 1f, 0.08f));
            InputField field = rect.gameObject.AddComponent<InputField>();
            Text text = UiKit.Label(rect, string.Empty, 28, TextAnchor.MiddleLeft, UiKit.Ink);
            text.supportRichText = false;
            Text hint = UiKit.Label(rect, placeholder, 24, TextAnchor.MiddleLeft, UiKit.Muted);
            field.textComponent = text;
            field.placeholder = hint;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        static void Place(Component widget, float xMin, float yMin, float xMax, float yMax)
        {
            UiKit.Stretch(widget.GetComponent<RectTransform>(), new Vector2(xMin, yMin), new Vector2(xMax, yMax));
        }

        static MatchConfig NewDraft()
        {
            MatchConfig config = new MatchConfig();
            config.StadiumId = "harbor_oval";
            config.UserTeamId = "harbor_kings";
            config.OppTeamId = "ridge_falcons";
            config.Overs = 5;
            config.Difficulty = Difficulty.Medium;
            config.Pitch = PitchType.Hard;
            config.TimeOfDay = TimeOfDay.Afternoon;
            return config;
        }

        static string CycleTeam(string current, string blocked)
        {
            TeamSheet[] all = TeamDatabase.All();
            int index = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Id == current)
                {
                    index = i;
                }
            }

            for (int step = 1; step <= all.Length; step++)
            {
                TeamSheet candidate = all[(index + step) % all.Length];
                if (candidate.Id != blocked)
                {
                    return candidate.Id;
                }
            }

            return current;
        }

        static int OversCost(int overs)
        {
            switch (overs)
            {
                case 10: return 2000;
                case 20: return 8000;
                case 50: return 20000;
                default: return 0;
            }
        }

        static string GroundLabel(StadiumInfo info)
        {
            bool owned = GameManager.Instance.Profile.HasStadium(info.Id);
            return owned ? info.Name + " owned" : info.Name + "  " + info.UnlockCost;
        }
    }
}
