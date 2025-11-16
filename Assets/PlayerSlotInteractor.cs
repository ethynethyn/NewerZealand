using UnityEngine;

public class PlayerSlotInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public float rayDistance = 3f;
    public KeyCode interactKey = KeyCode.E;

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
            TryInteract();
    }

    void TryInteract()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            SlotInteractable interactable = hit.collider.GetComponent<SlotInteractable>();
            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<SlotInteractable>();

            if (interactable == null || interactable.slotMachine == null) return;

            switch (interactable.type)
            {
                case SlotInteractable.InteractableType.SpinHandle:
                    interactable.slotMachine.Spin();
                    break;

                case SlotInteractable.InteractableType.BetButton:
                    interactable.slotMachine.SetBet(interactable.betValue);
                    break;
            }
        }
    }
}
