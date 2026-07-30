using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Data definition for an item type. Create assets via:
/// Assets > Create > Inventory > Item
///
/// A slot in the inventory stores a reference to one of these + a count,
/// which is what allows stacking ("10 of an item").
/// </summary>
[CreateAssetMenu(menuName = "Inventory/Item", fileName = "New Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "New Item";

    [Tooltip("Shown under the name in the hover tooltip.")]
    [TextArea(2, 5)]
    public string description = "";

    [Tooltip("Tick this ONLY on the special empty-hand item that lives in hotbar slot 0. " +
             "It is never added, removed, dragged, or used.")]
    public bool isEmptyHand = false;

    [Header("Icons")]
    [Tooltip("Sprite shown inside hotbar / backpack slots.")]
    public Sprite icon;

    [Header("Hand Visuals (shown near the player's hand)")]
    [Tooltip("Sprite shown near the hand while HOLDING this item.")]
    public Sprite handHoldingSprite;

    [Tooltip("Optional sprite shown while USING this item (e.g. can tilted to drink). " +
             "If left empty, the holding sprite is used instead.")]
    public Sprite handUsingSprite;

    [Header("Stacking")]
    [Min(1)]
    [Tooltip("Maximum number of this item that can occupy a single slot.")]
    public int maxStackSize = 10;

    [Header("Use Settings")]
    [Tooltip("Can this item be used at all? Turn OFF for non-consumables (or the empty hand).")]
    public bool isUsable = true;

    [Tooltip("How long the 'using' state lasts before the effect fires. 0 = instant.")]
    public float useDuration = 0f;

    [Tooltip("Sound played when the item is used.")]
    public AudioClip useSound;

    [Tooltip("Prefabs spawned when the use COMPLETES (after useDuration). " +
             "Each is a modular effect, e.g. a prefab that adds +1 hunger then deletes itself.")]
    public List<GameObject> useEffectPrefabs = new List<GameObject>();

    [Header("Transform On Use")]
    [Tooltip("If ON, one unit of this item is consumed each time it is used.")]
    public bool consumeOnUse = true;

    [Tooltip("After use, add this item to the inventory (e.g. full can -> empty can). " +
             "Leave empty to just consume. The new item avoids the currently held slot so " +
             "you can keep using without interruption; it stacks onto matching items if any exist.")]
    public ItemData transformInto;

    [Header("World (optional)")]
    [Tooltip("Prefab spawned in the world if you later add a 'drop to ground' feature.")]
    public GameObject worldPrefab;
}