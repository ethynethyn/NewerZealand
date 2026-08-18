using UnityEngine;

/// <summary>
/// Testing hotkeys. Stick this on New_InventoryCanvas so it follows you
/// between scenes. Delete it (or leave editorOnly ticked) before shipping.
/// </summary>
public class New_DebugKeys : MonoBehaviour
{
    [Tooltip("Keys do nothing in a build unless you untick this.")]
    public bool editorOnly = true;

    [Header("Stars")]
    public KeyCode giveStarsKey = KeyCode.F1;
    public int starsToGive = 1000;

    [Header("Items")]
    public KeyCode giveAllItemsKey = KeyCode.F2;

    [Header("Wipe")]
    public KeyCode resetEverythingKey = KeyCode.F3;

    void Update()
    {
        if (editorOnly && !Application.isEditor) return;

        if (Input.GetKeyDown(giveStarsKey))
        {
            New_StarFlags.Add(starsToGive);
            Debug.Log("DEBUG  +" + starsToGive + " stars, now " + New_StarFlags.Count);
        }

        if (Input.GetKeyDown(giveAllItemsKey))
        {
            foreach (New_ItemID id in System.Enum.GetValues(typeof(New_ItemID)))
            {
                New_InventoryUI.Give(id);
            }
            Debug.Log("DEBUG  gave every item");
        }

        if (Input.GetKeyDown(resetEverythingKey))
        {
            New_ItemFlags.ResetAll();
            New_StarFlags.ResetAll();

            if (New_InventoryUI.Instance != null) New_InventoryUI.Instance.RebuildAll();

            Debug.Log("DEBUG  wiped items and stars");
        }
    }
}