using System.Collections;
using UnityEngine;
using DialogueEditor;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;

    [Header("Camera")]
    [SerializeField] private GameObject cutsceneCamera;
    [SerializeField] private GameObject cameraToDisable;
    [SerializeField] private bool restoreCameraOnDialogueEnd = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Tooltip("Exact state name as it appears in the Animator window.")]
    [SerializeField] private string animationStateName = "";
    [SerializeField] private int animationLayer = 0;

    [Header("Dialogue")]
    [SerializeField] private float dialogueDelay = 2f;
    [SerializeField] private NPCConversation conversation;

    [Header("Optional")]
    [Tooltip("Player controller, HUD, etc. Switched off for the cutscene.")]
    [SerializeField] private GameObject[] disableDuringCutscene;

    private bool hasTriggered;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTrigger(collision.gameObject);
    }

    private void TryTrigger(GameObject other)
    {
        if (triggerOnce && hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        hasTriggered = true;
        StopAllCoroutines();
        StartCoroutine(CutsceneRoutine());
    }

    private IEnumerator CutsceneRoutine()
    {
        for (int i = 0; i < disableDuringCutscene.Length; i++)
        {
            if (disableDuringCutscene[i] != null)
                disableDuringCutscene[i].SetActive(false);
        }

        if (cameraToDisable != null) cameraToDisable.SetActive(false);
        if (cutsceneCamera != null) cutsceneCamera.SetActive(true);

        if (animator != null && !string.IsNullOrEmpty(animationStateName))
            animator.Play(animationStateName, animationLayer, 0f);

        yield return new WaitForSeconds(dialogueDelay);

        if (conversation == null)
        {
            Debug.LogWarning($"{name}: no NPCConversation assigned.", this);
            EndCutscene();
            yield break;
        }

        // Delete this line (and OnConversationEnded below) if your
        // DialogueEditor version doesn't expose the event.
        if (restoreCameraOnDialogueEnd)
            ConversationManager.OnConversationEnded += OnConversationEnded;

        ConversationManager.Instance.StartConversation(conversation);
    }

    private void OnConversationEnded()
    {
        ConversationManager.OnConversationEnded -= OnConversationEnded;
        EndCutscene();
    }

    private void EndCutscene()
    {
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        if (cameraToDisable != null) cameraToDisable.SetActive(true);

        for (int i = 0; i < disableDuringCutscene.Length; i++)
        {
            if (disableDuringCutscene[i] != null)
                disableDuringCutscene[i].SetActive(true);
        }
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationEnded -= OnConversationEnded;
    }
}