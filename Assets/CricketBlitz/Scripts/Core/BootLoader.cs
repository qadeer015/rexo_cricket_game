using CricketBlitz.Audio;
using CricketBlitz.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CricketBlitz.Core
{
    /// <summary>
    /// Starts Cricket Blitz from any scene, including an empty one.
    /// Open the project and press Play.
    /// </summary>
    public static class BootLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameManager.Instance != null)
            {
                return;
            }

            UiKit.EnsureEventSystem();
            GameObject root = new GameObject("CricketBlitz");
            root.AddComponent<GameManager>();
            root.AddComponent<MatchAudio>();
            root.AddComponent<MenuFlow>();
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                UiKit.EnsureEventSystem();
            }
        }
    }
}
