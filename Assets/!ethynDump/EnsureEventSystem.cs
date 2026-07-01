using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put this on a manager object in a MINIGAME scene that has NO EventSystem of its own
/// (delete the EventSystem object from that scene).
///
/// - Loaded additively during gameplay: the main scene's (persistent) EventSystem is already
///   present, so this finds it and does nothing - the minigame uses the main scene's UI input.
/// - Played on its own (for testing): there's no EventSystem, so this creates one so the
///   scene's UI still works.
///
/// Either way there's never more than one EventSystem, so no "two event systems" warning,
/// and you never depend on a duplicate being destroyed after the fact.
/// </summary>
public class EnsureEventSystem : MonoBehaviour
{
    private void Awake()
    {
        // One already exists (e.g. the persistent one from the main scene)? Use it, do nothing.
        if (FindObjectOfType<EventSystem>() != null) return;

        // None found - we're running this scene on its own. Make a basic one.
        GameObject go = new GameObject("EventSystem (auto)");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>(); // OLD input manager.
                                                  // New Input System only? Use InputSystemUIInputModule instead.
    }
}
