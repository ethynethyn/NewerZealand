using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    private List<Quest> quests = new List<Quest>();

    private void Awake()
    {
        Instance = this;
        LoadQuests();
    }

    void LoadQuests()
    {
        quests.Clear();

        foreach (Transform child in transform)
        {
            Quest q = child.GetComponent<Quest>();
            if (q != null)
                quests.Add(q);
        }
    }

    public List<Quest> GetActiveQuests()
    {
        List<Quest> active = new List<Quest>();

        foreach (Quest q in quests)
            if (q.IsQuestActive)
                active.Add(q);

        return active;
    }
}
