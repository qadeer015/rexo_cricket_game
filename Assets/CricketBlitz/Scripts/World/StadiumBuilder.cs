using CricketBlitz.Data;
using CricketBlitz.Sim;
using UnityEngine;

namespace CricketBlitz.World
{
    public sealed class StumpSet
    {
        public Transform Root;
        public Transform[] Stumps;
        bool broken;

        public void Break()
        {
            if (broken || Stumps == null)
            {
                return;
            }

            broken = true;
            for (int i = 0; i < Stumps.Length; i++)
            {
                if (Stumps[i] == null)
                {
                    continue;
                }

                float side = i == 0 ? -18f : (i == 2 ? 18f : 8f);
                Stumps[i].localRotation = Quaternion.Euler(78f, side, side * 0.4f);
                Stumps[i].localPosition += new Vector3(0f, 0.05f, 0.2f);
            }
        }

        public void Reset()
        {
            broken = false;
            if (Stumps == null)
            {
                return;
            }

            float[] x = { -0.11f, 0f, 0.11f };
            for (int i = 0; i < Stumps.Length; i++)
            {
                Stumps[i].localRotation = Quaternion.identity;
                Stumps[i].localPosition = new Vector3(x[i], 0.36f, 0f);
            }
        }
    }

    public sealed class StadiumContext
    {
        public GameObject Root;
        public Transform PitchMarker;
        public Transform ArrivalMarker;
        public Transform Ball;
        public Light Sun;
        public Light[] Floods;
        public PlayerAvatar Striker;
        public PlayerAvatar NonStriker;
        public PlayerAvatar Bowler;
        public PlayerAvatar Keeper;
        public PlayerAvatar Umpire;
        public PlayerAvatar[] Fielders;
        public StumpSet StrikerStumps;
        public StumpSet BowlerStumps;
        public TextMesh BigScreen;
        public ParticleSystem Rain;
        public float Boundary = BallSim.BoundaryRadius;
    }

    /// <summary>
    /// Builds one stadium from primitives. Every ground in the database reuses this
    /// layout and changes colours, name and sponsor boards.
    /// </summary>
    public static class StadiumBuilder
    {
        public static StadiumContext Build(Transform parent, StadiumInfo info, PitchType pitch)
        {
            StadiumContext context = new StadiumContext();
            GameObject root = new GameObject("Stadium");
            root.transform.SetParent(parent, false);
            context.Root = root;

            Part(root.transform, PrimitiveType.Cylinder, "Outfield", Vector3.zero, new Vector3(118f, 0.04f, 118f), info.Outfield);
            Color pitchColor = pitch == PitchType.Green
                ? new Color(0.45f, 0.52f, 0.28f)
                : (pitch == PitchType.Dry ? new Color(0.78f, 0.68f, 0.46f) : new Color(0.72f, 0.58f, 0.38f));
            Part(root.transform, PrimitiveType.Cube, "Pitch", new Vector3(0f, 0.05f, 0f), new Vector3(3.05f, 0.04f, 20.12f), pitchColor);
            Part(root.transform, PrimitiveType.Cube, "CreaseNear", new Vector3(0f, 0.08f, 8.84f), new Vector3(2.64f, 0.02f, 0.08f), Color.white);
            Part(root.transform, PrimitiveType.Cube, "CreaseFar", new Vector3(0f, 0.08f, -8.84f), new Vector3(2.64f, 0.02f, 0.08f), Color.white);
            Part(root.transform, PrimitiveType.Cube, "PoppingNear", new Vector3(0f, 0.08f, 7.62f), new Vector3(1.83f, 0.02f, 0.06f), Color.white);
            Part(root.transform, PrimitiveType.Cube, "PoppingFar", new Vector3(0f, 0.08f, -7.62f), new Vector3(1.83f, 0.02f, 0.06f), Color.white);

            context.StrikerStumps = MakeStumps(root.transform, "StrikerStumps", new Vector3(0f, 0f, BallSim.StumpZ));
            context.BowlerStumps = MakeStumps(root.transform, "BowlerStumps", new Vector3(0f, 0f, -BallSim.StumpZ));

            context.PitchMarker = Part(root.transform, PrimitiveType.Cylinder, "PitchMarker", new Vector3(0f, 0.1f, 4.2f), new Vector3(0.28f, 0.02f, 0.28f), new Color(0.85f, 0.15f, 0.12f)).transform;
            context.ArrivalMarker = Part(root.transform, PrimitiveType.Sphere, "ArrivalMarker", new Vector3(0f, 0.15f, 9f), new Vector3(0.18f, 0.18f, 0.18f), new Color(0.2f, 0.45f, 0.95f)).transform;

            BuildRope(root.transform);
            BuildStands(root.transform, info);
            BuildBoards(root.transform, info);
            BuildFloods(root.transform, context);
            BuildBigScreen(root.transform, info, context);
            context.Rain = BuildRain(root.transform);

            context.Sun = LightFinder.FindAnyLight();
            return context;
        }

        public static void SpawnPlayers(StadiumContext context, Transform parent, Color batColor, Color batTrim, Color bowlColor, Color bowlTrim)
        {
            context.Striker = PlayerAvatar.Create(parent, "Striker", batColor, batTrim);
            context.NonStriker = PlayerAvatar.Create(parent, "NonStriker", batColor, batTrim);
            context.Bowler = PlayerAvatar.Create(parent, "Bowler", bowlColor, bowlTrim);
            context.Keeper = PlayerAvatar.Create(parent, "Keeper", bowlColor, bowlTrim);
            context.Umpire = PlayerAvatar.Create(parent, "Umpire", new Color(0.15f, 0.15f, 0.16f), Color.white);
            context.Umpire.SetIdentity("Umpire", 0, true);
            context.Fielders = new PlayerAvatar[9];
            for (int i = 0; i < context.Fielders.Length; i++)
            {
                context.Fielders[i] = PlayerAvatar.Create(parent, "Fielder" + (i + 1), bowlColor, bowlTrim);
            }

            PlaceIdle(context);
        }

        public static void PlaceIdle(StadiumContext context)
        {
            SetXZ(context.Striker, 0f, 8.7f);
            SetXZ(context.NonStriker, 0f, -8.7f);
            SetXZ(context.Bowler, 0.4f, -14.5f);
            SetXZ(context.Keeper, 0f, 11.5f);
            SetXZ(context.Umpire, -1.6f, 12.4f);
            Face(context.Striker, new Vector3(0f, 0f, -1f));
            Face(context.NonStriker, new Vector3(0f, 0f, 1f));
            Face(context.Bowler, new Vector3(0f, 0f, 1f));
        }

        public static void SetXZ(PlayerAvatar avatar, float x, float z)
        {
            if (avatar == null)
            {
                return;
            }

            Vector3 position = avatar.transform.localPosition;
            position.x = x;
            position.z = z;
            avatar.transform.localPosition = position;
        }

        public static void Face(PlayerAvatar avatar, Vector3 forward)
        {
            if (avatar == null || forward.sqrMagnitude < 0.001f)
            {
                return;
            }

            avatar.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        static void BuildRope(Transform parent)
        {
            LineRenderer line = new GameObject("Boundary").AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.positionCount = 48;
            line.loop = true;
            line.widthMultiplier = 0.12f;
            line.material = GameMaterials.Get(Color.white);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float radius = BallSim.BoundaryRadius;
            for (int i = 0; i < 48; i++)
            {
                float angle = i / 48f * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.25f, Mathf.Sin(angle) * radius));
            }
        }

        static void BuildStands(Transform parent, StadiumInfo info)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 68f, 2.2f, Mathf.Sin(angle) * 68f);
                GameObject stand = Part(parent, PrimitiveType.Cube, "Stand" + i, position, new Vector3(18f, 4.4f, 8f), info.Stands);
                stand.transform.rotation = Quaternion.LookRotation(new Vector3(-position.x, 0f, -position.z), Vector3.up);
                int crowd = QualitySettings.GetQualityLevel() <= 0 ? 4 : 8;
                for (int c = 0; c < crowd; c++)
                {
                    float x = -6f + c * 1.7f;
                    Color shirt = Color.Lerp(info.Accent, Color.white, (c % 3) / 3f);
                    Part(stand.transform, PrimitiveType.Capsule, "Fan" + c, new Vector3(x, 0.7f, 3.2f), new Vector3(0.35f, 0.28f, 0.35f), shirt);
                }
            }
        }

        static void BuildBoards(Transform parent, StadiumInfo info)
        {
            string[] sponsors = info.Sponsors;
            for (int i = 0; i < 8; i++)
            {
                float angle = (i + 0.5f) / 8f * Mathf.PI * 2f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 58.5f, 0.7f, Mathf.Sin(angle) * 58.5f);
                GameObject board = Part(parent, PrimitiveType.Cube, "Board" + i, position, new Vector3(7.5f, 1.1f, 0.15f), info.Accent);
                board.transform.rotation = Quaternion.LookRotation(new Vector3(-position.x, 0f, -position.z), Vector3.up);
                GameObject textObject = new GameObject("Sponsor");
                textObject.transform.SetParent(board.transform, false);
                textObject.transform.localPosition = new Vector3(0f, 0f, -0.6f);
                textObject.transform.localRotation = Quaternion.identity;
                TextMesh text = textObject.AddComponent<TextMesh>();
                text.text = sponsors[i % sponsors.Length];
                text.font = GameMaterials.BuiltinFont();
                text.fontSize = 42;
                text.characterSize = 0.12f;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.color = new Color(0.08f, 0.08f, 0.1f);
                MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
                if (renderer != null && text.font != null)
                {
                    renderer.sharedMaterial = text.font.material;
                }
            }
        }

        static void BuildFloods(Transform parent, StadiumContext context)
        {
            context.Floods = new Light[4];
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * Mathf.PI * 2f + 0.4f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 62f, 0f, Mathf.Sin(angle) * 62f);
                Part(parent, PrimitiveType.Cylinder, "LightPole" + i, position + new Vector3(0f, 8f, 0f), new Vector3(0.35f, 8f, 0.35f), new Color(0.7f, 0.72f, 0.75f));
                GameObject lamp = Part(parent, PrimitiveType.Cube, "Lamp" + i, position + new Vector3(0f, 16.2f, 0f), new Vector3(1.4f, 0.35f, 1.4f), new Color(1f, 0.95f, 0.75f));
                Light light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 80f;
                light.intensity = 1.4f;
                light.color = new Color(1f, 0.96f, 0.88f);
                light.enabled = false;
                context.Floods[i] = light;
            }
        }

        static void BuildBigScreen(Transform parent, StadiumInfo info, StadiumContext context)
        {
            GameObject frame = Part(parent, PrimitiveType.Cube, "BigScreen", new Vector3(0f, 8.5f, 72f), new Vector3(14f, 6f, 0.4f), new Color(0.05f, 0.06f, 0.08f));
            GameObject textObject = new GameObject("ScreenText");
            textObject.transform.SetParent(frame.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.7f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = info.Name;
            text.font = GameMaterials.BuiltinFont();
            text.fontSize = 64;
            text.characterSize = 0.09f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = info.Accent;
            MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
            if (renderer != null && text.font != null)
            {
                renderer.sharedMaterial = text.font.material;
            }

            context.BigScreen = text;
        }

        static ParticleSystem BuildRain(Transform parent)
        {
            GameObject go = new GameObject("Rain");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 18f, 0f);
            ParticleSystem rain = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = rain.main;
            main.loop = true;
            main.startLifetime = 1.4f;
            main.startSpeed = 18f;
            main.startSize = 0.06f;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = rain.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 1f, 40f);
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GameMaterials.Get(new Color(0.75f, 0.82f, 0.9f, 0.7f));
            rain.Stop();
            return rain;
        }

        static StumpSet MakeStumps(Transform parent, string objectName, Vector3 position)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            StumpSet set = new StumpSet();
            set.Root = root.transform;
            set.Stumps = new Transform[3];
            float[] x = { -0.11f, 0f, 0.11f };
            Color wood = new Color(0.93f, 0.86f, 0.62f);
            for (int i = 0; i < 3; i++)
            {
                set.Stumps[i] = Part(root.transform, PrimitiveType.Cylinder, "Stump" + i, new Vector3(x[i], 0.36f, 0f), new Vector3(0.045f, 0.36f, 0.045f), wood).transform;
            }

            Part(root.transform, PrimitiveType.Cube, "Bails", new Vector3(0f, 0.74f, 0f), new Vector3(0.28f, 0.025f, 0.04f), wood);
            return set;
        }

        static GameObject Part(Transform parent, PrimitiveType type, string objectName, Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameMaterials.Get(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }
    }

    public static class LightFinder
    {
        public static Light FindAnyLight()
        {
            Light light = Object.FindObjectOfType<Light>();
            if (light == null)
            {
                GameObject go = new GameObject("Sun");
                light = go.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.shadows = LightShadows.None;
            return light;
        }
    }
}
