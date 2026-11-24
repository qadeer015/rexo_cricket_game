using System;
using System.Collections.Generic;
using System.Text;
using CricketBlitz.Audio;
using CricketBlitz.Batting;
using CricketBlitz.Bowling;
using CricketBlitz.Core;
using CricketBlitz.Data;
using CricketBlitz.Fielding;
using CricketBlitz.Services;
using CricketBlitz.Sim;
using CricketBlitz.Tutorial;
using CricketBlitz.UI;
using CricketBlitz.World;
using UnityEngine;

namespace CricketBlitz.Match
{
    public enum MatchPhase
    {
        Intro,
        TossCall,
        Walkout,
        SelectBatter,
        Between,
        Setup,
        RunUp,
        Alive,
        Umpire,
        Highlight,
        InningsBreak,
        Done
    }

    /// <summary>
    /// One match: toss, two innings, and the ball-by-ball loop.
    /// Batting, bowling and fielding controllers report in; the laws live in InningsModel.
    /// </summary>
    public sealed class MatchManager : MonoBehaviour
    {
        public static MatchManager Current { get; private set; }

        public MatchPhase Phase;
        MatchConfig config;
        TeamSheet userTeam;
        TeamSheet oppTeam;
        TeamSheet battingTeam;
        TeamSheet bowlingTeam;
        StadiumInfo ground;
        StadiumContext world;
        InningsModel innings;
        InningsModel firstInnings;
        BallPhysics ball;
        BattingController batting;
        BowlingController bowling;
        FielderAI fielding;
        ScoreboardUI board;
        MatchInputUI input;
        MatchCameraDirector cameraRig;
        TutorialManager tutorial;
        readonly System.Random random = new System.Random();
        readonly List<Vector3> samples = new List<Vector3>(480);

        int inningsNumber = 1;
        int bowlerIndex;
        int firstRuns = -1;
        int firstOvers = 5;
        int chaseTarget = -1;
        int battingRuns;
        int bowlingWickets;
        float timer;
        float sinceHit;
        bool userBatting;
        bool paused;
        bool selectSingle;
        bool battersReady;
        bool raining;
        bool freeHitThisBall;
        bool shotApplied;
        bool contact;
        bool crossed;
        bool wide;
        bool noBall;
        bool wicket;
        bool edge;
        bool ballDead;
        bool ballResolved;
        bool runningOpen;
        bool going;
        bool aborting;
        bool aIsStriker = true;
        bool aTowardNegative = true;
        bool overJustEnded;
        bool aiDecided;
        bool sampleStride;
        bool matchEnded;
        WicketKind wicketKind;
        int boundary;
        int desiredRuns;
        int completedRuns;
        float endA = 8.7f;
        float endB = -8.7f;
        string shotName = string.Empty;
        DeliveryPlan currentPlan;
        ShotQuery aiQuery;
        BallRecord lastRecord;
        FieldPlan shownField = FieldPlan.Balanced;

        public static MatchManager Launch(MatchConfig matchConfig)
        {
            GameObject root = new GameObject("Match");
            MatchManager match = root.AddComponent<MatchManager>();
            match.Begin(matchConfig);
            return match;
        }

        void Begin(MatchConfig matchConfig)
        {
            Current = this;
            config = matchConfig;
            if (config.Overs != 2 && config.Overs != 5 && config.Overs != 10 && config.Overs != 20 && config.Overs != 50)
            {
                config.Overs = 5;
            }

            userTeam = TeamDatabase.Get(config.UserTeamId);
            oppTeam = TeamDatabase.Get(config.OppTeamId);
            if (oppTeam.Id == userTeam.Id)
            {
                oppTeam = userTeam.Id == "harbor_kings" ? TeamDatabase.Get("ridge_falcons") : TeamDatabase.Get("harbor_kings");
            }

            ground = StadiumDatabase.Get(config.StadiumId);
            config.WindKmh = ground.WindKmh;
            world = StadiumBuilder.Build(transform, ground, config.Pitch);
            ball = BallPhysics.Create(transform);
            world.Ball = ball.transform;
            batting = gameObject.AddComponent<BattingController>();
            bowling = gameObject.AddComponent<BowlingController>();
            fielding = gameObject.AddComponent<FielderAI>();
            board = gameObject.AddComponent<ScoreboardUI>();
            board.Build();
            board.PauseClicked = Pause;
            board.CameraClicked = () =>
            {
                if (cameraRig != null)
                {
                    cameraRig.Cycle();
                }
            };
            input = gameObject.AddComponent<MatchInputUI>();
            input.Build(board.Canvas.transform);
            input.OnRun = CallRun;
            input.OnCancelRun = CancelRun;
            input.Bind(batting, bowling, fielding, GameManager.Instance != null ? GameManager.Instance.Profile.settings.controlScheme : 0);

            Camera main = Camera.main;
            if (main == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                main = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            cameraRig = MatchCameraDirector.Attach(main);
            cameraRig.Ball = ball.transform;
            ApplyAtmosphere();
            if (MatchAudio.Instance != null)
            {
                MatchAudio.Instance.StartCrowd();
            }

            tutorial = gameObject.AddComponent<TutorialManager>();
            if (config.IsTutorial)
            {
                tutorial.Begin(board, input);
                tutorial.OnYes = LeaveToQuickMatch;
                tutorial.OnNo = LeaveToMenu;
            }

            if (config.IsTutorial || config.IsNets || config.SkipIntro)
            {
                config.UserBatsFirst = config.IsNets || config.IsTutorial || config.UserBatsFirst;
                if (config.IsNets)
                {
                    config.UserBatsFirst = true;
                }

                StartInnings();
                Enter(config.IsTutorial || !userBatting ? MatchPhase.Setup : MatchPhase.SelectBatter);
                if (config.IsTutorial)
                {
                    board.SetCommentary("Coach Jalen Crowe is on the sideline. Follow the numbered tasks.");
                }
            }
            else
            {
                Enter(MatchPhase.Intro);
            }
        }

        void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }

            Time.timeScale = 1f;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (paused)
                {
                    Resume();
                }
                else if (Phase != MatchPhase.Done)
                {
                    Pause();
                }
            }

            if (tutorial != null && tutorial.Active)
            {
                tutorial.Point();
                tutorial.NotifyAim(batting.Aim);
                tutorial.NotifyFamily(batting.Planned);
            }

            if (paused || (tutorial != null && (tutorial.WaitingForReward || tutorial.Finished)) || Phase == MatchPhase.Done)
            {
                return;
            }

            float dt = Time.deltaTime;
            timer += dt;
            TickAvatars(dt);
            switch (Phase)
            {
                case MatchPhase.Intro:
                    if (timer > 3.2f)
                    {
                        Enter(MatchPhase.TossCall);
                    }

                    break;
                case MatchPhase.Walkout:
                    if (timer > 2.4f)
                    {
                        Enter(userBatting ? MatchPhase.SelectBatter : MatchPhase.Setup);
                    }

                    break;
                case MatchPhase.SelectBatter:
                    if (battersReady)
                    {
                        battersReady = false;
                        SyncBatters();
                        Enter(MatchPhase.Between);
                    }

                    break;
                case MatchPhase.Between:
                    if (timer > 0.45f)
                    {
                        Enter(MatchPhase.Setup);
                    }

                    break;
                case MatchPhase.Setup:
                    TickSetup();
                    break;
                case MatchPhase.RunUp:
                    TickRunUp();
                    break;
                case MatchPhase.Alive:
                    TickAlive(dt);
                    break;
                case MatchPhase.Umpire:
                    if (timer > 1.05f)
                    {
                        bool show = boundary >= 4 || wicket;
                        Enter(show && samples.Count > 8 ? MatchPhase.Highlight : MatchPhase.Between);
                        if (Phase == MatchPhase.Between)
                        {
                            NextBall();
                        }
                    }

                    break;
                case MatchPhase.Highlight:
                    TickHighlight();
                    break;
                case MatchPhase.InningsBreak:
                    if (timer > 2.6f)
                    {
                        Enter(userBatting ? MatchPhase.SelectBatter : MatchPhase.Setup);
                    }

                    break;
            }

            if (Phase == MatchPhase.Setup || Phase == MatchPhase.RunUp || Phase == MatchPhase.Alive)
            {
                UpdateMarkers();
            }
        }

        void Enter(MatchPhase next)
        {
            Phase = next;
            timer = 0f;
            input.ShowBatting(false);
            input.ShowBowling(false);
            input.ShowRunning(false);
            batting.WindowOpen = false;
            switch (next)
            {
                case MatchPhase.Intro:
                    board.ShowBanner(
                        "Live from " + ground.Name,
                        userTeam.Name + "  vs  " + oppTeam.Name + "\n" + ConditionsText());
                    board.SetCommentary(CommentaryBank.Pick("intro", ground.Name));
                    break;
                case MatchPhase.TossCall:
                    board.HideBanner();
                    board.ShowChoices("Toss", "The captains walk out. Call it.", "Heads", "Tails", () => ResolveToss(true), () => ResolveToss(false));
                    break;
                case MatchPhase.Walkout:
                    board.SetCommentary("The players walk out. Jersey numbers on their backs.");
                    break;
                case MatchPhase.SelectBatter:
                    board.ShowBatterSelect(
                        innings.Batters,
                        selectSingle,
                        selectSingle ? innings.Striker : 0,
                        OnBattersPicked);
                    break;
                case MatchPhase.Setup:
                    board.HideBanner();
                    input.ShowBowling(!userBatting);
                    input.ShowBatting(userBatting);
                    cameraRig.Mode = userBatting ? CamMode.BehindBatsman : CamMode.BehindBowler;
                    if (!userBatting)
                    {
                        PlayerSeed seed = bowlingTeam.Players[bowlerIndex];
                        bowling.SetType(seed.Stock);
                        bowling.ResetForBall();
                    }
                    else
                    {
                        PrepareAiDelivery();
                    }

                    fielding.Place(shownField, innings.IsPowerplay);
                    RefreshHud();
                    break;
                case MatchPhase.RunUp:
                    if (world.Bowler != null)
                    {
                        world.Bowler.PlayBowl();
                    }

                    input.ShowBatting(userBatting);
                    break;
                case MatchPhase.Alive:
                    batting.WindowOpen = userBatting;
                    batting.ResetForBall();
                    input.ShowBatting(userBatting);
                    if (freeHitThisBall)
                    {
                        board.SetCommentary(CommentaryBank.Pick("freehit", string.Empty));
                    }

                    break;
                case MatchPhase.Umpire:
                    ShowUmpire();
                    break;
                case MatchPhase.Highlight:
                    ball.gameObject.SetActive(true);
                    cameraRig.Mode = CamMode.FollowBall;
                    board.SetCommentary("Replay");
                    break;
                case MatchPhase.InningsBreak:
                    board.ShowBanner(
                        battingTeam.Name + " need " + chaseTarget,
                        "Target " + chaseTarget + " from " + innings.TotalOvers + " overs.");
                    board.SetCommentary(CommentaryBank.Pick("target", battingTeam.Name));
                    break;
            }
        }

        void TickSetup()
        {
            if (tutorial != null && tutorial.BlockBowling())
            {
                return;
            }

            if (!userBatting)
            {
                if (bowling.Field != shownField)
                {
                    shownField = bowling.Field;
                    fielding.Place(shownField, innings.IsPowerplay);
                }

                if (bowling.Ready || Input.GetKeyDown(KeyCode.Return))
                {
                    currentPlan = bowling.BuildPlan(bowlingTeam.Players[bowlerIndex], innings.StrikerScore.RightHanded);
                    bowling.ResetForBall();
                    BeginRunUp();
                }
            }
            else if (timer > (config.IsTutorial ? 0.4f : 0.85f))
            {
                BeginRunUp();
            }
        }

        void BeginRunUp()
        {
            if (config.IsTutorial)
            {
                currentPlan.SpeedKmh = 102f;
                currentPlan.BounceZ = 6.7f;
                currentPlan.LineX = 0.08f;
                currentPlan.Swing = 0f;
                currentPlan.Spin = 0f;
                currentPlan.MarkedNoBall = false;
            }

            ResetDelivery();
            Enter(MatchPhase.RunUp);
        }

        void TickRunUp()
        {
            float along = Mathf.Clamp01(timer / 0.8f);
            float side = currentPlan.RoundTheWicket ? -0.8f : 0.45f;
            StadiumBuilder.SetXZ(world.Bowler, side, Mathf.Lerp(-18f, -12.8f, along));
            if (timer >= 0.8f)
            {
                ReleaseBall();
            }
        }

        void ReleaseBall()
        {
            freeHitThisBall = innings.FreeHitForNextBall();
            if (freeHitThisBall)
            {
                innings.ConsumeFreeHitFlag();
            }

            noBall = currentPlan.MarkedNoBall;
            ball.Release(currentPlan, config.Pitch, config.WindKmh * 0.03f);
            board.SetSpeed(currentPlan.SpeedKmh);
            if (world.BigScreen != null)
            {
                world.BigScreen.text = Mathf.RoundToInt(currentPlan.SpeedKmh) + " km/h";
            }

            Enter(MatchPhase.Alive);
        }

        void TickAlive(float dt)
        {
            TryResolveShot();
            if (!ballDead && ball.State.Alive)
            {
                ball.Tick(dt, OnSegment);
            }

            if (contact)
            {
                sinceHit += dt;
            }

            Vector3 ballPosition = ball.transform.position;
            fielding.Tick(dt, ballPosition, contact && !ballDead && boundary == 0);
            TickRunners(dt);
            if (userBatting && !shotApplied)
            {
                board.SetTimingNeedle(ball.TimeToContact());
            }

            TryCatch();
            TryPickup();
            ReturnBowler(dt);

            bool settled = ballDead || ball.State.Resting || !ball.State.Alive;
            bool runsDone = !going && desiredRuns <= completedRuns;
            if ((settled && runsDone && timer > 0.3f) || timer > 9f)
            {
                FinishDelivery();
            }
            else if (contact && runningOpen && boundary == 0 && !wicket)
            {
                input.ShowBatting(false);
                input.ShowRunning(true);
            }
        }

        void TryResolveShot()
        {
            if (shotApplied || ballDead)
            {
                return;
            }

            if (userBatting)
            {
                ShotQuery query;
                if (!crossed && batting.TryConsume(innings.StrikerScore, currentPlan, ball.TimeToContact(), WindowScale(), out query))
                {
                    ApplyShot(ShotResolver.Resolve(query));
                }
                else if (crossed)
                {
                    shotApplied = true;
                    runningOpen = !wide && !noBall && !wicket;
                    shotName = batting.ForceLeave || batting.Foot == Footwork.Leave ? "Leave" : "Beaten";
                    board.SetShotName(shotName);
                    board.SetTimingResult("MISS");
                }
            }
            else
            {
                if (!aiDecided && ball.TimeToContact() < 0.22f)
                {
                    aiDecided = true;
                    aiQuery = AiBrains.Bat(
                        ToProfile(innings.StrikerScore),
                        currentPlan,
                        config.Difficulty,
                        random,
                        config.CustomTimingScale);
                }

                if (aiDecided && (crossed || ball.TimeToContact() <= 0.01f))
                {
                    ApplyShot(ShotResolver.Resolve(aiQuery));
                }
            }
        }

        void ApplyShot(ShotResult result)
        {
            shotApplied = true;
            shotName = result.ShotName;
            edge = result.Edge;
            board.SetShotName(shotName);
            board.SetTimingResult(result.Grade.ToString().ToUpperInvariant());
            if (world.Striker != null)
            {
                world.Striker.PlayShot();
            }

            if (result.MadeContact)
            {
                contact = true;
                wide = false;
                runningOpen = true;
                ball.Hit(result.Velocity);
                sinceHit = 0f;
                cameraRig.Mode = result.ElevationDeg > 24f ? CamMode.Sky : CamMode.FollowBall;
                if (MatchAudio.Instance != null)
                {
                    MatchAudio.Instance.PlayBat();
                }

                if (tutorial != null)
                {
                    tutorial.NotifyShot(true);
                }
            }
            else
            {
                runningOpen = true;
                if (tutorial != null)
                {
                    tutorial.NotifyShot(false);
                }
            }
        }

        void OnSegment(Vec3 before, Vec3 after)
        {
            sampleStride = !sampleStride;
            if (sampleStride && samples.Count < 480)
            {
                samples.Add(new Vector3(after.X, after.Y, after.Z));
            }

            if (ballDead)
            {
                return;
            }

            if (!shotApplied && !crossed && !contact)
            {
                Vec3 at;
                if (BallSim.CrossedPlane(before, after, BallSim.ContactZ, out at))
                {
                    crossed = true;
                    if (BallSim.IsWide(at, false))
                    {
                        wide = true;
                    }

                    if (BallSim.IsAboveWaistNoBall(at, ball.State.HasBounced))
                    {
                        noBall = true;
                    }
                }
            }

            if (ball.State.IsThrow)
            {
                bool nearEnd = BallSim.SegmentHitsStumps(before, after, BallSim.StumpZ);
                bool farEnd = BallSim.SegmentHitsStumps(before, after, -BallSim.StumpZ);
                if (nearEnd || farEnd)
                {
                    if (going && RunnerIsShort())
                    {
                        wicket = true;
                        wicketKind = WicketKind.RunOut;
                        runningOpen = false;
                        world.StrikerStumps.Break();
                    }

                    KillBall();
                }

                return;
            }

            if (!contact && !ball.State.HasBeenHit && BallSim.SegmentHitsStumps(before, after, BallSim.StumpZ))
            {
                wicket = true;
                wicketKind = WicketKind.Bowled;
                runningOpen = false;
                world.StrikerStumps.Break();
                KillBall();
                return;
            }

            if (contact)
            {
                int runs = BallSim.BoundaryRuns(before, after, true, ball.State.BouncedSinceHit);
                if (runs > 0)
                {
                    boundary = runs;
                    runningOpen = false;
                    desiredRuns = completedRuns;
                    going = false;
                    KillBall();
                    cameraRig.Mode = CamMode.Sky;
                }
            }
        }

        void TryCatch()
        {
            if (!contact || ballDead || wicket)
            {
                return;
            }

            PlayerAvatar who;
            if (fielding.TryCatch(ball.transform.position, ball.State.BouncedSinceHit, edge, sinceHit, out who))
            {
                wicket = true;
                wicketKind = WicketKind.Caught;
                runningOpen = false;
                who.Celebrate();
                KillBall();
                if (MatchAudio.Instance != null)
                {
                    MatchAudio.Instance.PlayCheer();
                }
            }
        }

        void TryPickup()
        {
            if (ballDead || !contact || boundary > 0 || ball.State.IsThrow)
            {
                return;
            }

            if (!ball.State.HasBounced && ball.transform.position.y > 1.2f)
            {
                return;
            }

            PlayerAvatar nearest = fielding.Nearest(ball.transform.position);
            if (nearest == null)
            {
                return;
            }

            float dx = nearest.transform.position.x - ball.transform.position.x;
            float dz = nearest.transform.position.z - ball.transform.position.z;
            if (dx * dx + dz * dz > 0.8f)
            {
                return;
            }

            if (going || desiredRuns > completedRuns)
            {
                Vector3 target = DangerStump();
                ball.ThrowTo(target, 34f);
            }
            else
            {
                KillBall();
            }
        }

        void TickRunners(float dt)
        {
            if (!runningOpen || wicket || boundary > 0)
            {
                going = false;
                PlaceRunners();
                return;
            }

            if (!going && desiredRuns > completedRuns && !aborting)
            {
                going = true;
                aTowardNegative = aIsStriker;
            }

            if (!going)
            {
                PlaceRunners();
                return;
            }

            float speed = 9.2f;
            float direction = aTowardNegative ? -1f : 1f;
            endA += direction * speed * dt;
            endB -= direction * speed * dt;
            bool aHome = aTowardNegative ? endA <= -8.7f : endA >= 8.7f;
            bool bHome = aTowardNegative ? endB >= 8.7f : endB <= -8.7f;
            if (aHome && bHome)
            {
                endA = Mathf.Clamp(endA, -8.7f, 8.7f);
                endB = Mathf.Clamp(endB, -8.7f, 8.7f);
                going = false;
                if (!aborting)
                {
                    completedRuns += 1;
                    aIsStriker = !aIsStriker;
                }

                aborting = false;
            }

            PlaceRunners();
        }

        void PlaceRunners()
        {
            float strikerZ = aIsStriker ? endA : endB;
            float nonZ = aIsStriker ? endB : endA;
            StadiumBuilder.SetXZ(world.Striker, 0f, strikerZ);
            StadiumBuilder.SetXZ(world.NonStriker, 0f, nonZ);
            if (world.Striker != null)
            {
                world.Striker.SetRunning(going);
            }

            if (world.NonStriker != null)
            {
                world.NonStriker.SetRunning(going);
            }
        }

        bool RunnerIsShort()
        {
            if (!going)
            {
                return false;
            }

            float strikerZ = aIsStriker ? endA : endB;
            bool toNon = aIsStriker ? aTowardNegative : !aTowardNegative;
            if (toNon)
            {
                return strikerZ > -8.5f;
            }

            return strikerZ < 8.5f;
        }

        Vector3 DangerStump()
        {
            bool toNon = !going || (aIsStriker ? aTowardNegative : !aTowardNegative);
            if (!going)
            {
                toNon = true;
            }

            return toNon ? new Vector3(0f, 0.4f, -BallSim.StumpZ) : new Vector3(0f, 0.4f, BallSim.StumpZ);
        }

        void CallRun()
        {
            if (!runningOpen || ballDead || boundary > 0 || wicket)
            {
                return;
            }

            desiredRuns = Mathf.Min(3, desiredRuns + 1);
            if (tutorial != null)
            {
                tutorial.NotifyRun();
            }
        }

        void CancelRun()
        {
            if (!going)
            {
                desiredRuns = completedRuns;
                return;
            }

            float traveled = Mathf.Abs((aIsStriker ? endA : endB) - (aTowardNegative ? 8.7f : -8.7f));
            if (traveled < 7f)
            {
                aborting = true;
                aTowardNegative = !aTowardNegative;
                desiredRuns = completedRuns;
            }
        }

        void FinishDelivery()
        {
            if (ballResolved || Phase != MatchPhase.Alive)
            {
                return;
            }

            ballResolved = true;
            KillBall();
            lastRecord = BuildRecord();
            int oversBefore = innings.CompletedOvers;
            bool battingThisBall = userBatting;
            innings.Commit(lastRecord);
            overJustEnded = innings.CompletedOvers != oversBefore;
            if (battingThisBall)
            {
                battingRuns += lastRecord.BatRuns;
            }

            Talk(lastRecord);
            if (tutorial != null && tutorial.Step == 3 && (boundary >= 4 || completedRuns > 0))
            {
                tutorial.NotifyRun();
            }

            RefreshHud();
            Enter(MatchPhase.Umpire);
        }

        BallRecord BuildRecord()
        {
            BallRecord record = new BallRecord();
            record.FreeHitActive = freeHitThisBall;
            record.OutIsStriker = true;
            int running = boundary > 0 ? 0 : completedRuns;
            if (noBall)
            {
                record.Extra = ExtraKind.NoBall;
                record.ExtraRuns = 1;
            }
            else if (wide && !contact)
            {
                record.Extra = ExtraKind.Wide;
                record.ExtraRuns = 1;
            }

            if (wicket)
            {
                record.Wicket = true;
                record.WicketKind = wicketKind;
                if (wicketKind == WicketKind.RunOut)
                {
                    record.BatRuns = running;
                }
            }
            else if (record.Extra == ExtraKind.None && !contact && running > 0)
            {
                record.Extra = ExtraKind.Bye;
                record.ExtraRuns = running;
            }
            else if (record.Extra != ExtraKind.Wide)
            {
                record.BatRuns = boundary > 0 ? boundary : (contact ? running : 0);
            }

            record.Symbol = InningsModel.MakeSymbol(record);
            return record;
        }

        void ShowUmpire()
        {
            string signal = "DOT";
            if (InningsModel.WicketCounts(lastRecord))
            {
                signal = "OUT";
                cameraRig.Shake = 1f;
                Haptics.Pulse();
                if (MatchAudio.Instance != null)
                {
                    MatchAudio.Instance.PlayStumps();
                }
            }
            else if (boundary == 6)
            {
                signal = "SIX";
                Haptics.Pulse();
                if (MatchAudio.Instance != null)
                {
                    MatchAudio.Instance.PlayCheer();
                }
            }
            else if (boundary == 4)
            {
                signal = "FOUR";
                if (MatchAudio.Instance != null)
                {
                    MatchAudio.Instance.PlayCheer();
                }
            }
            else if (lastRecord.Extra == ExtraKind.Wide)
            {
                signal = "WIDE";
            }
            else if (lastRecord.Extra == ExtraKind.NoBall)
            {
                signal = "NO BALL";
            }

            board.ShowBanner(signal, shotName.Length > 0 ? shotName : boardText());
            if (world.BigScreen != null)
            {
                world.BigScreen.text = signal;
            }

            if (world.Umpire != null && signal != "DOT")
            {
                world.Umpire.Celebrate();
            }
        }

        string boardText()
        {
            return battingTeam.ShortName + " " + innings.Runs + "-" + innings.Wickets;
        }

        void TickHighlight()
        {
            int index = Mathf.Clamp(Mathf.FloorToInt(timer / 0.016f), 0, samples.Count - 1);
            if (samples.Count == 0 || timer > 1.6f || index >= samples.Count - 1 && timer > 0.4f)
            {
                Enter(MatchPhase.Between);
                NextBall();
                return;
            }

            ball.Snap(samples[index]);
        }

        void NextBall()
        {
            board.HideBanner();
            if (tutorial != null && (tutorial.Finished || !tutorial.Active) && config.IsTutorial)
            {
                return;
            }

            if (innings.InningsOver)
            {
                EndInnings();
                return;
            }

            if (overJustEnded)
            {
                MaybeRain();
                bowlerIndex = NextBowler(bowlerIndex);
                innings.CurrentBowler = bowlerIndex;
                shownField = userBatting ? AiField() : bowling.Field;
                fielding.Place(shownField, innings.IsPowerplay);
            }

            bool needBatter = lastRecord.Wicket && InningsModel.WicketCounts(lastRecord) && userBatting;
            SyncBatters();
            ResetCreases();
            if (needBatter)
            {
                selectSingle = true;
                Enter(MatchPhase.SelectBatter);
            }
            else
            {
                selectSingle = false;
                Enter(MatchPhase.Between);
            }
        }

        void EndInnings()
        {
            if (userBatting)
            {
                battingRuns = innings.Runs;
            }
            else
            {
                bowlingWickets += innings.Wickets;
            }

            if (config.IsNets || config.IsTutorial || inningsNumber == 2)
            {
                FinishMatch();
                return;
            }

            firstInnings = innings;
            firstRuns = innings.Runs;
            firstOvers = Mathf.Max(1, innings.TotalOvers);
            chaseTarget = firstRuns + 1;
            inningsNumber = 2;
            StartInnings();
            Enter(MatchPhase.InningsBreak);
        }

        void StartInnings()
        {
            userBatting = inningsNumber == 1 ? config.UserBatsFirst : !config.UserBatsFirst;
            battingTeam = userBatting ? userTeam : oppTeam;
            bowlingTeam = userBatting ? oppTeam : userTeam;
            innings = new InningsModel();
            innings.Batters = battingTeam.ToBatters();
            innings.TotalOvers = config.Overs;
            innings.BattingTeam = battingTeam.Name;
            innings.BattingShort = battingTeam.ShortName;
            innings.BowlingTeam = bowlingTeam.Name;
            innings.Target = inningsNumber == 2 ? chaseTarget : -1;
            innings.Bowlers = new BowlerSpell[bowlingTeam.Players.Length];
            for (int i = 0; i < bowlingTeam.Players.Length; i++)
            {
                BowlerSpell spell = new BowlerSpell();
                spell.Name = bowlingTeam.Players[i].Name;
                spell.PlayerIndex = i;
                innings.Bowlers[i] = spell;
            }

            bowlerIndex = BestBowler();
            innings.CurrentBowler = bowlerIndex;
            selectSingle = false;
            SpawnPlayers();
            if (!userBatting || config.IsTutorial || config.IsNets)
            {
                innings.SetOpeners(0, 1);
            }
            else
            {
                innings.Striker = 0;
                innings.NonStriker = 1;
            }

            SyncBatters();
            shownField = userBatting ? AiField() : FieldPlan.Balanced;
            fielding.Place(shownField, true);
            RefreshHud();
            if (innings.IsPowerplay)
            {
                board.SetCommentary(CommentaryBank.Pick("powerplay", string.Empty));
            }
        }

        void SpawnPlayers()
        {
            ClearAvatar(world.Striker);
            ClearAvatar(world.NonStriker);
            ClearAvatar(world.Bowler);
            ClearAvatar(world.Keeper);
            ClearAvatar(world.Umpire);
            if (world.Fielders != null)
            {
                for (int i = 0; i < world.Fielders.Length; i++)
                {
                    ClearAvatar(world.Fielders[i]);
                }
            }

            StadiumBuilder.SpawnPlayers(world, transform, battingTeam.Color, battingTeam.Accent, bowlingTeam.Color, bowlingTeam.Accent);
            fielding.Bind(world.Fielders, world.Keeper, world.Bowler);
            world.Umpire.SetIdentity("Umpire", 0, true);
        }

        static void ClearAvatar(PlayerAvatar avatar)
        {
            if (avatar != null)
            {
                Destroy(avatar.gameObject);
            }
        }

        void OnBattersPicked(int striker, int non)
        {
            if (selectSingle)
            {
                if (striker != innings.Striker && striker >= 0 && !innings.Batters[striker].Out)
                {
                    BatterScore swap = innings.Batters[innings.Striker];
                    innings.Batters[innings.Striker] = innings.Batters[striker];
                    innings.Batters[striker] = swap;
                    innings.Batters[innings.Striker].HasBatted = true;
                    innings.Batters[innings.Striker].Out = false;
                    if (!innings.Batters[striker].Out)
                    {
                        innings.Batters[striker].HasBatted = false;
                    }
                }
            }
            else
            {
                innings.SetOpeners(striker, non);
            }

            battersReady = true;
            board.HideBatterSelect();
        }

        void SyncBatters()
        {
            if (innings == null || world.Striker == null)
            {
                return;
            }

            BatterScore striker = innings.Batters[innings.Striker];
            BatterScore other = innings.Batters[innings.NonStriker];
            world.Striker.SetIdentity(striker.Name, innings.Striker + 1, striker.RightHanded);
            world.NonStriker.SetIdentity(other.Name, innings.NonStriker + 1, other.RightHanded);
            PlayerSeed bowler = bowlingTeam.Players[bowlerIndex];
            world.Bowler.SetIdentity(bowler.Name, bowlerIndex + 1, !bowler.LeftArm);
            if (world.Keeper != null)
            {
                int keeper = KeeperIndex();
                world.Keeper.SetIdentity(bowlingTeam.Players[keeper].Name, keeper + 1, true);
            }

            if (world.Fielders != null)
            {
                int fielder = 0;
                for (int i = 0; i < bowlingTeam.Players.Length && fielder < world.Fielders.Length; i++)
                {
                    if (i == bowlerIndex || bowlingTeam.Players[i].Keeper)
                    {
                        continue;
                    }

                    world.Fielders[fielder].SetIdentity(bowlingTeam.Players[i].Name, i + 1, bowlingTeam.Players[i].RightHanded);
                    fielder += 1;
                }
            }

            ResetCreases();
        }

        void ResetCreases()
        {
            endA = 8.7f;
            endB = -8.7f;
            aIsStriker = true;
            going = false;
            StadiumBuilder.PlaceIdle(world);
            StadiumBuilder.Face(world.Striker, new Vector3(0f, 0f, -1f));
            StadiumBuilder.Face(world.NonStriker, new Vector3(0f, 0f, 1f));
        }

        void ResetDelivery()
        {
            shotApplied = false;
            contact = false;
            crossed = false;
            wide = false;
            noBall = false;
            wicket = false;
            edge = false;
            ballDead = false;
            ballResolved = false;
            runningOpen = false;
            going = false;
            aborting = false;
            aiDecided = false;
            wicketKind = WicketKind.None;
            boundary = 0;
            desiredRuns = 0;
            completedRuns = 0;
            sinceHit = 0f;
            shotName = string.Empty;
            endA = 8.7f;
            endB = -8.7f;
            aIsStriker = true;
            samples.Clear();
            sampleStride = false;
            world.StrikerStumps.Reset();
            world.BowlerStumps.Reset();
            board.SetShotName(string.Empty);
            board.SetTimingResult(string.Empty);
        }

        void PrepareAiDelivery()
        {
            PlayerSeed bowler = bowlingTeam.Players[bowlerIndex];
            BowlerProfile profile = new BowlerProfile();
            profile.Name = bowler.Name;
            profile.Skill = bowler.Bowl;
            profile.Stock = bowler.Stock;
            profile.LeftArm = bowler.LeftArm;
            profile.BaseSpeedKmh = bowler.Speed;
            currentPlan = AiBrains.Bowl(
                profile,
                ToProfile(innings.StrikerScore),
                config.Difficulty,
                config.Pitch,
                random,
                config.CustomSpray,
                innings.IsPowerplay);
            shownField = AiField();
        }

        FieldPlan AiField()
        {
            if (config.Difficulty == Difficulty.Easy)
            {
                return FieldPlan.Defensive;
            }

            if (config.Difficulty == Difficulty.Hardcore || config.Difficulty == Difficulty.Expert)
            {
                return FieldPlan.Attacking4;
            }

            if (config.Difficulty == Difficulty.Hard)
            {
                return FieldPlan.Attacking2;
            }

            return FieldPlan.Balanced;
        }

        void MaybeRain()
        {
            if (config.RainPercent <= 0 || innings.CompletedOvers == 0)
            {
                return;
            }

            int roll = random.Next(0, 100);
            if (roll > config.RainPercent / 2)
            {
                raining = config.RainPercent >= 50 && roll < config.RainPercent;
                ApplyRain();
                return;
            }

            int cut = innings.TotalOvers - 1;
            if (cut <= innings.CompletedOvers)
            {
                return;
            }

            raining = true;
            innings.TotalOvers = cut;
            if (inningsNumber == 2 && firstRuns >= 0)
            {
                innings.Target = DlsLite.RevisedTarget(firstRuns, firstOvers, cut);
                chaseTarget = innings.Target;
            }

            ApplyRain();
            board.SetCommentary(CommentaryBank.Pick("rain", ground.Name));
            board.ShowBanner("Rain delay", "Overs cut to " + cut + (innings.Target > 0 ? ". Target " + innings.Target : "."));
        }

        void Talk(BallRecord record)
        {
            string subject = innings.StrikerScore.Name;
            string key = "dot";
            if (InningsModel.WicketCounts(record))
            {
                key = record.WicketKind == WicketKind.Caught ? "caught" : (record.WicketKind == WicketKind.RunOut ? "runout" : "bowled");
            }
            else if (boundary == 6)
            {
                key = "six";
            }
            else if (boundary == 4)
            {
                key = "four";
            }
            else if (record.Extra == ExtraKind.Wide)
            {
                key = "wide";
            }
            else if (record.Extra == ExtraKind.NoBall)
            {
                key = "noball";
            }
            else if (record.BatRuns == 1)
            {
                key = "single";
            }
            else if (record.BatRuns == 2)
            {
                key = "two";
            }
            else if (record.BatRuns == 3)
            {
                key = "three";
            }
            else if (shotName == "Leave")
            {
                key = "leave";
            }

            board.SetCommentary(CommentaryBank.Pick(key, subject));
        }

        void FinishMatch()
        {
            if (matchEnded)
            {
                return;
            }

            matchEnded = true;
            Phase = MatchPhase.Done;
            MatchResult result = BuildResult();
            if (!config.IsTutorial && GameManager.Instance != null)
            {
                result.LevelsGained = GameManager.Instance.AddXp(result.Xp);
                GameManager.Instance.AddCoins(result.Coins);
                GameManager.Instance.Profile.matchesPlayed += 1;
                if (result.UserWon)
                {
                    GameManager.Instance.Profile.wins += 1;
                }

                if (result.Scorecard != null && battingRuns > GameManager.Instance.Profile.bestScore)
                {
                    GameManager.Instance.Profile.bestScore = battingRuns;
                }

                result.Level = GameManager.Instance.Profile.level;
                GameManager.Instance.Save();
            }

            if (MenuFlow.Instance != null)
            {
                MenuFlow.Instance.ShowResult(result);
            }

            Cleanup();
        }

        MatchResult BuildResult()
        {
            MatchResult result = new MatchResult();
            result.Config = config;
            result.Practice = config.IsNets;
            result.Scorecard = Scorecard();
            result.PlayerOfTheMatch = PickAward();
            if (config.IsNets)
            {
                result.Title = "Net session";
                result.Summary = battingTeam.ShortName + " " + innings.Runs + "-" + innings.Wickets + " (" + innings.OversText + ")";
                result.Coins = 20 + innings.Runs / 4;
                result.Xp = 8;
                return result;
            }

            bool tie = firstRuns >= 0 && innings.Runs == firstRuns && !innings.ChaseAchieved;
            bool secondBattingWon = innings.ChaseAchieved;
            bool userWon = !tie && (secondBattingWon ? !config.UserBatsFirst : config.UserBatsFirst);
            result.Tie = tie;
            result.UserWon = userWon;
            string winner = userWon ? userTeam.Name : oppTeam.Name;
            if (tie)
            {
                result.Title = "Match tied";
                result.Summary = "Both sides finish on " + firstRuns + ".";
            }
            else if (secondBattingWon)
            {
                int left = 10 - innings.Wickets;
                result.Title = winner + " win";
                result.Summary = "Won by " + left + " wicket" + (left == 1 ? string.Empty : "s") + ".";
            }
            else
            {
                int margin = Mathf.Max(0, firstRuns - innings.Runs);
                result.Title = winner + " win";
                result.Summary = "Won by " + margin + " runs.";
            }

            int rewardRuns = battingRuns;
            int rewardWickets = bowlingWickets;
            result.Xp = 18 + rewardRuns / 5 + rewardWickets * 8 + (userWon ? 22 : 0) + (tie ? 10 : 0);
            result.Coins = 30 + rewardRuns / 3 + rewardWickets * 12 + (userWon ? 70 : 0) + (tie ? 30 : 0);
            if (GameManager.Instance != null && GameManager.Instance.Profile.blitzPass)
            {
                result.Coins = Mathf.RoundToInt(result.Coins * 1.1f);
            }

            return result;
        }

        string Scorecard()
        {
            StringBuilder builder = new StringBuilder();
            if (firstInnings != null)
            {
                AppendInnings(builder, firstInnings);
            }

            if (innings != null && innings != firstInnings)
            {
                AppendInnings(builder, innings);
            }

            return builder.ToString();
        }

        static void AppendInnings(StringBuilder builder, InningsModel model)
        {
            if (model == null)
            {
                return;
            }

            builder.AppendLine(model.BattingShort + " " + model.Runs + "-" + model.Wickets + " (" + model.OversText + ")");
            for (int i = 0; i < model.Batters.Length; i++)
            {
                BatterScore batter = model.Batters[i];
                if (!batter.HasBatted)
                {
                    continue;
                }

                builder.AppendLine(batter.Name + "  " + batter.Runs + "(" + batter.Balls + ")" + (batter.Out ? string.Empty : " not out"));
            }

            builder.AppendLine();
        }

        string PickAward()
        {
            string name = userTeam.Players[0].Name;
            int best = -1;
            Consider(innings, ref name, ref best);
            Consider(firstInnings, ref name, ref best);
            return name;
        }

        static void Consider(InningsModel model, ref string name, ref int best)
        {
            if (model == null)
            {
                return;
            }

            for (int i = 0; i < model.Batters.Length; i++)
            {
                if (model.Batters[i].Runs > best)
                {
                    best = model.Batters[i].Runs;
                    name = model.Batters[i].Name;
                }
            }
        }

        void ResolveToss(bool heads)
        {
            bool landedHeads = random.NextDouble() < 0.5;
            bool won = heads == landedHeads;
            board.HideChoices();
            if (won)
            {
                board.ShowChoices(
                    userTeam.ShortName + " win the toss",
                    "Bat or bowl?",
                    "Bat",
                    "Bowl",
                    () => ChooseToss(true),
                    () => ChooseToss(false));
            }
            else
            {
                bool aiBats = random.NextDouble() < 0.4;
                config.UserBatsFirst = !aiBats;
                string winner = oppTeam.Name;
                board.ShowBanner(
                    winner + " win the toss",
                    aiBats ? "They will bat." : "They will bowl.");
                board.SetCommentary(CommentaryBank.Pick(aiBats ? "toss_bat" : "toss_bowl", oppTeam.Name));
                StartInnings();
                Enter(MatchPhase.Walkout);
            }
        }

        void ChooseToss(bool bat)
        {
            config.UserBatsFirst = bat;
            board.HideChoices();
            board.SetCommentary(CommentaryBank.Pick(bat ? "toss_bat" : "toss_bowl", userTeam.Name));
            StartInnings();
            Enter(MatchPhase.Walkout);
        }

        public void Pause()
        {
            if (paused || config.IsTutorial)
            {
                return;
            }

            paused = true;
            Time.timeScale = 0f;
            board.ShowPause(
                "J K L shot, R run, Enter bowl, Esc resume",
                Resume,
                Restart,
                LeaveToMenu);
        }

        public void Resume()
        {
            paused = false;
            Time.timeScale = 1f;
            board.HidePause();
        }

        public void Restart()
        {
            MatchConfig again = config;
            Resume();
            Cleanup();
            if (MenuFlow.Instance != null)
            {
                MenuFlow.Instance.HideForMatch();
            }

            Launch(again);
        }

        public void LeaveToMenu()
        {
            Resume();
            Cleanup();
            if (MenuFlow.Instance != null)
            {
                MenuFlow.Instance.ReturnToMenu();
            }
        }

        public void LeaveToQuickMatch()
        {
            Resume();
            Cleanup();
            if (MenuFlow.Instance != null)
            {
                MenuFlow.Instance.OpenQuickMatch();
            }
        }

        void Cleanup()
        {
            Time.timeScale = 1f;
            if (cameraRig != null)
            {
                Destroy(cameraRig);
            }

            if (MatchAudio.Instance != null)
            {
                MatchAudio.Instance.StopCrowd();
            }

            Destroy(gameObject);
        }

        void RefreshHud()
        {
            if (innings == null)
            {
                return;
            }

            board.Refresh(innings, inningsNumber, innings.Target, battingTeam.Color, battingTeam.ShortName);
            board.SetBadges(innings.IsPowerplay, freeHitThisBall, raining);
            if (world.BigScreen != null && Phase != MatchPhase.Umpire)
            {
                world.BigScreen.text = battingTeam.ShortName + " " + innings.Runs + "-" + innings.Wickets;
            }
        }

        void UpdateMarkers()
        {
            float x = currentPlan.LineX;
            float z = currentPlan.BounceZ;
            if (Phase == MatchPhase.Setup && !userBatting)
            {
                x = bowling.LineX;
                z = bowling.BounceZ;
            }

            if (world.PitchMarker != null)
            {
                world.PitchMarker.position = new Vector3(x, 0.12f, z);
            }

            if (world.ArrivalMarker != null)
            {
                world.ArrivalMarker.position = new Vector3(x + currentPlan.Swing * 0.45f, 0.25f, 8.9f);
            }
        }

        void ApplyAtmosphere()
        {
            Color sky;
            Color sun;
            float intensity;
            float tilt;
            bool floods;
            switch (config.TimeOfDay)
            {
                case TimeOfDay.Morning:
                    sky = new Color(0.55f, 0.72f, 0.86f);
                    sun = new Color(1f, 0.9f, 0.75f);
                    intensity = 1.05f;
                    tilt = 28f;
                    floods = false;
                    break;
                case TimeOfDay.Evening:
                    sky = new Color(0.85f, 0.55f, 0.32f);
                    sun = new Color(1f, 0.7f, 0.4f);
                    intensity = 0.8f;
                    tilt = 16f;
                    floods = false;
                    break;
                case TimeOfDay.Dusk:
                    sky = new Color(0.32f, 0.28f, 0.45f);
                    sun = new Color(1f, 0.55f, 0.35f);
                    intensity = 0.4f;
                    tilt = 8f;
                    floods = true;
                    break;
                case TimeOfDay.Night:
                    sky = new Color(0.05f, 0.07f, 0.12f);
                    sun = new Color(0.65f, 0.72f, 0.85f);
                    intensity = 0.12f;
                    tilt = 70f;
                    floods = true;
                    break;
                default:
                    sky = new Color(0.45f, 0.68f, 0.9f);
                    sun = Color.white;
                    intensity = 1.2f;
                    tilt = 55f;
                    floods = false;
                    break;
            }

            if (world.Sun != null)
            {
                world.Sun.color = sun;
                world.Sun.intensity = intensity;
                world.Sun.transform.rotation = Quaternion.Euler(tilt, -35f, 0f);
                world.Sun.shadows = LightShadows.None;
            }

            cameraRig.SetBackground(sky);
            RenderSettings.ambientLight = sky * 0.55f;
            if (world.Floods != null)
            {
                bool quality = GameManager.Instance == null || GameManager.Instance.Profile.settings.quality > 0;
                for (int i = 0; i < world.Floods.Length; i++)
                {
                    world.Floods[i].enabled = floods && quality;
                }
            }

            ApplyRain();
        }

        void ApplyRain()
        {
            if (world.Rain == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = world.Rain.emission;
            emission.rateOverTime = raining ? 60f : 0f;
            if (raining && !world.Rain.isPlaying)
            {
                world.Rain.Play();
            }

            if (!raining && world.Rain.isPlaying)
            {
                world.Rain.Stop();
            }
        }

        void TickAvatars(float dt)
        {
            TickOne(world.Striker, dt);
            TickOne(world.NonStriker, dt);
            TickOne(world.Bowler, dt);
            TickOne(world.Keeper, dt);
            TickOne(world.Umpire, dt);
            if (world.Fielders == null)
            {
                return;
            }

            for (int i = 0; i < world.Fielders.Length; i++)
            {
                TickOne(world.Fielders[i], dt);
            }
        }

        static void TickOne(PlayerAvatar avatar, float dt)
        {
            if (avatar != null)
            {
                avatar.Tick(dt);
            }
        }

        void ReturnBowler(float dt)
        {
            if (world.Bowler == null || Phase != MatchPhase.Alive)
            {
                return;
            }

            Vector3 position = world.Bowler.transform.localPosition;
            Vector3 back = Vector3.MoveTowards(new Vector3(position.x, 0f, position.z), new Vector3(0.4f, 0f, -14.5f), 6f * dt);
            StadiumBuilder.SetXZ(world.Bowler, back.x, back.z);
        }

        void KillBall()
        {
            ballDead = true;
            ball.StopDead();
        }

        float WindowScale()
        {
            if (config.IsTutorial)
            {
                return 1.7f;
            }

            return AiBrains.TimingScale(config.Difficulty, config.CustomTimingScale);
        }

        int BestBowler()
        {
            int best = 0;
            int skill = -1;
            for (int i = 0; i < bowlingTeam.Players.Length; i++)
            {
                if (bowlingTeam.Players[i].Bowl > skill)
                {
                    skill = bowlingTeam.Players[i].Bowl;
                    best = i;
                }
            }

            return best;
        }

        int NextBowler(int previous)
        {
            for (int step = 1; step <= bowlingTeam.Players.Length; step++)
            {
                int index = (previous + step) % bowlingTeam.Players.Length;
                if (bowlingTeam.Players[index].Bowl >= 55)
                {
                    return index;
                }
            }

            return previous;
        }

        int KeeperIndex()
        {
            for (int i = 0; i < bowlingTeam.Players.Length; i++)
            {
                if (bowlingTeam.Players[i].Keeper)
                {
                    return i;
                }
            }

            return 5;
        }

        string ConditionsText()
        {
            int temp = 28;
            switch (config.TimeOfDay)
            {
                case TimeOfDay.Morning: temp = 22; break;
                case TimeOfDay.Evening: temp = 27; break;
                case TimeOfDay.Dusk: temp = 24; break;
                case TimeOfDay.Night: temp = 21; break;
                default: temp = 31; break;
            }

            if (config.RainPercent >= 50)
            {
                temp -= 3;
            }

            string weather = config.RainPercent >= 75 ? "Heavy cloud" : (config.RainPercent >= 25 ? "Cloudy" : "Clear");
            return "Pitch " + config.Pitch + "   " + weather + "   " + temp + "°C\nWind "
                + Mathf.RoundToInt(config.WindKmh) + " km/h   Rain " + config.RainPercent + "%   "
                + config.Overs + " overs   " + config.Difficulty;
        }

        static BatterProfile ToProfile(BatterScore batter)
        {
            BatterProfile profile = new BatterProfile();
            profile.Name = batter.Name;
            profile.Technique = batter.Technique;
            profile.Timing = batter.Timing;
            profile.Type = batter.Type;
            profile.RightHanded = batter.RightHanded;
            return profile;
        }
    }
}
