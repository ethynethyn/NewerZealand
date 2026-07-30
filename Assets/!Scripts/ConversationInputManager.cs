using UnityEngine;
using DialogueEditor;

public class ConversationInputManager : MonoBehaviour
{
    [Header("Active only while a conversation is running")]
    public GameObject objectToToggle;   // your player-input freezer

    private float scrollCooldown = 0.5f;
    private float lastScrollTime = 0f;
    private bool wasActive = false;

    void Start()
    {
        if (objectToToggle != null)
            objectToToggle.SetActive(false);
    }

    void Update()
    {
        bool active = ConversationManager.Instance != null &&
                      ConversationManager.Instance.IsConversationActive;

        // Only flip the object when the state actually changes
        if (active != wasActive)
        {
            if (objectToToggle != null)
                objectToToggle.SetActive(active);
            wasActive = active;
        }

        if (active)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Time.time - lastScrollTime > scrollCooldown)
            {
                if (scroll > 0f)
                {
                    ConversationManager.Instance.SelectNextOption();
                    lastScrollTime = Time.time;
                }
                else if (scroll < 0f)
                {
                    ConversationManager.Instance.SelectPreviousOption();
                    lastScrollTime = Time.time;
                }
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                ConversationManager.Instance.PressSelectedOption();
            }
        }
    }
}