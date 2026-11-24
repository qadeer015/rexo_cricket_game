using CricketBlitz.Sim;

namespace CricketBlitz.Match
{
    public sealed class MatchConfig
    {
        public string StadiumId = "harbor_oval";
        public Difficulty Difficulty = Difficulty.Medium;
        public int Overs = 5;
        public PitchType Pitch = PitchType.Hard;
        public bool SkipIntro;
        public TimeOfDay TimeOfDay = TimeOfDay.Afternoon;
        public int RainPercent;
        public bool IsTutorial;
        public bool IsNets;
        public string UserTeamId = "harbor_kings";
        public string OppTeamId = "ridge_falcons";
        public bool UserBatsFirst = true;
        public float CustomSpray = 0.3f;
        public float CustomTimingScale = 1f;
        public float WindKmh = 8f;
    }

    public sealed class MatchResult
    {
        public string Title;
        public string Summary;
        public string Scorecard;
        public int Coins;
        public int Xp;
        public int LevelsGained;
        public int Level;
        public string PlayerOfTheMatch;
        public bool UserWon;
        public bool Tie;
        public bool Practice;
        public MatchConfig Config;
    }
}
