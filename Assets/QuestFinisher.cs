using UnityEngine;

public class QuestFinisher : MonoBehaviour
{
    public Quest quest;

    private void OnEnable()
    {
        if (quest == null) return;

        for (int i = 0; i < quest.completed.Length; i++)
            quest.CompleteObjective(i);

        if (QuestManager.Instance != null)
            QuestManager.Instance.GetComponentInChildren<QuestLogUI>()?.UpdateQuestList();
    }
}
