using System;
using CricketBlitz.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace CricketBlitz.UI
{
    /// <summary>
    /// Bottom scoreboard, timing needle, commentary and the modal panels
    /// used during a match (toss, batter list, reward, pause).
    /// </summary>
    public sealed class ScoreboardUI : MonoBehaviour
    {
        public Canvas Canvas;
        public Action PauseClicked;
        public Action CameraClicked;

        Text teamText;
        Image teamSwatch;
        Text strikerText;
        Text nonText;
        Text scoreText;
        Text oversText;
        Text inningsText;
        Text bowlerText;
        Text speedText;
        Text chaseText;
        Text commentary;
        Text timingText;
        Text shotText;
        Text badgeText;
        RectTransform needle;
        RectTransform banner;
        Text bannerTitle;
        Text bannerBody;
        RectTransform choices;
        Text choiceTitle;
        Text choiceBody;
        RectTransform batterPanel;
        RectTransform batterList;
        Text pauseTitle;
        RectTransform pausePanel;
        RectTransform rewardPanel;
        Text rewardText;
        RectTransform pointer;
        Text pointerText;
        Button skipButton;
        readonly Text[] balls = new Text[6];
        Action choiceA;
        Action choiceB;
        Action rewardAction;
        Action<int, int> batterConfirm;
        bool singlePick;
        int pickStriker = -1;
        int pickNon = -1;
        int pinnedBatter = -1;
        BatterScore[] pickSource;

        public void Build()
        {
            Canvas = UiKit.CreateCanvas("MatchCanvas", 30);
            Canvas.transform.SetParent(transform, false);

            RectTransform bar = UiKit.Box(Canvas.transform, "Scorebar", UiKit.Navy);
            UiKit.Stretch(bar, new Vector2(0f, 0f), new Vector2(1f, 0f));
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 168f);

            teamSwatch = UiKit.Box(bar, "Swatch", UiKit.Gold).GetComponent<Image>();
            UiKit.Place(teamSwatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 148f), new Vector2(0f, 0.5f));
            teamSwatch.rectTransform.anchoredPosition = new Vector2(16f, 0f);

            teamText = Make(bar, new Vector2(0.06f, 0.15f), new Vector2(0.16f, 0.85f), 32, TextAnchor.MiddleLeft);
            strikerText = Make(bar, new Vector2(0.16f, 0.5f), new Vector2(0.36f, 0.92f), 24, TextAnchor.MiddleLeft);
            nonText = Make(bar, new Vector2(0.16f, 0.08f), new Vector2(0.36f, 0.48f), 24, TextAnchor.MiddleLeft);
            scoreText = Make(bar, new Vector2(0.36f, 0.42f), new Vector2(0.52f, 0.92f), 34, TextAnchor.MiddleCenter);
            oversText = Make(bar, new Vector2(0.36f, 0.08f), new Vector2(0.46f, 0.42f), 24, TextAnchor.MiddleCenter);
            inningsText = Make(bar, new Vector2(0.46f, 0.08f), new Vector2(0.52f, 0.42f), 22, TextAnchor.MiddleCenter);
            bowlerText = Make(bar, new Vector2(0.52f, 0.5f), new Vector2(0.74f, 0.92f), 22, TextAnchor.MiddleLeft);
            chaseText = Make(bar, new Vector2(0.52f, 0.08f), new Vector2(0.74f, 0.48f), 20, TextAnchor.MiddleLeft);
            speedText = Make(bar, new Vector2(0.86f, 0.15f), new Vector2(0.99f, 0.85f), 28, TextAnchor.MiddleRight);

            for (int i = 0; i < 6; i++)
            {
                RectTransform pip = UiKit.Box(bar, "Ball" + i, new Color(1f, 1f, 1f, 0.08f));
                float x = 0.745f + i * 0.02f;
                UiKit.Stretch(pip, new Vector2(x, 0.28f), new Vector2(x + 0.018f, 0.72f));
                balls[i] = UiKit.Label(pip, string.Empty, 18, TextAnchor.MiddleCenter, UiKit.Ink);
            }

            RectTransform timing = UiKit.Box(Canvas.transform, "Timing", new Color(0f, 0f, 0f, 0.35f));
            UiKit.Stretch(timing, new Vector2(0.3f, 0.9f), new Vector2(0.7f, 0.98f));
            needle = UiKit.Box(timing, "Needle", UiKit.Gold);
            UiKit.Place(needle, new Vector2(0.5f, 0.5f), new Vector2(8f, 70f), new Vector2(0.5f, 0.5f));
            timingText = UiKit.Label(Canvas.transform, "WAIT", 32, TextAnchor.MiddleCenter, UiKit.Gold);
            RectTransform timingLabel = timingText.rectTransform;
            UiKit.Stretch(timingLabel, new Vector2(0.3f, 0.82f), new Vector2(0.7f, 0.9f));
            shotText = UiKit.Label(Canvas.transform, string.Empty, 26, TextAnchor.MiddleCenter, UiKit.Ink);
            UiKit.Stretch(shotText.rectTransform, new Vector2(0.3f, 0.76f), new Vector2(0.7f, 0.83f));

            commentary = UiKit.Label(Canvas.transform, string.Empty, 24, TextAnchor.MiddleCenter, UiKit.Ink);
            UiKit.Stretch(commentary.rectTransform, new Vector2(0.18f, 0.168f), new Vector2(0.82f, 0.23f));
            badgeText = UiKit.Label(Canvas.transform, string.Empty, 22, TextAnchor.MiddleLeft, UiKit.Gold);
            UiKit.Stretch(badgeText.rectTransform, new Vector2(0.01f, 0.9f), new Vector2(0.28f, 0.98f));

            banner = UiKit.Box(Canvas.transform, "Banner", UiKit.Panel);
            UiKit.Stretch(banner, new Vector2(0.22f, 0.38f), new Vector2(0.78f, 0.72f));
            bannerTitle = UiKit.Label(banner, string.Empty, 40, TextAnchor.UpperCenter, UiKit.Gold);
            bannerBody = UiKit.Label(banner, string.Empty, 26, TextAnchor.MiddleCenter, UiKit.Ink);
            UiKit.Stretch(bannerTitle.rectTransform, new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.92f));
            UiKit.Stretch(bannerBody.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.62f));
            banner.gameObject.SetActive(false);

            choices = UiKit.Box(Canvas.transform, "Choices", UiKit.Panel);
            UiKit.Stretch(choices, new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.74f));
            choiceTitle = UiKit.Label(choices, string.Empty, 36, TextAnchor.UpperCenter, UiKit.Gold);
            choiceBody = UiKit.Label(choices, string.Empty, 24, TextAnchor.MiddleCenter, UiKit.Ink);
            UiKit.Stretch(choiceTitle.rectTransform, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.94f));
            UiKit.Stretch(choiceBody.rectTransform, new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.68f));
            Button left = UiKit.Button(choices, "Yes", () => Fire(true), 28);
            Button right = UiKit.Button(choices, "No", () => Fire(false), 28);
            UiKit.Stretch(left.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.46f, 0.32f));
            UiKit.Stretch(right.GetComponent<RectTransform>(), new Vector2(0.54f, 0.08f), new Vector2(0.92f, 0.32f));
            choices.gameObject.SetActive(false);

            BuildBatterPanel();
            BuildPause();
            BuildReward();

            pointer = UiKit.Box(Canvas.transform, "Pointer", new Color(0f, 0f, 0f, 0f));
            UiKit.Place(pointer, new Vector2(0.5f, 0.5f), new Vector2(280f, 80f), new Vector2(0.5f, 1f));
            pointerText = UiKit.Label(pointer, "▼", 28, TextAnchor.MiddleCenter, UiKit.Gold);
            pointer.gameObject.SetActive(false);

            Button pause = UiKit.Button(Canvas.transform, "Pause", () =>
            {
                if (PauseClicked != null)
                {
                    PauseClicked();
                }
            }, 22);
            UiKit.Stretch(pause.GetComponent<RectTransform>(), new Vector2(0.9f, 0.9f), new Vector2(0.99f, 0.98f));
            Button cam = UiKit.Button(Canvas.transform, "Cam", () =>
            {
                if (CameraClicked != null)
                {
                    CameraClicked();
                }
            }, 22);
            UiKit.Stretch(cam.GetComponent<RectTransform>(), new Vector2(0.8f, 0.9f), new Vector2(0.89f, 0.98f));

            skipButton = UiKit.Button(Canvas.transform, "Skip Tutorial", null, 22);
            UiKit.Stretch(skipButton.GetComponent<RectTransform>(), new Vector2(0.64f, 0.9f), new Vector2(0.79f, 0.98f));
            skipButton.gameObject.SetActive(false);
        }

        public void Refresh(InningsModel innings, int number, int target, Color color, string shortName)
        {
            if (innings == null || innings.Batters.Length == 0)
            {
                return;
            }

            teamSwatch.color = color;
            teamText.text = shortName;
            teamText.color = color;
            BatterScore striker = innings.Batters[innings.Striker];
            BatterScore other = innings.Batters[innings.NonStriker];
            strikerText.text = Short(striker.Name) + "*   " + striker.Runs + "(" + striker.Balls + ")";
            nonText.text = Short(other.Name) + "   " + other.Runs + "(" + other.Balls + ")";
            scoreText.text = shortName + "  " + innings.Runs + "-" + innings.Wickets;
            oversText.text = innings.OversText;
            inningsText.text = "Inn " + number;
            bowlerText.text = innings.BowlerFigures();
            for (int i = 0; i < 6; i++)
            {
                balls[i].text = i < innings.LastSixCount ? innings.LastSix[i] : string.Empty;
                bool wicket = balls[i].text == "W";
                balls[i].color = wicket ? new Color(1f, 0.45f, 0.4f) : UiKit.Ink;
            }

            if (target > 0)
            {
                int need = target - innings.Runs;
                int left = innings.TotalOvers * 6 - innings.LegalBalls;
                chaseText.text = need <= 0 ? "Target reached" : "Need " + need + " from " + Mathf.Max(0, left);
            }
            else
            {
                chaseText.text = innings.IsPowerplay ? "Powerplay" : string.Empty;
            }
        }

        public void SetSpeed(float kmh)
        {
            speedText.text = Mathf.RoundToInt(kmh) + " km/h";
        }

        public void SetTimingNeedle(float timeToContact)
        {
            float t = Mathf.InverseLerp(0.5f, -0.18f, timeToContact);
            needle.anchorMin = new Vector2(t, 0.15f);
            needle.anchorMax = new Vector2(t, 0.85f);
            needle.anchoredPosition = Vector2.zero;
            if (timeToContact < 0.08f && timeToContact > -0.02f)
            {
                timingText.text = "NOW";
                timingText.color = UiKit.Good;
            }
            else if (string.IsNullOrEmpty(shotText.text))
            {
                timingText.text = "TIME THE SHOT";
                timingText.color = UiKit.Gold;
            }
        }

        public void SetTimingResult(string label)
        {
            timingText.text = label;
            timingText.color = label == "PERFECT" ? UiKit.Good : (label == "MISS" ? UiKit.Danger : UiKit.Gold);
        }

        public void SetShotName(string shot)
        {
            shotText.text = shot;
        }

        public void SetCommentary(string line)
        {
            commentary.text = line;
        }

        public void SetBadges(bool powerplay, bool freeHit, bool rain)
        {
            string text = string.Empty;
            if (powerplay)
            {
                text += "POWERPLAY  ";
            }

            if (freeHit)
            {
                text += "FREE HIT  ";
            }

            if (rain)
            {
                text += "RAIN";
            }

            badgeText.text = text;
        }

        public void ShowBanner(string title, string body)
        {
            banner.gameObject.SetActive(true);
            bannerTitle.text = title;
            bannerBody.text = body;
        }

        public void HideBanner()
        {
            if (banner != null)
            {
                banner.gameObject.SetActive(false);
            }
        }

        public void ShowChoices(string title, string body, string yes, string no, Action onYes, Action onNo)
        {
            choices.gameObject.SetActive(true);
            choiceTitle.text = title;
            choiceBody.text = body;
            choiceA = onYes;
            choiceB = onNo;
            Text[] labels = choices.GetComponentsInChildren<Text>();
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == choiceTitle || labels[i] == choiceBody)
                {
                    continue;
                }

                if (labels[i].transform.parent.name.Contains("Yes") || labels[i].text == "Yes" || labels[i].transform.parent.GetSiblingIndex() == 2)
                {
                    labels[i].text = yes;
                }
            }

            Button[] buttons = choices.GetComponentsInChildren<Button>();
            if (buttons.Length >= 2)
            {
                buttons[0].GetComponentInChildren<Text>().text = yes;
                buttons[1].GetComponentInChildren<Text>().text = no;
            }
        }

        public void HideChoices()
        {
            if (choices != null)
            {
                choices.gameObject.SetActive(false);
            }
        }

        public void ShowBatterSelect(BatterScore[] players, bool single, int preset, Action<int, int> onConfirm)
        {
            pickSource = players;
            singlePick = single;
            batterConfirm = onConfirm;
            pickStriker = preset;
            pinnedBatter = single ? preset : -1;
            pickNon = single ? -1 : (preset == 0 ? 1 : 0);
            batterPanel.gameObject.SetActive(true);
            RebuildBatterRows();
        }

        public void HideBatterSelect()
        {
            if (batterPanel != null)
            {
                batterPanel.gameObject.SetActive(false);
            }
        }

        public void ShowReward(string title, int coins, Action onCollect)
        {
            rewardPanel.gameObject.SetActive(true);
            rewardText.text = title + "\n+" + coins + " coins";
            rewardAction = onCollect;
        }

        public void HideReward()
        {
            if (rewardPanel != null)
            {
                rewardPanel.gameObject.SetActive(false);
            }
        }

        public void ShowPause(string body, Action resume, Action restart, Action quit)
        {
            pausePanel.gameObject.SetActive(true);
            pauseTitle.text = "Paused\n" + body;
            Button[] buttons = pausePanel.GetComponentsInChildren<Button>();
            if (buttons.Length >= 3)
            {
                Rewire(buttons[0], resume);
                Rewire(buttons[1], restart);
                Rewire(buttons[2], quit);
            }
        }

        public void HidePause()
        {
            if (pausePanel != null)
            {
                pausePanel.gameObject.SetActive(false);
            }
        }

        public void SetSkip(bool visible, Action onSkip)
        {
            skipButton.gameObject.SetActive(visible);
            skipButton.onClick.RemoveAllListeners();
            if (onSkip != null)
            {
                skipButton.onClick.AddListener(() => onSkip());
            }
        }

        public void PointAt(RectTransform target, string hint)
        {
            pointer.gameObject.SetActive(target != null);
            if (target == null)
            {
                return;
            }

            pointer.position = target.position + new Vector3(0f, -70f, 0f);
            pointerText.text = "▼  " + hint;
        }

        public void HidePointer()
        {
            pointer.gameObject.SetActive(false);
        }

        public void SetBigScreen(TextMesh screen, string text)
        {
            if (screen != null)
            {
                screen.text = text;
            }
        }

        void Fire(bool yes)
        {
            Action action = yes ? choiceA : choiceB;
            HideChoices();
            if (action != null)
            {
                action();
            }
        }

        void BuildBatterPanel()
        {
            batterPanel = UiKit.Box(Canvas.transform, "Batters", UiKit.Panel);
            UiKit.Stretch(batterPanel, new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.9f));
            Text header = UiKit.Label(batterPanel, "Select batter    Name    Hand    Technique    Timing    Type", 22, TextAnchor.MiddleLeft, UiKit.Gold);
            UiKit.Stretch(header.rectTransform, new Vector2(0.02f, 0.9f), new Vector2(0.98f, 0.99f));
            RectTransform scroll = UiKit.Box(batterPanel, "Scroll", new Color(0f, 0f, 0f, 0.2f));
            UiKit.Stretch(scroll, new Vector2(0.02f, 0.16f), new Vector2(0.98f, 0.88f));
            scroll.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            ScrollRect scroller = scroll.gameObject.AddComponent<ScrollRect>();
            scroller.horizontal = false;
            scroller.viewport = scroll;
            batterList = UiKit.Box(scroll, "List", new Color(0f, 0f, 0f, 0f));
            batterList.anchorMin = new Vector2(0f, 1f);
            batterList.anchorMax = new Vector2(1f, 1f);
            batterList.pivot = new Vector2(0.5f, 1f);
            batterList.sizeDelta = new Vector2(0f, 800f);
            VerticalLayoutGroup layout = batterList.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;
            ContentSizeFitter fitter = batterList.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroller.content = batterList;
            Button confirm = UiKit.Button(batterPanel, "Confirm", ConfirmBatters, 26);
            UiKit.Stretch(confirm.GetComponent<RectTransform>(), new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.13f));
            batterPanel.gameObject.SetActive(false);
        }

        void RebuildBatterRows()
        {
            for (int i = batterList.childCount - 1; i >= 0; i--)
            {
                Destroy(batterList.GetChild(i).gameObject);
            }

            if (pickSource == null)
            {
                return;
            }

            for (int i = 0; i < pickSource.Length; i++)
            {
                BatterScore batter = pickSource[i];
                if (batter.Out)
                {
                    continue;
                }

                if (singlePick && batter.HasBatted && i != pickStriker && i != pinnedBatter)
                {
                    continue;
                }

                int index = i;
                string hand = batter.RightHanded ? "R" : "L";
                string row = batter.Name + "    " + hand + "    " + batter.Technique + "    " + batter.Timing + "    " + batter.Type;
                bool chosen = index == pickStriker || index == pickNon;
                Button button = UiKit.Button(batterList, chosen ? "> " + row : row, () => PickBatter(index), 22);
                LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
                element.minHeight = 58f;
                button.GetComponent<Image>().color = chosen ? new Color(0.2f, 0.4f, 0.38f, 1f) : new Color(0.14f, 0.2f, 0.28f, 1f);
            }
        }

        void PickBatter(int index)
        {
            if (singlePick)
            {
                pickStriker = index;
                pickNon = -1;
            }
            else if (pickStriker < 0 || (pickNon >= 0 && pickStriker >= 0))
            {
                pickStriker = index;
                pickNon = -1;
            }
            else if (index != pickStriker)
            {
                pickNon = index;
            }

            RebuildBatterRows();
        }

        void ConfirmBatters()
        {
            if (pickStriker < 0)
            {
                return;
            }

            if (!singlePick && pickNon < 0)
            {
                return;
            }

            Action<int, int> action = batterConfirm;
            HideBatterSelect();
            if (action != null)
            {
                action(pickStriker, pickNon);
            }
        }

        void BuildPause()
        {
            pausePanel = UiKit.Box(Canvas.transform, "Pause", UiKit.Panel);
            UiKit.Stretch(pausePanel, new Vector2(0.3f, 0.22f), new Vector2(0.7f, 0.78f));
            pauseTitle = UiKit.Label(pausePanel, "Paused", 28, TextAnchor.UpperCenter, UiKit.Gold);
            UiKit.Stretch(pauseTitle.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.96f));
            Button resume = UiKit.Button(pausePanel, "Resume", null, 26);
            Button restart = UiKit.Button(pausePanel, "Restart", null, 26);
            Button quit = UiKit.Button(pausePanel, "Quit to menu", null, 26);
            UiKit.Stretch(resume.GetComponent<RectTransform>(), new Vector2(0.15f, 0.52f), new Vector2(0.85f, 0.68f));
            UiKit.Stretch(restart.GetComponent<RectTransform>(), new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.48f));
            UiKit.Stretch(quit.GetComponent<RectTransform>(), new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.28f));
            pausePanel.gameObject.SetActive(false);
        }

        void BuildReward()
        {
            rewardPanel = UiKit.Box(Canvas.transform, "Reward", UiKit.Panel);
            UiKit.Stretch(rewardPanel, new Vector2(0.32f, 0.34f), new Vector2(0.68f, 0.7f));
            rewardText = UiKit.Label(rewardPanel, string.Empty, 30, TextAnchor.MiddleCenter, UiKit.Gold);
            UiKit.Stretch(rewardText.rectTransform, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.92f));
            Button collect = UiKit.Button(rewardPanel, "Collect Reward", () =>
            {
                Action action = rewardAction;
                HideReward();
                if (action != null)
                {
                    action();
                }
            }, 26);
            UiKit.Stretch(collect.GetComponent<RectTransform>(), new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.3f));
            rewardPanel.gameObject.SetActive(false);
        }

        static void Rewire(Button button, Action action)
        {
            button.onClick.RemoveAllListeners();
            if (action != null)
            {
                button.onClick.AddListener(() => action());
            }
        }

        static Text Make(Transform parent, Vector2 min, Vector2 max, int size, TextAnchor anchor)
        {
            Text text = UiKit.Label(parent, string.Empty, size, anchor, UiKit.Ink);
            UiKit.Stretch(text.rectTransform, min, max);
            return text;
        }

        static string Short(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            int space = name.IndexOf(' ');
            if (space <= 0)
            {
                return name;
            }

            return name.Substring(0, 1) + ". " + name.Substring(space + 1);
        }
    }
}
