using System;
using CricketBlitz.Batting;
using CricketBlitz.Bowling;
using CricketBlitz.Fielding;
using CricketBlitz.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CricketBlitz.UI
{
    /// <summary>
    /// Touch controls for a landscape phone: joystick on the left,
    /// shot and bowling buttons on the right, field radar at the top left.
    /// Keyboard mirrors the same actions so the match can be played in the editor.
    /// </summary>
    public sealed class MatchInputUI : MonoBehaviour
    {
        public RectTransform JoystickRoot;
        public RectTransform StrokeRoot;
        public RectTransform RunRoot;
        public Action OnRun;
        public Action OnCancelRun;

        VirtualJoystick joystick;
        BattingController batting;
        BowlingController bowling;
        FielderAI fielding;
        RectTransform battingGroup;
        RectTransform bowlingGroup;
        RectTransform runningGroup;
        RectTransform radar;
        readonly RectTransform[] dots = new RectTransform[9];
        Text fieldLabel;
        Text sideLabel;
        Text spinLabel;
        Image powerFill;
        bool draggingDot;
        int dragIndex = -1;
        int controlScheme;
        RectTransform swipePad;

        public void Build(Transform canvas)
        {
            joystick = VirtualJoystick.Create(canvas, "Joystick", new Vector2(0.1f, 0.34f), new Vector2(190f, 190f));
            JoystickRoot = joystick.GetComponent<RectTransform>();

            battingGroup = UiKit.Box(canvas, "BattingControls", new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(battingGroup, new Vector2(0.58f, 0.2f), new Vector2(0.99f, 0.72f));
            MakeShot(battingGroup, "Push", new Vector2(0f, 0.52f), new Vector2(0.32f, 0.98f), ShotFamily.Push);
            StrokeRoot = MakeShot(battingGroup, "Stroke", new Vector2(0.34f, 0.52f), new Vector2(0.66f, 0.98f), ShotFamily.Stroke).GetComponent<RectTransform>();
            MakeShot(battingGroup, "Loft", new Vector2(0.68f, 0.52f), new Vector2(1f, 0.98f), ShotFamily.Loft);
            MakeFoot(battingGroup, "Advance", new Vector2(0f, 0f), new Vector2(0.24f, 0.46f), Footwork.Advance);
            MakeFoot(battingGroup, "Back", new Vector2(0.25f, 0f), new Vector2(0.49f, 0.46f), Footwork.BackFoot);
            MakeFoot(battingGroup, "Front", new Vector2(0.5f, 0f), new Vector2(0.74f, 0.46f), Footwork.FrontFoot);
            MakeFoot(battingGroup, "Leave", new Vector2(0.75f, 0f), new Vector2(1f, 0.46f), Footwork.Leave);

            RectTransform power = UiKit.Box(canvas, "Power", new Color(1f, 1f, 1f, 0.12f));
            UiKit.Place(power, new Vector2(0.54f, 0.42f), new Vector2(28f, 220f), new Vector2(0.5f, 0.5f));
            powerFill = UiKit.Box(power, "Fill", UiKit.Gold).GetComponent<Image>();
            powerFill.rectTransform.anchorMin = Vector2.zero;
            powerFill.rectTransform.anchorMax = new Vector2(1f, 0f);
            powerFill.rectTransform.offsetMin = Vector2.zero;
            powerFill.rectTransform.offsetMax = Vector2.zero;
            powerFill.type = Image.Type.Simple;

            bowlingGroup = UiKit.Box(canvas, "BowlingControls", new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(bowlingGroup, new Vector2(0.4f, 0.2f), new Vector2(0.99f, 0.72f));
            DeliveryType[] types =
            {
                DeliveryType.Straight, DeliveryType.OutSwing, DeliveryType.InSwing, DeliveryType.LegCutter,
                DeliveryType.OffCutter, DeliveryType.Slower, DeliveryType.ReverseSwing
            };
            string[] names = { "Straight", "Out", "In", "Leg", "Off", "Slow", "Rev" };
            for (int i = 0; i < types.Length; i++)
            {
                DeliveryType type = types[i];
                float x = i / 7f;
                Button button = UiKit.Button(bowlingGroup, names[i], () =>
                {
                    if (bowling != null)
                    {
                        bowling.SetType(type);
                    }
                }, 18);
                UiKit.Stretch(button.GetComponent<RectTransform>(), new Vector2(x + 0.01f, 0.72f), new Vector2(x + 0.13f, 0.98f));
            }

            Button side = UiKit.Button(bowlingGroup, "Change Side", () =>
            {
                if (bowling != null)
                {
                    bowling.ToggleSide();
                }
            }, 20);
            UiKit.Stretch(side.GetComponent<RectTransform>(), new Vector2(0f, 0.4f), new Vector2(0.28f, 0.66f));
            sideLabel = UiKit.Label(bowlingGroup, "Over the wicket", 18, TextAnchor.MiddleLeft, UiKit.Ink);
            UiKit.Stretch(sideLabel.rectTransform, new Vector2(0.3f, 0.4f), new Vector2(0.62f, 0.66f));

            RectTransform spin = UiKit.Box(bowlingGroup, "SpinPad", new Color(0.8f, 0.2f, 0.15f, 1f));
            UiKit.Stretch(spin, new Vector2(0.64f, 0.38f), new Vector2(0.78f, 0.68f));
            UiKit.Label(spin, "Ball", 18, TextAnchor.MiddleCenter, Color.white);
            DragAxis drag = spin.gameObject.AddComponent<DragAxis>();
            drag.OnDeltaX = delta =>
            {
                if (bowling != null)
                {
                    bowling.AddSpin(delta);
                }
            };
            spinLabel = UiKit.Label(bowlingGroup, "Spin 0", 18, TextAnchor.MiddleLeft, UiKit.Ink);
            UiKit.Stretch(spinLabel.rectTransform, new Vector2(0.8f, 0.4f), new Vector2(1f, 0.66f));

            Button ok = UiKit.Button(bowlingGroup, "OK Bowl", () =>
            {
                if (bowling != null)
                {
                    bowling.MarkReady();
                }
            }, 26);
            UiKit.Stretch(ok.GetComponent<RectTransform>(), new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.32f));

            runningGroup = UiKit.Box(canvas, "Running", new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(runningGroup, new Vector2(0.62f, 0.22f), new Vector2(0.98f, 0.48f));
            Button run = UiKit.Button(runningGroup, "RUN", () =>
            {
                if (OnRun != null)
                {
                    OnRun();
                }
            }, 32);
            RunRoot = run.GetComponent<RectTransform>();
            UiKit.Stretch(RunRoot, new Vector2(0f, 0f), new Vector2(0.48f, 1f));
            Button cancel = UiKit.Button(runningGroup, "RUN CANCEL", () =>
            {
                if (OnCancelRun != null)
                {
                    OnCancelRun();
                }
            }, 22);
            UiKit.Stretch(cancel.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 1f));

            radar = UiKit.Box(canvas, "Radar", new Color(0.05f, 0.12f, 0.1f, 0.8f));
            UiKit.Place(radar, new Vector2(0f, 1f), new Vector2(150f, 150f), new Vector2(0f, 1f));
            radar.anchoredPosition = new Vector2(16f, -16f);
            fieldLabel = UiKit.Label(canvas, "Balanced", 18, TextAnchor.UpperLeft, UiKit.Gold);
            UiKit.Stretch(fieldLabel.rectTransform, new Vector2(0f, 0.78f), new Vector2(0.18f, 0.84f));
            for (int i = 0; i < dots.Length; i++)
            {
                RectTransform dot = UiKit.Box(radar, "Dot" + i, Color.white);
                UiKit.Place(dot, new Vector2(0.5f, 0.5f), new Vector2(12f, 12f), new Vector2(0.5f, 0.5f));
                dots[i] = dot;
                int index = i;
                HoldButton hold = dot.gameObject.AddComponent<HoldButton>();
                hold.Down = () =>
                {
                    draggingDot = true;
                    dragIndex = index;
                };
                hold.Up = () => draggingDot = false;
            }

            Button cycle = UiKit.Button(canvas, "Field", () =>
            {
                if (bowling != null)
                {
                    bowling.CycleField(1);
                }
            }, 16);
            RectTransform cycleRect = cycle.GetComponent<RectTransform>();
            UiKit.Place(cycleRect, new Vector2(0f, 1f), new Vector2(90f, 36f), new Vector2(0f, 1f));
            cycleRect.anchoredPosition = new Vector2(180f, -24f);

            swipePad = UiKit.Box(canvas, "SwipePad", new Color(1f, 1f, 1f, 0.04f));
            UiKit.Stretch(swipePad, new Vector2(0.18f, 0.2f), new Vector2(0.55f, 0.75f));
            SwipeShot swipe = swipePad.gameObject.AddComponent<SwipeShot>();
            swipe.OnSwipe = OnSwipe;
            UiKit.Label(swipePad, "Swipe shot", 20, TextAnchor.MiddleCenter, UiKit.Muted);
            swipePad.gameObject.SetActive(false);

            ShowBatting(false);
            ShowBowling(false);
            ShowRunning(false);
        }

        void OnSwipe(Vector2 aim, float power, ShotFamily family)
        {
            if (batting == null || !battingGroup.gameObject.activeSelf)
            {
                return;
            }

            batting.SetAim(Vector2.ClampMagnitude(aim, 1f));
            batting.Power01 = power;
            batting.Hold(family, true);
            batting.Hold(family, false);
        }

        public void Bind(BattingController batter, BowlingController bowler, FielderAI field, int scheme)
        {
            batting = batter;
            bowling = bowler;
            fielding = field;
            controlScheme = scheme;
            RefreshStick();
        }

        public void ShowBatting(bool visible)
        {
            battingGroup.gameObject.SetActive(visible);
            powerFill.transform.parent.gameObject.SetActive(visible);
            RefreshStick();
        }

        public void ShowBowling(bool visible)
        {
            bowlingGroup.gameObject.SetActive(visible);
            RefreshStick();
        }

        void RefreshStick()
        {
            bool battingOn = battingGroup != null && battingGroup.gameObject.activeSelf;
            bool bowlingOn = bowlingGroup != null && bowlingGroup.gameObject.activeSelf;
            bool swipe = controlScheme == 1 && battingOn && !bowlingOn;
            if (JoystickRoot != null)
            {
                JoystickRoot.gameObject.SetActive((battingOn || bowlingOn) && !swipe);
            }

            if (swipePad != null)
            {
                swipePad.gameObject.SetActive(swipe);
            }
        }

        public void ShowRunning(bool visible)
        {
            runningGroup.gameObject.SetActive(visible);
        }

        void Update()
        {
            if (batting != null && battingGroup.gameObject.activeSelf)
            {
                Vector2 aim = joystick.Value;
                aim.x += Axis(KeyCode.D, KeyCode.A, KeyCode.RightArrow, KeyCode.LeftArrow);
                aim.y += Axis(KeyCode.W, KeyCode.S, KeyCode.UpArrow, KeyCode.DownArrow);
                batting.SetAim(Vector2.ClampMagnitude(aim, 1f));
                batting.Tick(Time.deltaTime);
                KeyShot(KeyCode.J, ShotFamily.Push);
                KeyShot(KeyCode.K, ShotFamily.Stroke);
                KeyShot(KeyCode.L, ShotFamily.Loft);
                if (Input.GetKeyDown(KeyCode.F))
                {
                    batting.CycleFoot();
                }

                if (Input.GetKeyDown(KeyCode.X))
                {
                    batting.PlayLeave();
                }

                float power = batting.Power01;
                powerFill.rectTransform.anchorMax = new Vector2(1f, Mathf.Clamp01(power));
            }

            if (bowling != null && bowlingGroup.gameObject.activeSelf)
            {
                Vector2 nudge = joystick.Value;
                if (nudge.sqrMagnitude < 0.01f)
                {
                    nudge.x = Axis(KeyCode.D, KeyCode.A, KeyCode.RightArrow, KeyCode.LeftArrow);
                    nudge.y = Axis(KeyCode.W, KeyCode.S, KeyCode.UpArrow, KeyCode.DownArrow);
                }

                if (nudge.sqrMagnitude > 0.01f)
                {
                    bowling.Nudge(nudge);
                }

                KeyDelivery(KeyCode.Alpha1, DeliveryType.Straight);
                KeyDelivery(KeyCode.Alpha2, DeliveryType.OutSwing);
                KeyDelivery(KeyCode.Alpha3, DeliveryType.InSwing);
                KeyDelivery(KeyCode.Alpha4, DeliveryType.LegCutter);
                KeyDelivery(KeyCode.Alpha5, DeliveryType.OffCutter);
                KeyDelivery(KeyCode.Alpha6, DeliveryType.Slower);
                KeyDelivery(KeyCode.Alpha7, DeliveryType.ReverseSwing);
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    bowling.ToggleSide();
                }

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    bowling.MarkReady();
                }

                sideLabel.text = bowling.RoundTheWicket ? "Round the wicket" : "Over the wicket";
                spinLabel.text = "Spin " + bowling.Spin.ToString("0.0");
                fieldLabel.text = BowlingController.FieldLabel(bowling.Field);
            }
            else if (bowling != null)
            {
                fieldLabel.text = BowlingController.FieldLabel(bowling.Field);
            }

            if (runningGroup.gameObject.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.R) && OnRun != null)
                {
                    OnRun();
                }

                if (Input.GetKeyDown(KeyCode.C) && OnCancelRun != null)
                {
                    OnCancelRun();
                }
            }

            UpdateRadar();
        }

        void UpdateRadar()
        {
            if (fielding == null || fielding.Fielders == null)
            {
                return;
            }

            for (int i = 0; i < dots.Length && i < fielding.Spots.Length; i++)
            {
                Vector3 spot = fielding.Spots[i];
                dots[i].anchoredPosition = new Vector2(spot.x / 56f * 60f, spot.z / 56f * 60f);
            }

            if (draggingDot && dragIndex >= 0 && bowlingGroup.gameObject.activeSelf && Input.GetMouseButton(0))
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(radar, Input.mousePosition, null, out local);
                Vector3 world = new Vector3(local.x / 60f * 56f, 0f, local.y / 60f * 56f);
                fielding.MoveFielder(dragIndex, world);
            }
        }

        HoldButton MakeShot(Transform parent, string label, Vector2 min, Vector2 max, ShotFamily family)
        {
            Button button = UiKit.Button(parent, label, null, 22);
            RectTransform rect = button.GetComponent<RectTransform>();
            UiKit.Stretch(rect, min, max);
            HoldButton hold = button.gameObject.AddComponent<HoldButton>();
            hold.Down = () =>
            {
                if (batting != null)
                {
                    batting.Hold(family, true);
                }
            };
            hold.Up = () =>
            {
                if (batting != null)
                {
                    batting.Hold(family, false);
                }
            };
            return hold;
        }

        void MakeFoot(Transform parent, string label, Vector2 min, Vector2 max, Footwork foot)
        {
            Button button = UiKit.Button(parent, label, () =>
            {
                if (batting == null)
                {
                    return;
                }

                if (foot == Footwork.Leave)
                {
                    batting.PlayLeave();
                }
                else
                {
                    batting.SetFoot(foot);
                }
            }, 18);
            UiKit.Stretch(button.GetComponent<RectTransform>(), min, max);
        }

        void KeyShot(KeyCode key, ShotFamily family)
        {
            if (batting == null)
            {
                return;
            }

            if (Input.GetKeyDown(key))
            {
                batting.Hold(family, true);
            }

            if (Input.GetKeyUp(key))
            {
                batting.Hold(family, false);
            }
        }

        void KeyDelivery(KeyCode key, DeliveryType type)
        {
            if (bowling != null && Input.GetKeyDown(key))
            {
                bowling.SetType(type);
            }
        }

        static float Axis(KeyCode positive, KeyCode negative, KeyCode altPositive, KeyCode altNegative)
        {
            float value = 0f;
            if (Input.GetKey(positive) || Input.GetKey(altPositive))
            {
                value += 1f;
            }

            if (Input.GetKey(negative) || Input.GetKey(altNegative))
            {
                value -= 1f;
            }

            return value;
        }
    }

    /// <summary>
    /// Drag direction becomes the shot. A long upward drag is a loft.
    /// </summary>
    public sealed class SwipeShot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action<Vector2, float, ShotFamily> OnSwipe;
        Vector2 origin;

        public void OnPointerDown(PointerEventData eventData)
        {
            origin = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - origin;
            if (delta.magnitude < 28f || OnSwipe == null)
            {
                return;
            }

            float power = Mathf.Clamp01(delta.magnitude / 240f);
            ShotFamily family = delta.y > 90f ? ShotFamily.Loft : (delta.magnitude < 90f ? ShotFamily.Push : ShotFamily.Stroke);
            Vector2 aim = new Vector2(Mathf.Clamp(delta.x / 140f, -1f, 1f), Mathf.Clamp(delta.y / 140f, -1f, 1f));
            OnSwipe(aim, power, family);
        }
    }
}
