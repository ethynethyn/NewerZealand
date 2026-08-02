using UnityEngine;
using System.Collections.Generic;

// Put this on a parent object in a Lunch/Recess scene that represents a side-quest
// character. It reads the quest's global state from the SideQuestManager and turns
// on the matching variant object — each variant being a different version of the
// character with its own dialogue. States you don't list simply show nothing.
//
// "Gone for good" in a later scene: either don't place this character in that scene
// at all, or tick 'hidden' below.
public class QuestGiverVariants : MonoBehaviour
{
    [System.Serializable]
    public class Variant
    {
        [Tooltip("Show this object when the quest is in this state.")]
        public SideQuestState state;
        [Tooltip("The variant GameObject (its own model + dialogue).")]
        public GameObject root;
    }

    [Tooltip("Must match the quest ID used by your dialogue / SideQuestManager calls.")]
    public string questId;

    [Tooltip("One entry per state you want to represent. Omit a state to show nothing for it.")]
    public List<Variant> variants = new List<Variant>();

    [Tooltip("If true, this character is 'gone' — no variant is shown, whatever the state.")]
    public bool hidden = false;

    void OnEnable()
    {
        Apply();
        if (SideQuestManager.Instance != null)
            SideQuestManager.Instance.OnQuestStateChanged += HandleChanged;
    }

    void OnDisable()
    {
        if (SideQuestManager.Instance != null)
            SideQuestManager.Instance.OnQuestStateChanged -= HandleChanged;
    }

    void HandleChanged(string id, SideQuestState state)
    {
        if (id == questId) Apply();
    }

    // Turns on the variant matching the current quest state; turns the rest off.
    public void Apply()
    {
        SideQuestState current = SideQuestManager.Instance != null
            ? SideQuestManager.Instance.GetState(questId)
            : SideQuestState.NotStarted;

        foreach (var v in variants)
        {
            if (v == null || v.root == null) continue;
            v.root.SetActive(!hidden && v.state == current);
        }
    }
}
