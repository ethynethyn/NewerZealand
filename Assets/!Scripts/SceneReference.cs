using UnityEngine;

// A serializable reference to a Scene. In the Editor you drag a Scene asset into
// the field; at runtime the scene NAME is used to load it.
//
// IMPORTANT: every scene you reference must also be added to
// File ▸ Build Settings ▸ Scenes In Build, or it won't load.
[System.Serializable]
public class SceneReference : ISerializationCallbackReceiver
{
#if UNITY_EDITOR
    // Editor-only: the actual Scene asset you drag in. Stripped from builds.
    [SerializeField] private UnityEditor.SceneAsset sceneAsset;
#endif
    [SerializeField] private string sceneName;
    [SerializeField] private string scenePath;

    public string SceneName => sceneName;
    public string ScenePath => scenePath;
    public bool IsValid => !string.IsNullOrEmpty(sceneName);

    // Keeps the runtime strings in sync with the dragged-in asset.
    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (sceneAsset != null)
        {
            scenePath = UnityEditor.AssetDatabase.GetAssetPath(sceneAsset);
            sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }
        else
        {
            scenePath = "";
            sceneName = "";
        }
#endif
    }

    public void OnAfterDeserialize() { }
}
