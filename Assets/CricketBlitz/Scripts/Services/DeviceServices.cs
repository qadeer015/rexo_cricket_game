using System;
using System.Collections;
using UnityEngine;

namespace CricketBlitz.Services
{
    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Pulse()
        {
            if (!Enabled)
            {
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            try
            {
                Handheld.Vibrate();
            }
            catch (Exception)
            {
                Enabled = false;
            }
#endif
        }
    }

    /// <summary>
    /// Stubs for a rewarded-ad SDK and a store SDK.
    /// They never contact a network. The UI tells the player the reward is local.
    /// </summary>
    public static class MonetizationStubs
    {
        public const string Coins500 = "blitz.coins.500";
        public const string Coins2000 = "blitz.coins.2000";
        public const string Pass = "blitz.pass.monthly";

        public static IEnumerator PracticeReward(Action<bool> onDone)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            if (onDone != null)
            {
                onDone(true);
            }
        }

        public static IEnumerator PracticePurchase(string productId, Action<bool> onDone)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            bool ok = productId == Coins500 || productId == Coins2000 || productId == Pass;
            if (onDone != null)
            {
                onDone(ok);
            }
        }
    }

    public static class LocationPrompt
    {
        public static void Ask(Action<bool> onDone)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                const string permission = UnityEngine.Android.Permission.CoarseLocation;
                if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
                {
                    onDone(true);
                    return;
                }

                UnityEngine.Android.PermissionCallbacks callbacks = new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted += _ => onDone(true);
                callbacks.PermissionDenied += _ => onDone(false);
                callbacks.PermissionDeniedAndDontAskAgain += _ => onDone(false);
                UnityEngine.Android.Permission.RequestUserPermission(permission, callbacks);
            }
            catch (Exception)
            {
                onDone(false);
            }
#else
            onDone(true);
#endif
        }
    }
}
