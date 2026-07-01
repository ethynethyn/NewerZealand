using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Guarantees there is exactly ONE EventSystem across all your scenes.
///
/// Put this on the EventSystem object in EVERY scene that has one - your main scene
/// AND each minigame scene. The first one to load survives and is marked
/// DontDestroyOnLoad; any other EventSystem that loads afterwards (e.g. when a minigame
/// scene is loaded additively on top) destroys itself in Awake.
///
/// Result: no "There can be only one active Event System" warning, and the surviving
/// EventSystem is persistent - so it is never switched off by the trigger's PauseScene
/// and keeps driving whatever UI is on screen (the minigame canvas during a round, your
/// main UI the rest of the time).
///
/// Each minigame scene still keeps its own EventSystem, so it also works when you play
/// that scene on its own - the duplicate is only removed when another one already exists.
///
/// (This replaces ClaimEventSystem - you can delete that script.)
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class PersistentEventSystem : MonoBehaviour
{
    private static PersistentEventSystem instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // Another EventSystem already owns the game - this one is a duplicate, remove it.
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
