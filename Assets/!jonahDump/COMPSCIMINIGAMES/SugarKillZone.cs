using UnityEngine;

// A trigger (place it across the bottom, below the cups) that removes sugar
// which missed every cup. Destroying missed grains is what lets the level
// resolve quickly instead of waiting on stuff that's already off-screen.
// Keep it from overlapping the cup interiors.
[RequireComponent(typeof(Collider2D))]
public class SugarKillZone : MonoBehaviour
{
    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var grain = other.GetComponent<SugarGrain>();
        if (grain == null) grain = other.GetComponentInParent<SugarGrain>();
        if (grain == null) return;

        if (SugarGameManager.Instance != null)
            SugarGameManager.Instance.KillGrain(grain);
        else
            Destroy(grain.gameObject);
    }
}
