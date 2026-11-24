using UnityEditor;
using UnityEngine;

namespace CricketBlitz.EditorTools
{
    /// <summary>
    /// Applies the Android landscape player settings the first time the project is opened.
    /// Run again from the menu if you want to restore them.
    /// </summary>
    [InitializeOnLoad]
    public static class CricketBlitzProjectSetup
    {
        const string AppliedKey = "CricketBlitz.MobileSettingsApplied";

        static CricketBlitzProjectSetup()
        {
            EditorApplication.delayCall += ApplyOnce;
        }

        static void ApplyOnce()
        {
            if (EditorPrefs.GetBool(AppliedKey, false))
            {
                return;
            }

            Apply();
            EditorPrefs.SetBool(AppliedKey, true);
        }

        [MenuItem("Cricket Blitz/Apply Mobile Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "Harborline Games";
            PlayerSettings.productName = "Cricket Blitz";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = false;
            PlayerSettings.stripEngineCode = true;
            try
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.harborline.cricketblitz");
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Android settings were skipped until the Android module is installed. " + exception.Message);
            }

            Debug.Log("Cricket Blitz mobile settings applied. Press Play in any scene to start.");
        }
    }
}
