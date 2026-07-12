using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// REPLACES your existing HandUIController.
/// - Animates the base hand (idle / sprint / holding / npc / punch) as before.
/// - Adds an item overlay Image that shows the currently selected item's sprite
///   (holding sprite normally, using sprite while ItemUseController is using it).
/// - Punch only fires when the hand is bare (no held item, no physics grab).
///
/// Keep your existing HandAnimationSet.cs — it is unchanged.
/// </summary>
public class HandUIController : MonoBehaviour
{
    [Header("Base Hand")]
    public Image handImage;                 // the animated hand
    public PlayerPickUp playerPickUp;       // optional (your physics grab system)

    [Header("Held Item Overlay")]
    [Tooltip("Image positioned near the hand that shows the current item's sprite. " +
             "Leave empty if you don't want an item overlay.")]
    public Image heldItemImage;

    [Header("Animation Sets")]
    public HandAnimationSet idle;
    public HandAnimationSet sprint;
    public HandAnimationSet holding;
    public HandAnimationSet npc;
    public HandAnimationSet punch;

    [Header("Input")]
    public Key sprintKey = Key.LeftShift;

    [HideInInspector] public bool npcNearby = false;
    [HideInInspector] public bool punchEnabled = true;

    HandState currentState;
    Coroutine animationRoutine;
    bool isPunching = false;
    bool lockState = false;

    void Start()
    {
        if (handImage != null) handImage.gameObject.SetActive(true);
        currentState = HandState.Idle;
        StartAnimation(idle);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnSelectionChanged += RefreshHeldItem;
            InventoryManager.Instance.OnChanged += RefreshHeldItem;
        }
        RefreshHeldItem();
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance == null) return;
        InventoryManager.Instance.OnSelectionChanged -= RefreshHeldItem;
        InventoryManager.Instance.OnChanged -= RefreshHeldItem;
    }

    void Update()
    {
        // Punch only with a bare hand, while unfrozen, and not while the inventory is open.
        if (IsBareHand() && !isPunching && punchEnabled && !InventoryPanelUI.IsOpen &&
            Time.timeScale > 0f &&
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(PlayPunchOnce());
        }

        if (!isPunching)
            DetermineState();
    }

    bool IsBareHand()
    {
        bool physicsHolding = playerPickUp != null && playerPickUp.IsHoldingObject();
        bool itemSelected = InventoryManager.Instance != null &&
                            !InventoryManager.Instance.IsSelectedEmptyHand();
        return !physicsHolding && !itemSelected;
    }

    public void SetNPCNearby(bool value) => npcNearby = value;

    void DetermineState()
    {
        if (lockState) return;

        bool physicsHolding = playerPickUp != null && playerPickUp.IsHoldingObject();
        bool itemSelected = InventoryManager.Instance != null &&
                            !InventoryManager.Instance.IsSelectedEmptyHand();
        bool holdingAnything = physicsHolding || itemSelected;

        if (npcNearby) SetState(HandState.NPCNearby);
        else if (holdingAnything) SetState(HandState.Holding);
        else if (Keyboard.current != null && Keyboard.current[sprintKey].isPressed) SetState(HandState.Sprinting);
        else SetState(HandState.Idle);
    }

    // ---- Item overlay ----------------------------------------------------

    /// <summary>Show the selected item's HOLDING sprite (or hide if bare hand).</summary>
    public void RefreshHeldItem()
    {
        if (heldItemImage == null) return;

        var item = InventoryManager.Instance != null ? InventoryManager.Instance.GetSelectedItem() : null;
        if (item != null && item.handHoldingSprite != null)
        {
            heldItemImage.sprite = item.handHoldingSprite;
            heldItemImage.enabled = true;
        }
        else
        {
            heldItemImage.sprite = null;
            heldItemImage.enabled = false;
        }
    }

    /// <summary>Called by ItemUseController: swap to the USING sprite (falls back to holding).</summary>
    public void ShowUsingSprite(ItemData item)
    {
        if (heldItemImage == null || item == null) return;
        Sprite s = item.handUsingSprite != null ? item.handUsingSprite : item.handHoldingSprite;
        if (s != null)
        {
            heldItemImage.sprite = s;
            heldItemImage.enabled = true;
        }
    }

    /// <summary>Called by ItemUseController when a use ends: back to the holding sprite.</summary>
    public void ShowHoldingSprite() => RefreshHeldItem();

    // ---- State machine (unchanged behaviour) -----------------------------

    public void ForceIdle()
    {
        isPunching = false;
        currentState = HandState.Idle;
        StartAnimation(idle);
        lockState = true;
        StartCoroutine(UnlockStateNextFrame());
    }

    IEnumerator UnlockStateNextFrame() { yield return null; lockState = false; }

    void SetState(HandState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        StartAnimation(GetSet(newState));
    }

    void StartAnimation(HandAnimationSet set)
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        if (set != null && set.frames.Length > 0)
            animationRoutine = StartCoroutine(PlayAnimation(set));
    }

    HandAnimationSet GetSet(HandState state)
    {
        switch (state)
        {
            case HandState.Punching: return punch;
            case HandState.NPCNearby: return npc;
            case HandState.Holding: return holding;
            case HandState.Sprinting: return sprint;
            default: return idle;
        }
    }

    IEnumerator PlayPunchOnce()
    {
        isPunching = true;
        if (animationRoutine != null) StopCoroutine(animationRoutine);

        if (punch != null && punch.frames.Length > 0)
        {
            for (int i = 0; i < punch.frames.Length; i++)
            {
                handImage.sprite = punch.frames[i];
                yield return new WaitForSeconds(punch.frameRate);
            }
        }

        isPunching = false;
        currentState = HandState.Idle;
        StartAnimation(idle);
    }

    IEnumerator PlayAnimation(HandAnimationSet set)
    {
        int index = 0;
        while (true)
        {
            if (set.frames.Length == 0) yield break;
            handImage.sprite = set.frames[index];
            index = (index + 1) % set.frames.Length;
            yield return new WaitForSeconds(set.frameRate);
        }
    }
}

public enum HandState
{
    Idle,
    Sprinting,
    Holding,
    NPCNearby,
    Punching
}