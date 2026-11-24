using System.Collections.Generic;
using UnityEngine;

namespace CricketBlitz.World
{
    public static class GameMaterials
    {
        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();
        static Shader shader;
        static bool shaderMissing;

        public static Material Get(Color color)
        {
            int key = ColorKey(color);
            Material material;
            if (cache.TryGetValue(key, out material))
            {
                return material;
            }

            Shader found = FindShader();
            material = found != null ? new Material(found) : new Material(Shader.Find("Sprites/Default"));
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            cache[key] = material;
            return material;
        }

        public static Font BuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }

        static Shader FindShader()
        {
            if (shader != null || shaderMissing)
            {
                return shader;
            }

            string[] names =
            {
                "Unlit/Color",
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Lit",
                "Standard",
                "Sprites/Default"
            };

            for (int i = 0; i < names.Length; i++)
            {
                shader = Shader.Find(names[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            shaderMissing = true;
            return null;
        }

        static int ColorKey(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255f);
            int g = Mathf.RoundToInt(color.g * 255f);
            int b = Mathf.RoundToInt(color.b * 255f);
            int a = Mathf.RoundToInt(color.a * 255f);
            return (r << 24) | (g << 16) | (b << 8) | a;
        }
    }
}
