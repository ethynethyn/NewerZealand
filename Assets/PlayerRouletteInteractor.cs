using UnityEngine;

public class PlayerRouletteInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public float rayDistance = 3f;
    public KeyCode interactKey = KeyCode.E;

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
            TryInteract();
    }

    void TryInteract()
    {
        if (!playerCamera) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            RouletteInteractable r = hit.collider.GetComponent<RouletteInteractable>();
            if (!r)
                r = hit.collider.GetComponentInParent<RouletteInteractable>();

            if (r)
                r.Interact();
        }
    }
}
