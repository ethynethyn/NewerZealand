using UnityEngine;
using DialogueEditor;

public class ConversationInputManager : MonoBehaviour
{
    private float scrollCooldown = 0.5f;
    private float lastScrollTime = 0f;

    void Update()
    {
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            // Add cooldown to prevent fast scrolling through options
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
