using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Left-click to "use" the currently held item.
///
/// Flow (per ItemData):
///   1. swap hand to the using sprite  (if useDuration > 0)
///   2. play the use sound
///   3. wait useDuration  (0 = instant)
///   4. spawn the modular effect prefabs
///   5. consume one unit and/or add the transform item (e.g. empty can)
///   6. swap hand back to the holding sprite
///
/// Switching hotbar slots mid-use cancels the use.
/// </summary>
public class ItemUseController : MonoBehaviour
{
    [Header("References")]
    public HandUIController handUI;
    [Tooltip("Optional: prevents using an item while a physics object is grabbed.")]
    public PlayerPickUp playerPickUp;
    [Tooltip("Optional: plays each item's use sound.")]
    public AudioSource audioSource;

    [Header("Input")]
    public bool useMouseLeft = true;
    public Key useKey = Key.None;

    bool isUsing = false;
    Coroutine useRoutine;

    void Start()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnSelectionChanged += CancelUseOnSwitch;
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnSelectionChanged -= CancelUseOnSwitch;
    }

    void Update()
    {
        if (isUsing) return;
        if (InventoryPanelUI.IsOpen) return;
        if (Time.timeScale == 0f) return; // paused
        if (playerPickUp != null && playerPickUp.IsHoldingObject()) return;

        if (!UsePressed()) return;

        var mgr = InventoryManager.Instance;
        var item = mgr.GetSelectedItem();
        if (item != null && item.isUsable)
            useRoutine = StartCoroutine(UseRoutine(item, mgr.SelectedHotbarIndex));
    }

    bool UsePressed()
    {
        bool pressed = false;
        if (useMouseLeft && Mouse.current != null)
            pressed |= Mouse.current.leftButton.wasPressedThisFrame;
        if (useKey != Key.None && Keyboard.current != null)
            pressed |= Keyboard.current[useKey].wasPressedThisFrame;
        return pressed;
    }

    IEnumerator UseRoutine(ItemData item, int slotIndex)
    {
        isUsing = true;

        if (item.useDuration > 0f)
        {
            if (handUI != null) handUI.ShowUsingSprite(item);
            if (item.useSound != null && audioSource != null) audioSource.PlayOneShot(item.useSound);
            yield return new WaitForSeconds(item.useDuration);
        }
        else if (item.useSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(item.useSound);
        }

        var mgr = InventoryManager.Instance;
        var slot = mgr.hotbar[mgr.SelectedHotbarIndex];
        bool stillValid = mgr.SelectedHotbarIndex == slotIndex && !slot.IsEmpty && slot.item == item;

        if (stillValid)
        {
            if (item.useEffectPrefabs != null)
                foreach (var prefab in item.useEffectPrefabs)
                    if (prefab != null) Instantiate(prefab);

            if (item.consumeOnUse)
                mgr.ConsumeFromSelected(1);

            if (item.transformInto != null)
                mgr.AddItem(item.transformInto, 1, avoidHotbarIndex: mgr.SelectedHotbarIndex);
        }

        if (handUI != null) handUI.ShowHoldingSprite();
        isUsing = false;
    }

    void CancelUseOnSwitch()
    {
        if (!isUsing) return;
        if (useRoutine != null) StopCoroutine(useRoutine);
        isUsing = false;
        if (handUI != null) handUI.ShowHoldingSprite();
    }
}
