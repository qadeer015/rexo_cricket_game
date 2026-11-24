using System;
using CricketBlitz.Core;
using CricketBlitz.Sim;
using CricketBlitz.UI;
using UnityEngine;

namespace CricketBlitz.Tutorial
{
    /// <summary>
    /// Four numbered tasks over a slow net ball: aim, choose Stroke,
    /// time the shot, then run. Each task pays 60-90 coins.
    /// </summary>
    public sealed class TutorialManager : MonoBehaviour
    {
        public bool Active;
        public bool WaitingForReward;
        public bool Finished;
        public int Step;
        public Action OnYes;
        public Action OnNo;

        ScoreboardUI ui;
        MatchInputUI input;
        static readonly int[] Rewards = { 60, 70, 80, 90 };
        static readonly string[] Titles =
        {
            "Task 1 complete",
            "Task 2 complete",
            "Task 3 complete",
            "Task 4 complete"
        };

        public void Begin(ScoreboardUI scoreboard, MatchInputUI controls)
        {
            ui = scoreboard;
            input = controls;
            Active = true;
            Step = 0;
            ui.SetCommentary("Coach Jalen Crowe: drag the stick to pick a shot direction.");
            ui.SetSkip(true, Skip);
        }

        public bool BlockBowling()
        {
            return Active && Step < 2;
        }

        public void Point()
        {
            if (!Active || ui == null || WaitingForReward || Finished)
            {
                return;
            }

            if (Step == 0 && input.JoystickRoot != null)
            {
                ui.PointAt(input.JoystickRoot, "1  Drag to aim");
            }
            else if (Step == 1 && input.StrokeRoot != null)
            {
                ui.PointAt(input.StrokeRoot, "2  Tap Stroke");
            }
            else if (Step == 2)
            {
                ui.PointAt(input.StrokeRoot, "3  Release on NOW");
            }
            else if (Step == 3 && input.RunRoot != null)
            {
                ui.PointAt(input.RunRoot, "4  Tap RUN");
            }
        }

        public void NotifyAim(Vector2 aim)
        {
            if (Active && Step == 0 && aim.magnitude > 0.65f)
            {
                Advance("You found a gap.");
            }
        }

        public void NotifyFamily(ShotFamily family)
        {
            if (Active && Step == 1 && family == ShotFamily.Stroke)
            {
                Advance("Stroke is your driving shot.");
            }
        }

        public void NotifyShot(bool contact)
        {
            if (Active && Step == 2 && contact)
            {
                Advance("Timing plus technique is the whole shot.");
            }
        }

        public void NotifyRun()
        {
            if (Active && Step == 3)
            {
                Advance("Called the run. Watch the throw.");
            }
        }

        public void Skip()
        {
            if (!Active)
            {
                return;
            }

            Active = false;
            Finished = true;
            WaitingForReward = false;
            ui.HidePointer();
            ui.HideReward();
            ui.SetSkip(false, null);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(50);
                GameManager.Instance.Profile.tutorialComplete = true;
                GameManager.Instance.Save();
            }

            ui.ShowChoices(
                "Tutorial skipped",
                "Ready for challenge mode?",
                "Yes",
                "No",
                OnYes,
                OnNo);
        }

        void Advance(string reason)
        {
            int coins = Rewards[Mathf.Clamp(Step, 0, Rewards.Length - 1)];
            string title = Titles[Mathf.Clamp(Step, 0, Titles.Length - 1)] + "\n" + reason;
            Step += 1;
            WaitingForReward = true;
            ui.HidePointer();
            ui.ShowReward(title, coins, () => Collect(coins));
        }

        void Collect(int coins)
        {
            WaitingForReward = false;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(coins);
                GameManager.Instance.Profile.tutorialCoins += coins;
            }

            if (Step >= 4)
            {
                Active = false;
                Finished = true;
                ui.SetSkip(false, null);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.Profile.tutorialComplete = true;
                    GameManager.Instance.Save();
                }

                ui.ShowChoices(
                    "Ready for challenge mode?",
                    "A 5-over match. You can bat and bowl.",
                    "Yes",
                    "No",
                    OnYes,
                    OnNo);
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.Save();
            }
        }
    }
}
