namespace CricketBlitz.Sim
{
    public enum PitchType
    {
        Hard = 0,
        Dry = 1,
        Green = 2
    }

    public enum DeliveryType
    {
        Straight = 0,
        OutSwing = 1,
        InSwing = 2,
        LegCutter = 3,
        OffCutter = 4,
        Slower = 5,
        ReverseSwing = 6
    }

    public enum ShotFamily
    {
        Push = 0,
        Stroke = 1,
        Loft = 2
    }

    public enum Footwork
    {
        Advance = 0,
        BackFoot = 1,
        FrontFoot = 2,
        Leave = 3
    }

    public enum TimingGrade
    {
        Early = 0,
        Perfect = 1,
        Late = 2,
        Miss = 3
    }

    public enum BatsmanType
    {
        Balanced = 0,
        Brute = 1,
        Radical = 2,
        Defensive = 3
    }

    public enum Difficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
        Expert = 3,
        Hardcore = 4,
        Custom = 5
    }

    public enum ExtraKind
    {
        None = 0,
        Wide = 1,
        NoBall = 2,
        Bye = 3
    }

    public enum WicketKind
    {
        None = 0,
        Bowled = 1,
        Caught = 2,
        RunOut = 3
    }

    public enum TimeOfDay
    {
        Morning = 0,
        Afternoon = 1,
        Evening = 2,
        Dusk = 3,
        Night = 4
    }
}
