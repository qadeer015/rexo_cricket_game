using UnityEngine;

namespace CricketBlitz.World
{
    /// <summary>
    /// Low-poly cricketer built from primitives, with a jersey number.
    /// Shot, bowl and run poses are code-driven so the MVP needs no animation clips.
    /// </summary>
    public sealed class PlayerAvatar : MonoBehaviour
    {
        public int JerseyNumber;
        public string DisplayName = string.Empty;
        public bool IsRightHanded = true;

        Transform bat;
        Transform arm;
        TextMesh label;
        float shot;
        float bowl;
        float runPhase;
        bool running;
        bool celebrating;

        public static PlayerAvatar Create(Transform parent, string objectName, Color shirt, Color trim)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(parent, false);
            PlayerAvatar avatar = root.AddComponent<PlayerAvatar>();
            avatar.Build(shirt, trim);
            return avatar;
        }

        void Build(Color shirt, Color trim)
        {
            AddPart(PrimitiveType.Capsule, "Body", new Vector3(0f, 0.95f, 0f), new Vector3(0.42f, 0.48f, 0.42f), shirt);
            AddPart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.72f, 0f), new Vector3(0.28f, 0.28f, 0.28f), new Color(0.86f, 0.72f, 0.58f));
            AddPart(PrimitiveType.Cube, "Pads", new Vector3(0f, 0.38f, 0.06f), new Vector3(0.28f, 0.46f, 0.16f), trim);
            arm = AddPart(PrimitiveType.Cube, "Arm", new Vector3(0.28f, 1.25f, 0.05f), new Vector3(0.1f, 0.38f, 0.1f), shirt).transform;
            bat = AddPart(PrimitiveType.Cube, "Bat", new Vector3(0.42f, 0.85f, 0.18f), new Vector3(0.08f, 0.72f, 0.18f), new Color(0.93f, 0.84f, 0.62f)).transform;

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            label = textObject.AddComponent<TextMesh>();
            Font font = GameMaterials.BuiltinFont();
            label.font = font;
            label.fontSize = 48;
            label.characterSize = 0.07f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
            if (renderer != null && font != null)
            {
                renderer.sharedMaterial = font.material;
            }
        }

        public void SetIdentity(string playerName, int number, bool rightHanded)
        {
            DisplayName = playerName ?? string.Empty;
            JerseyNumber = number;
            IsRightHanded = rightHanded;
            if (label != null)
            {
                label.text = number + "  " + ShortName(DisplayName);
            }

            float side = rightHanded ? 1f : -1f;
            if (bat != null)
            {
                bat.localPosition = new Vector3(0.42f * side, 0.85f, 0.18f);
            }

            if (arm != null)
            {
                arm.localPosition = new Vector3(0.28f * side, 1.25f, 0.05f);
            }
        }

        public void SetRunning(bool value)
        {
            running = value;
        }

        public void PlayShot()
        {
            shot = 1f;
        }

        public void PlayBowl()
        {
            bowl = 1f;
        }

        public void Celebrate()
        {
            celebrating = true;
        }

        public void Tick(float dt)
        {
            if (running)
            {
                runPhase += dt * 10f;
            }

            float bob = running ? Mathf.Sin(runPhase) * 0.07f : 0f;
            if (celebrating)
            {
                bob = Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.16f;
            }

            Vector3 position = transform.localPosition;
            position.y = bob;
            transform.localPosition = position;

            if (shot > 0f && bat != null)
            {
                shot = Mathf.MoveTowards(shot, 0f, dt * 3.2f);
                bat.localRotation = Quaternion.Euler(Mathf.Sin((1f - shot) * Mathf.PI) * -75f, 0f, 0f);
            }

            if (bowl > 0f && arm != null)
            {
                bowl = Mathf.MoveTowards(bowl, 0f, dt * 1.4f);
                arm.localRotation = Quaternion.Euler(Mathf.Sin((1f - bowl) * Mathf.PI) * 120f, 0f, 0f);
            }

            if (label != null && Camera.main != null)
            {
                Vector3 toCamera = label.transform.position - Camera.main.transform.position;
                if (toCamera.sqrMagnitude > 0.01f)
                {
                    label.transform.rotation = Quaternion.LookRotation(toCamera);
                }
            }
        }

        GameObject AddPart(PrimitiveType type, string objectName, Vector3 position, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameMaterials.Get(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part;
        }

        static string ShortName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                return string.Empty;
            }

            int space = playerName.IndexOf(' ');
            if (space <= 0 || space >= playerName.Length - 1)
            {
                return playerName;
            }

            return playerName.Substring(0, 1) + ". " + playerName.Substring(space + 1);
        }
    }
}
