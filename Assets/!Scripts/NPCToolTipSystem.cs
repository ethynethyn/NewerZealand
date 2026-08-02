using UnityEngine;
using TMPro;
using System.Collections;
using DialogueEditor;

public class NPCTooltipScreenSpace : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public HandUIController handUI; // 👈 ADDED

    [Header("UI")]
    public TextMeshProUGUI tooltipUI;
    public RectTransform tooltipRect;

    [Header("Settings")]
    public float interactRange = 3f;
    public LayerMask npcLayer;

    [Header("Animation")]
    public float popDuration = 0.18f;
    public float popOvershoot = 1.15f;

    private NPCTooltip currentNPC;

    private Vector3 originalScale;
    private Coroutine popRoutine;

    void Start()
    {
        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);

        if (tooltipRect != null)
            originalScale = tooltipRect.localScale;
        else if (tooltipUI != null)
            originalScale = tooltipUI.rectTransform.localScale;
    }

    void Update()
    {
        HandleLookAtNPC();
        HandleHandWave(); // 👈 ADDED
    }

    void HandleLookAtNPC()
    {
        // 🔥 BLOCK ALL TOOLTIP LOGIC DURING CONVERSATION
        if (ConversationManager.Instance != null &&
            ConversationManager.Instance.IsConversationActive)
        {
            HideTooltip();
            return;
        }

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, npcLayer))
        {
            NPCTooltip npc = hit.collider.GetComponentInParent<NPCTooltip>();

            if (npc != null)
            {
                currentNPC = npc;
                ShowTooltip(npc);
                return;
            }
        }

        currentNPC = null;
        HideTooltip();
    }

    // ---------------- HAND WAVE ----------------
    void HandleHandWave()
    {
        if (handUI == null) return;

        bool canInteract =
            ConversationManager.Instance != null &&
            !ConversationManager.Instance.IsConversationActive &&
            Physics.Raycast(playerCamera.position, playerCamera.forward, interactRange, npcLayer);

        handUI.SetNPCNearby(canInteract);
    }

    // ---------------- TOOLTIP ----------------
    void ShowTooltip(NPCTooltip npc)
    {
        if (tooltipUI == null) return;

        bool wasInactive = !tooltipUI.gameObject.activeSelf;

        tooltipUI.gameObject.SetActive(true);
        tooltipUI.text = npc.GetTooltipText();

        if (wasInactive)
            PlayPopAnimation();
    }

    void HideTooltip()
    {
        currentNPC = null;

        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
            popRoutine = null;
        }

        if (tooltipRect != null)
            tooltipRect.localScale = originalScale;

        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);
    }

    // ---------------- ANIMATION ----------------
    void PlayPopAnimation()
    {
        if (tooltipRect == null && tooltipUI != null)
            tooltipRect = tooltipUI.rectTransform;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(PopIn());
    }

    IEnumerator PopIn()
    {
        float t = 0f;

        tooltipRect.localScale = Vector3.zero;

        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float norm = t / popDuration;

            float scale = Mathf.Sin(norm * Mathf.PI * 0.5f) * popOvershoot;

            tooltipRect.localScale = Vector3.LerpUnclamped(
                Vector3.zero,
                originalScale,
                scale
            );

            yield return null;
        }

        tooltipRect.localScale = originalScale;
    }
}