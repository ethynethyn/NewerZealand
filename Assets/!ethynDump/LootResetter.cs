using UnityEngine;

/// <summary>
/// Re-rolls loot in every LootContainer except those marked "Don't Refresh Loot".
///
/// Two ways to trigger it:
///   - Enable this GameObject (it resets in OnEnable, then disables itself so you
///     can enable it again on the next school bell).
///   - Or call ResetAllLoot() directly from a UnityEvent (e.g. the bell's event).
/// </summary>
public class LootResetter : MonoBehaviour
{
    [Tooltip("Reset all refreshable loot the moment this object is enabled.")]
    public bool resetOnEnable = true;

    [Tooltip("Disable this object right after resetting, so re-enabling it later " +
             "(e.g. each bell) triggers another reset.")]
    public bool disableSelfAfterReset = true;

    void OnEnable()
    {
        if (!resetOnEnable) return;
        ResetAllLoot();
        if (disableSelfAfterReset) gameObject.SetActive(false);
    }

    /// <summary>Refresh loot in every container that isn't marked Don't Refresh Loot.</summary>
    public void ResetAllLoot()
    {
        var list = LootContainer.All;
        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            if (c != null && !c.dontRefreshLoot) c.RefreshLoot();
        }
    }
}
