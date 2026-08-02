using UnityEngine;
using System;
using System.Collections.Generic;

// The state a side quest can be in. Add more values here if you need finer control.
public enum SideQuestState { NotStarted, Active, Completed }

// Persistent, scene-INDEPENDENT store of side-quest states. Because it lives across
// scene loads, a quest started (or finished) in Lunch 1 is still remembered in
// Lunch 3, Lunch 5, etc. Quests are keyed by a string ID you choose per quest.
//
// This is completely separate from SceneProgressionManager — quest state never
// affects which scene loads; it only affects which character variant is shown.
public class SideQuestManager : MonoBehaviour
{
    public static SideQuestManager Instance { get; private set; }

    // Fired whenever any quest changes state (questId, newState). QuestGiverVariants
    // listens so a giver updates live if its quest changes during the same scene.
    public event Action<string, SideQuestState> OnQuestStateChanged;

    private readonly Dictionary<string, SideQuestState> states = new Dictionary<string, SideQuestState>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public SideQuestState GetState(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return SideQuestState.NotStarted;
        return states.TryGetValue(questId, out var s) ? s : SideQuestState.NotStarted;
    }

    public void SetState(string questId, SideQuestState state)
    {
        if (string.IsNullOrEmpty(questId)) return;
        states[questId] = state;
        Debug.Log($"[SideQuest] '{questId}' → {state}");
        OnQuestStateChanged?.Invoke(questId, state);
    }

    // ── UnityEvent-friendly helpers (pass the quest ID string) ────────
    public void StartQuest(string questId)    => SetState(questId, SideQuestState.Active);
    public void CompleteQuest(string questId) => SetState(questId, SideQuestState.Completed);
    public void ResetQuest(string questId)    => SetState(questId, SideQuestState.NotStarted);

    // ── Queries (handy as dialogue conditions) ────────────────────────
    public bool IsNotStarted(string questId) => GetState(questId) == SideQuestState.NotStarted;
    public bool IsActive(string questId)     => GetState(questId) == SideQuestState.Active;
    public bool IsCompleted(string questId)  => GetState(questId) == SideQuestState.Completed;
}
