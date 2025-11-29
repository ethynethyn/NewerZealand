using UnityEngine;

public class QuestActivator : MonoBehaviour
{
    public Quest quest;

    private void OnEnable()
    {
        if (quest == null) return;

        quest.IsQuestActive = true;

        // Refresh UI if the log is open
        if (QuestManager.Instance != null)
            QuestManager.Instance.GetComponentInChildren<QuestLogUI>()?.UpdateQuestList();
    }
}
