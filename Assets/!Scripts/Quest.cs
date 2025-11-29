using UnityEngine;

public class Quest : MonoBehaviour
{
    [Header("Quest Info")]
    public string questTitle;
    public string[] objectives;  // List of objectives
    public bool[] completed;     // Same size as objectives

    public bool IsQuestActive = false;

    private void Awake()
    {
        // Auto-generate completed state
        if (completed == null || completed.Length != objectives.Length)
            completed = new bool[objectives.Length];
    }

    public void CompleteObjective(int index)
    {
        if (index < 0 || index >= completed.Length) return;
        completed[index] = true;
    }

    public bool IsQuestFullyComplete()
    {
        foreach (bool b in completed)
            if (!b) return false;
        return true;
    }
}
