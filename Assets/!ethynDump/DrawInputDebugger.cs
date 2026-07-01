using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// TEMPORARY DIAGNOSTIC - delete it once the drawing works.
/// Put it on any object in the drawing scene. Load the minigame additively, left-click the
/// draw area, and read the Console. It tells you exactly where the pointer chain is breaking.
/// </summary>
public class DrawInputDebugger : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        EventSystem es = EventSystem.current;
        if (es == null)
        {
            Debug.Log("[DrawDebug] CASE A: no active EventSystem.current. Nothing can receive UI " +
                      "input - your EventSystem is missing or got paused during the round.");
            return;
        }

        PointerEventData data = new PointerEventData(es) { position = Input.mousePosition };
        List<RaycastResult> hits = new List<RaycastResult>();
        es.RaycastAll(data, hits);

        Debug.Log($"[DrawDebug] EventSystem = '{es.name}', mouse = {Input.mousePosition}, " +
                  $"UI elements under cursor = {hits.Count}");

        for (int i = 0; i < hits.Count; i++)
            Debug.Log($"[DrawDebug]    #{i} (topmost first): '{hits[i].gameObject.name}'  " +
                      $"via raycaster on '{hits[i].module?.gameObject.name}'");

        if (hits.Count == 0)
            Debug.Log("[DrawDebug] CASE B: nothing raycastable under the cursor. The Canvas's raycast " +
                      "is failing - almost always a Screen Space - Camera canvas pointed at the paused " +
                      "main camera. Set the draw Canvas to Screen Space - Overlay.");
        else
            Debug.Log("[DrawDebug] CASE C: read #0 above. If it's your RawImage/draw canvas, clicks ARE " +
                      "reaching it. If it's something else (an input blocker, a panel), THAT object is on " +
                      "top eating the clicks - turn off its Raycast Target or make sure it's off mid-round.");
    }
}
