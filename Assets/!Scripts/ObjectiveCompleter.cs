using UnityEngine;

public class ObjectiveCompleter : MonoBehaviour
{
    public Quest quest;
    public int objectiveIndex;

    private void OnEnable()
    {
        if (quest == null) return;
        quest.CompleteObjective(objectiveIndex);

        if (QuestManager.Instance != null)
            QuestManager.Instance.GetComponentInChildren<QuestLogUI>()?.UpdateQuestList();
    }
}
