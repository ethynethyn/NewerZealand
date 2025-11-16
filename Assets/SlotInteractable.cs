using UnityEngine;

public class SlotInteractable : MonoBehaviour
{
    public enum InteractableType { SpinHandle, BetButton }
    public InteractableType type = InteractableType.SpinHandle;

    public float betValue = 5f; // Only used for BetButton

    [Header("Slot Machine Reference")]
    public SlotMachine slotMachine; // Assign the machine this lever/button belongs to
}
