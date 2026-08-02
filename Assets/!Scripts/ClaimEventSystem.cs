using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put this on the EventSystem object IN THE DRAWING (minigame) SCENE.
///
/// When that scene is loaded additively on top of your main scene, there are two
/// EventSystems and the main scene's stays the "current" one - so the drawing canvas
/// never receives pointer input. This claims EventSystem.current for THIS one as soon
/// as the scene loads, so input goes to the minigame. When the scene unloads, this
/// EventSystem is destroyed and the main scene's takes over again automatically.
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class ClaimEventSystem : MonoBehaviour
{
    // Start runs after every EventSystem in the scene has registered itself, so the
    // assignment is guaranteed to take. The scene loads fresh each round, so this runs
    // each time it's opened.
    private void Start()
    {
        EventSystem.current = GetComponent<EventSystem>();
    }
}
