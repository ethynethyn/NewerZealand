#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class UIDrawConsoleReview
{
    private const string PrefKey = "UIDraw_LastReviewVersion";
    private static readonly string CurrentVersion = "1.1"; // Update per release
    private const string AssetStoreURL = "https://assetstore.unity.com/packages/tools/painting/uidraw-332738";

    static UIDrawConsoleReview()
    {
        // Don't show if current version is null or empty
        if (string.IsNullOrEmpty(CurrentVersion)) return;
        
        string lastVersion = EditorPrefs.GetString(PrefKey);
        
        // Don't show if this is a fresh install (no previous version)
        if (string.IsNullOrEmpty(lastVersion))
        {
            EditorPrefs.SetString(PrefKey, CurrentVersion);
            return;
        }
        
        // Only show once per version
        if (lastVersion == CurrentVersion) return;

        EditorApplication.delayCall += () =>
        {
            // Colored console message with clickable link
            Debug.Log(
                "<color=#00FFAA>⭐ Thanks for updating UIDraw! If you enjoy using it, leave a quick review here: ⭐</color>\n" +
                "<a href=\"" + AssetStoreURL + "\">" + AssetStoreURL + "</a>"
            );

            // Mark as shown for this version
            EditorPrefs.SetString(PrefKey, CurrentVersion);
        };
    }
}
#endif