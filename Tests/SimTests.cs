using System;
using System.Globalization;
using CricketBlitz.Sim;

internal static class SimTests
{
    static int failed;
    static int passed;

    static void Check(bool condition, string name)
    {
        if (condition)
        {
            passed += 1;
            Console.WriteLine("  PASS  " + name);
        }
        else
        {
            failed += 1;
            Console.WriteLine("  FAIL  " + name);
        }
    }

    static InningsModel NewInnings(int overs, int batters)
    {
        InningsModel innings = new InningsModel();
        innings.TotalOvers = overs;
        innings.BattingShort = "HBR";
        innings.Batters = new BatterScore[batters];
        for (int i = 0; i < batters; i++)
        {
            innings.Batters[i] = new BatterScore();
            innings.Batters[i].Name = "Batter " + (i + 1).ToString(CultureInfo.InvariantCulture);
        }

        innings.Bowlers = new BowlerSpell[1];
        innings.Bowlers[0] = new BowlerSpell();
        innings.Bowlers[0].Name = "Dale Voss";
        innings.SetOpeners(0, 1);
        return innings;
    }

    static BallRecord Runs(int batRuns)
    {
        BallRecord record = new BallRecord();
        record.BatRuns = batRuns;
        return record;
    }

    static void TestScoring()
    {
        Console.WriteLine("Scoring");
        InningsModel innings = NewInnings(5, 11);
        innings.Commit(Runs(1));
        Check(innings.Runs == 1, "single adds a run");
        Check(innings.Striker == 1, "single swaps strike");
        Check(innings.Batters[0].Runs == 1, "single is credited to the batter who faced");
        Check(innings.Batters[0].Balls == 1, "single counts as a ball faced");
        Check(innings.LegalBalls == 1, "single is a legal ball");
        Check(innings.LastSix[0] == "1", "ticker shows 1");

        innings.Commit(Runs(4));
        Check(innings.Striker == 1, "four keeps the new strike");
        Check(innings.Batters[1].Runs == 4, "four credited to striker");
        Check(innings.Batters[1].Fours == 1, "four is counted");
        Check(innings.Runs == 5, "team total includes the four");

        innings.Commit(Runs(6));
        Check(innings.Striker == 1, "six does not swap strike");
        Check(innings.Batters[1].Sixes == 1, "six is counted");

        BallRecord wide = new BallRecord();
        wide.Extra = ExtraKind.Wide;
        wide.ExtraRuns = 1;
        innings.Commit(wide);
        Check(innings.Runs == 12, "wide adds one");
        Check(innings.Extras == 1, "wide counts as an extra");
        Check(innings.LegalBalls == 3, "wide is not a legal ball");
        Check(innings.LastSix[3] == "Wd", "ticker shows Wd");
        Check(!innings.FreeHitPending, "wide does not arm a free hit");

        BallRecord noBall = new BallRecord();
        noBall.Extra = ExtraKind.NoBall;
        noBall.ExtraRuns = 1;
        noBall.BatRuns = 4;
        innings.Commit(noBall);
        Check(innings.Runs == 17, "no-ball four is 5 team runs");
        Check(innings.Batters[1].Runs == 14, "bat runs on a no-ball stick");
        Check(innings.Batters[1].Balls == 2, "no-ball is not a ball faced");
        Check(innings.FreeHitPending, "no-ball arms a free hit");
        Check(innings.LastSix[4] == "5Nb", "ticker shows 5Nb");

        BallRecord freeHitBowled = new BallRecord();
        freeHitBowled.FreeHitActive = true;
        freeHitBowled.Wicket = true;
        freeHitBowled.WicketKind = WicketKind.Bowled;
        freeHitBowled.OutIsStriker = true;
        int wickets = innings.Wickets;
        innings.ConsumeFreeHitFlag();
        innings.Commit(freeHitBowled);
        Check(innings.Wickets == wickets, "free hit ignores a bowled appeal");
        Check(!innings.FreeHitPending, "free hit was consumed");
        Check(innings.LastSix[5] == "0", "free-hit dot is a dot");

        BallRecord freeHitRunOut = new BallRecord();
        freeHitRunOut.FreeHitActive = true;
        freeHitRunOut.Wicket = true;
        freeHitRunOut.WicketKind = WicketKind.RunOut;
        freeHitRunOut.OutIsStriker = true;
        freeHitRunOut.BatRuns = 1;
        innings.Commit(freeHitRunOut);
        Check(innings.Wickets == wickets + 1, "free hit still allows a run out");
        Check(innings.LastSix[5] == "W", "run out prints W");
        Check(innings.Bowlers[0].Wickets == 0, "run out is not the bowler's wicket");
    }

    static void TestOverAndInnings()
    {
        Console.WriteLine("Overs");
        InningsModel maiden = NewInnings(5, 11);
        for (int i = 0; i < 6; i++)
        {
            maiden.Commit(Runs(0));
        }

        Check(maiden.CompletedOvers == 1, "six balls complete an over");
        Check(maiden.Bowlers[0].Maidens == 1, "a blank over is a maiden");
        Check(maiden.Striker == 1, "end of a dot over hands strike to the other opener");
        Check(maiden.OversText == "1.0", "overs text after a full over");

        InningsModel strike = NewInnings(5, 11);
        for (int i = 0; i < 5; i++)
        {
            strike.Commit(Runs(0));
        }

        strike.Commit(Runs(1));
        Check(strike.Striker == 0, "single off the last ball swaps back at the over");
        Check(strike.Bowlers[0].Maidens == 0, "a single spoils the maiden");
        Check(strike.BowlerFigures().Contains("0-1"), "figures include wickets and runs");

        InningsModel chase = NewInnings(5, 11);
        chase.Target = 10;
        chase.Commit(Runs(6));
        Check(!chase.InningsOver, "six does not finish a chase of 10");
        chase.Commit(Runs(4));
        Check(chase.ChaseAchieved, "reaching the target wins the chase");
        Check(chase.InningsOver, "chase closes the innings");

        InningsModel allOut = NewInnings(5, 11);
        for (int i = 0; i < 10; i++)
        {
            BallRecord wicket = new BallRecord();
            wicket.Wicket = true;
            wicket.WicketKind = WicketKind.Bowled;
            wicket.OutIsStriker = true;
            allOut.Commit(wicket);
        }

        Check(allOut.Wickets == 10, "ten wickets are all out");
        Check(allOut.InningsOver, "all out ends the innings");
        int notOut = 0;
        for (int i = 0; i < allOut.Batters.Length; i++)
        {
            if (!allOut.Batters[i].Out)
            {
                notOut += 1;
            }
        }

        Check(notOut == 1, "one batter remains not out");
        Check(allOut.Bowlers[0].Wickets == 10, "bowled wickets credit the bowler");

        InningsModel next = NewInnings(5, 11);
        BallRecord firstWicket = new BallRecord();
        firstWicket.Wicket = true;
        firstWicket.WicketKind = WicketKind.Caught;
        firstWicket.OutIsStriker = true;
        next.Commit(firstWicket);
        Check(next.Striker == 2, "the next batter takes strike");
        Check(next.Batters[0].Out, "caught batter is out");
        Check(next.Batters[0].Runs == 0, "a catch does not award runs");
    }

    static void TestDlsAndPowerplay()
    {
        Console.WriteLine("DLS and powerplay");
        Check(InningsModel.PowerplayOvers(5) == 1, "5-over powerplay is 1");
        Check(InningsModel.PowerplayOvers(10) == 2, "10-over powerplay is 2");
        Check(InningsModel.PowerplayOvers(20) == 6, "20-over powerplay is 6");
        Check(InningsModel.PowerplayOvers(50) == 10, "50-over powerplay is 10");

        int target = DlsLite.RevisedTarget(80, 5, 3);
        Check(target < 81, "a shortened chase is not harder than the full one");
        Check(target < 80, "cutting two overs lowers the target");
        Check(target > 40, "three overs still leave a real chase");
        Check(DlsLite.RevisedTarget(40, 5, 0) == 1, "no overs left sets a nominal target of 1");
    }

    static void TestBall()
    {
        Console.WriteLine("Ball");
        DeliveryPlan plan = DeliveryPlan.Make(DeliveryType.Straight, 130f, 0.1f, 4.5f, false);
        BallState state = BallSim.Release(plan);
        bool stayedUp = true;
        for (int i = 0; i < 900 && state.BounceCount == 0 && state.Alive; i++)
        {
            BallSim.Step(ref state, 0.004f, PitchType.Hard, 0f, 0f, 0f);
            if (state.Pos.Y < 0.03f)
            {
                stayedUp = false;
            }
        }

        Check(state.BounceCount == 1, "straight ball pitches once");
        Check(Math.Abs(state.Pos.X - 0.1f) < 0.45f, "straight ball pitches on the aimed line, x=" + state.Pos.X.ToString("0.00"));
        Check(Math.Abs(state.Pos.Z - 4.5f) < 0.55f, "straight ball pitches on the aimed length, z=" + state.Pos.Z.ToString("0.00"));
        Check(stayedUp, "ball never falls through the ground");

        BallState swung = BallSim.Release(plan);
        BallState plain = BallSim.Release(plan);
        for (int i = 0; i < 900 && swung.BounceCount == 0; i++)
        {
            BallSim.Step(ref swung, 0.004f, PitchType.Green, 1f, 0f, 0f);
        }

        for (int i = 0; i < 900 && plain.BounceCount == 0; i++)
        {
            BallSim.Step(ref plain, 0.004f, PitchType.Green, 0f, 0f, 0f);
        }

        Check(swung.Pos.X > plain.Pos.X + 0.05f, "out-swing lands wider than a straight ball");

        float hardBounce = FirstBounceVy(PitchType.Hard);
        float dryBounce = FirstBounceVy(PitchType.Dry);
        Check(hardBounce > dryBounce, "a hard pitch bounces higher than a dry one");

        Check(BallSim.SegmentHitsStumps(new Vec3(0f, 0.4f, 9f), new Vec3(0f, 0.4f, 11f), BallSim.StumpZ), "ball on the stumps is out");
        Check(!BallSim.SegmentHitsStumps(new Vec3(0.5f, 0.4f, 9f), new Vec3(0.5f, 0.4f, 11f), BallSim.StumpZ), "a wide line misses the stumps");
        Check(!BallSim.SegmentHitsStumps(new Vec3(0f, 1.4f, 9f), new Vec3(0f, 1.4f, 11f), BallSim.StumpZ), "a ball over the bails misses");

        int six = BallSim.BoundaryRuns(new Vec3(0f, 3f, 54f), new Vec3(0f, 2.4f, 58f), true, false);
        int four = BallSim.BoundaryRuns(new Vec3(0f, 0.2f, 54f), new Vec3(0f, 0.15f, 58f), true, true);
        int none = BallSim.BoundaryRuns(new Vec3(0f, 0.2f, 54f), new Vec3(0f, 0.15f, 58f), false, false);
        Check(six == 6, "airborne boundary is a six");
        Check(four == 4, "grounded boundary is a four");
        Check(none == 0, "an unstruck ball is not a boundary");

        Check(BallSim.IsWide(new Vec3(0.8f, 0.6f, BallSim.ContactZ), false), "a passing wide is a wide");
        Check(!BallSim.IsWide(new Vec3(0.8f, 0.6f, BallSim.ContactZ), true), "a hit wide is played");
        Check(BallSim.IsAboveWaistNoBall(new Vec3(0f, 1.5f, BallSim.ContactZ), false), "a full toss above the waist is a no-ball");
    }

    static float FirstBounceVy(PitchType pitch)
    {
        BallState state = new BallState();
        state.Pos = new Vec3(0f, 3f, 0f);
        state.Vel = new Vec3(0f, 0f, 0f);
        state.Alive = true;
        for (int i = 0; i < 400; i++)
        {
            int before = state.BounceCount;
            BallSim.Step(ref state, 0.005f, pitch, 0f, 0f, 0f);
            if (state.BounceCount > before)
            {
                return state.Vel.Y;
            }
        }

        return 0f;
    }

    static void TestShots()
    {
        Console.WriteLine("Shots");
        ShotQuery query = new ShotQuery();
        query.Technique = 80;
        query.TimingSkill = 80;
        query.Type = BatsmanType.Balanced;
        query.Family = ShotFamily.Stroke;
        query.Foot = Footwork.FrontFoot;
        query.AimAngleDeg = 0f;
        query.BounceZ = 5f;
        query.IncomingSpeedKmh = 130f;
        query.DifficultyWindowScale = 1f;
        query.StrikerRightHanded = true;
        query.TimeError = 0f;
        ShotResult perfect = ShotResolver.Resolve(query);

        query.TimeError = 0.12f;
        ShotResult late = ShotResolver.Resolve(query);

        query.TimeError = 0f;
        query.Foot = Footwork.Leave;
        ShotResult leave = ShotResolver.Resolve(query);

        Check(perfect.Grade == TimingGrade.Perfect, "zero error is perfect");
        Check(perfect.MadeContact, "perfect timing makes contact");
        Check(perfect.ShotName == "Straight Drive", "straight stroke is a straight drive");
        Check(late.Grade == TimingGrade.Late, "a small delay is late");
        Check(perfect.Velocity.Magnitude > late.Velocity.Magnitude, "perfect contact is faster than late contact");
        Check(!leave.MadeContact && leave.ShotName == "Leave", "leave does not contact");

        Check(ShotResolver.NameShot(-80f, ShotFamily.Stroke, Footwork.BackFoot, 1f) == "Pull", "back-foot leg shot is a pull");
        Check(ShotResolver.NameShot(-80f, ShotFamily.Push, Footwork.FrontFoot, 6f) == "Sweep", "a pushed leg shot is a sweep");
        Check(ShotResolver.NameShot(36f, ShotFamily.Stroke, Footwork.FrontFoot, 6.8f) == "Cover Drive", "off-side stroke is a cover drive");

        float easy = AiBrains.Spray(Difficulty.Easy, 0.2f);
        float hardcore = AiBrains.Spray(Difficulty.Hardcore, 0.2f);
        Check(easy > hardcore, "easy bowling is less accurate than hardcore");
        Check(AiBrains.TimingScale(Difficulty.Easy, 1f) > AiBrains.TimingScale(Difficulty.Hardcore, 1f), "easy gives the batter a wider window");

        Random random = new Random(7);
        BowlerProfile bowler = new BowlerProfile();
        bowler.Name = "Caleb Nyx";
        bowler.Skill = 40;
        bowler.Stock = DeliveryType.OutSwing;
        bowler.BaseSpeedKmh = 132f;
        BatterProfile batter = new BatterProfile();
        batter.Name = "Adrian Cole";
        batter.Technique = 70;
        batter.Timing = 70;
        batter.Type = BatsmanType.Balanced;
        batter.RightHanded = true;
        DeliveryPlan easyBall = AiBrains.Bowl(bowler, batter, Difficulty.Easy, PitchType.Hard, random, 0.3f, true);
        Check(easyBall.SpeedKmh > 70f && easyBall.SpeedKmh < 160f, "AI pace stays in a legal range");
        Check(easyBall.BowlerName == "Caleb Nyx", "delivery remembers the bowler");
    }

    static int Main()
    {
        TestScoring();
        TestOverAndInnings();
        TestDlsAndPowerplay();
        TestBall();
        TestShots();
        Console.WriteLine();
        Console.WriteLine(passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
