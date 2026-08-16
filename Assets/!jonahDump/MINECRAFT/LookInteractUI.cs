using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the object you want to look at.
/// Look at it + press Mouse0 -> opens a UI panel.
/// Click the assigned button in that panel -> activates a target GameObject.
///
/// Requirements:
///  - This GameObject needs a Collider.
///  - Your scene needs an EventSystem (GameObject > UI > Event System) for button clicks.
///  - Camera must be tagged MainCamera.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LookInteractUI : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("How far the camera can be and still interact.")]
    [SerializeField] private float interactRange = 3f;

    [Tooltip("Which layers the ray can hit. Leave as Everything unless you need to filter.")]
    [SerializeField] private LayerMask interactMask = ~0;

    [Tooltip("OFF = ray fires from the centre of the screen (first person / crosshair). " +
             "ON = ray fires from the mouse cursor position.")]
    [SerializeField] private bool aimWithMouseCursor = false;

    [Header("UI")]
    [Tooltip("The panel GameObject to show. Usually a child of your Canvas.")]
    [SerializeField] private GameObject uiPanel;

    [Tooltip("The button inside the panel that triggers the activation.")]
    [SerializeField] private Button triggerButton;

    [Tooltip("Optional close/back button.")]
    [SerializeField] private Button closeButton;

    [Header("Result")]
    [Tooltip("The GameObject that gets switched on when the button is clicked.")]
    [SerializeField] private GameObject objectToActivate;

    [SerializeField] private bool hideTargetOnStart = true;
    [SerializeField] private bool closeUIAfterActivate = true;
    [SerializeField] private bool onlyWorksOnce = false;

    [Header("Cursor")]
    [Tooltip("Unlock and show the cursor while the panel is open. Turn off if you handle this elsewhere.")]
    [SerializeField] private bool manageCursor = true;

    [Tooltip("Close the panel with Escape.")]
    [SerializeField] private bool closeWithEscape = true;

    // Stops two panels opening at once, and lets your player-look script check
    // LookInteractUI.AnyUIOpen to freeze movement while a panel is up.
    public static bool AnyUIOpen { get; private set; }

    private Camera cam;
    private bool isOpen;
    private bool alreadyUsed;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    private void Awake()
    {
        cam = Camera.main;

        if (uiPanel != null)
            uiPanel.SetActive(false);

        if (hideTargetOnStart && objectToActivate != null)
            objectToActivate.SetActive(false);

        if (triggerButton != null)
            triggerButton.onClick.AddListener(OnTriggerButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
    }

    private void OnDestroy()
    {
        if (triggerButton != null)
            triggerButton.onClick.RemoveListener(OnTriggerButtonClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseUI);

        // Safety: don't leave the static flag stuck on if this object is destroyed while open.
        if (isOpen)
            AnyUIOpen = false;
    }

    private void Update()
    {
        // While our own panel is open, ignore world clicks entirely.
        if (isOpen)
        {
            if (closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
                CloseUI();
            return;
        }

        if (AnyUIOpen) return;                       // some other panel is up
        if (onlyWorksOnce && alreadyUsed) return;
        if (!Input.GetMouseButtonDown(0)) return;

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        Ray ray = aimWithMouseCursor
            ? cam.ScreenPointToRay(Input.mousePosition)
            : cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask, QueryTriggerInteraction.Ignore))
        {
            // Accept hits on this object or any of its children.
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                OpenUI();
        }
    }

    private void OpenUI()
    {
        if (uiPanel == null)
        {
            Debug.LogWarning($"{name}: No UI Panel assigned on LookInteractUI.", this);
            return;
        }

        uiPanel.SetActive(true);
        isOpen = true;
        AnyUIOpen = true;

        if (manageCursor)
        {
            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void CloseUI()
    {
        if (!isOpen) return;

        if (uiPanel != null)
            uiPanel.SetActive(false);

        isOpen = false;
        AnyUIOpen = false;

        if (manageCursor)
        {
            Cursor.lockState = previousLockState;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void OnTriggerButtonClicked()
    {
        if (objectToActivate != null)
            objectToActivate.SetActive(true);

        alreadyUsed = true;

        if (closeUIAfterActivate)
            CloseUI();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}