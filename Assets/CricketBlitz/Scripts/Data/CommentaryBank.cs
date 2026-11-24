namespace CricketBlitz.Data
{
    public static class CommentaryBank
    {
        public static string Pick(string key, string subject)
        {
            switch (key)
            {
                case "intro":
                    return "Live from " + subject + ". Welcome to Cricket Blitz.";
                case "toss_bat":
                    return subject + " win the toss and choose to bat.";
                case "toss_bowl":
                    return subject + " win the toss and choose to bowl.";
                case "four":
                    return "That's four. " + subject + " finds the rope.";
                case "six":
                    return "That's into the stands. Six from " + subject + ".";
                case "bowled":
                    return "The stumps light up. " + subject + " is bowled.";
                case "caught":
                    return "In the air and taken. " + subject + " has to go.";
                case "runout":
                    return "Direct hit. " + subject + " is short of the crease.";
                case "wide":
                    return "Wide. The umpire stretches the arms.";
                case "noball":
                    return "No-ball. A free hit is coming.";
                case "freehit":
                    return "Free hit. They can swing without fear of the stumps.";
                case "dot":
                    return "No run. Well bowled to " + subject + ".";
                case "single":
                    return "They take the single and keep the strike moving.";
                case "two":
                    return "They come back for two. Good running.";
                case "three":
                    return "Three. The field was slow to the rope.";
                case "leave":
                    return subject + " leaves it alone.";
                case "rain":
                    return "Rain on the covers. The overs are cut and the target is revised.";
                case "powerplay":
                    return "Powerplay. Only two fielders are allowed out on the rope.";
                case "target":
                    return subject + " need to chase that down.";
                case "chase_done":
                    return "The chase is done. " + subject + " get home.";
                case "defend":
                    return subject + " defend the total.";
                default:
                    return subject;
            }
        }
    }
}
