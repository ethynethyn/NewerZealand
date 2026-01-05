using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PrefabPlace : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string requiredLayerName = "Pickup";
    public GameObject requiredPrefab;
    public bool destroyObjectOnSuccess = true;   // Destroy the collected prefab
    public bool destroyTriggerOnSuccess = false; // Destroy this trigger itself when completed

    [Header("UI Settings")]
    public TextMeshProUGUI messageUI;
    public float messageTime = 3f;

    [System.Serializable]
    public class StatChange
    {
        public Character targetCharacter;
        public string statName;
        public float amount;
    }

    [System.Serializable]
    public class JobOutcome
    {
        [Range(0, 100)]
        public float chance;

        [TextArea]
        public string message;

        public List<StatChange> statChanges = new List<StatChange>();
    }

    [System.Serializable]
    public class PostCompletionObject
    {
        public GameObject obj;
        [Range(0, 100)]
        public float activationChance = 100f;
        public bool deactivateInstead = false;
    }

    [Header("Possible Outcomes")]
    public List<JobOutcome> outcomes = new List<JobOutcome>();

    [Header("Post-Completion Objects (chance-based)")]
    public List<PostCompletionObject> postCompletionObjects = new List<PostCompletionObject>();

    private int requiredLayer;

    void Start()
    {
        requiredLayer = LayerMask.NameToLayer(requiredLayerName);

        if (requiredLayer < 0)
            Debug.LogError("Layer " + requiredLayerName + " does not exist.");

        if (messageUI != null)
            messageUI.gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != requiredLayer)
            return;

        if (requiredPrefab != null && !MatchesPrefab(other.gameObject))
            return;

        // Prevent double triggering
        if (!enabled) return;
        enabled = false; // disable this script immediately to block retriggers

        JobOutcome selectedOutcome = RunRandomOutcome();

        if (selectedOutcome != null)
            StartCoroutine(HandleOutcome(selectedOutcome, other));
    }

    private System.Collections.IEnumerator HandleOutcome(JobOutcome outcome, Collider other)
    {
        ApplyOutcome(outcome);
        ProcessPostCompletionObjects();

        // Destroy the collected prefab if needed
        if (destroyObjectOnSuccess)
            Destroy(other.gameObject);

        // Wait for message UI to finish
        if (messageUI != null)
            yield return new WaitForSeconds(messageTime);

        // Destroy this trigger if needed
        if (destroyTriggerOnSuccess)
            Destroy(gameObject);
        else
            enabled = true; // re-enable the script if we want it reusable
    }



    bool MatchesPrefab(GameObject obj)
    {
        string prefabName = requiredPrefab.name;
        string instanceName = obj.name.Replace("(Clone)", "").Trim();
        return prefabName == instanceName;
    }

    JobOutcome RunRandomOutcome()
    {
        if (outcomes.Count == 0)
        {
            Debug.LogWarning("No outcomes set on " + name);
            return null;
        }

        float totalChance = 0f;
        foreach (JobOutcome o in outcomes)
            totalChance += o.chance;

        float roll = Random.Range(0f, totalChance);
        float cumulative = 0f;

        foreach (JobOutcome o in outcomes)
        {
            cumulative += o.chance;
            if (roll <= cumulative)
                return o;
        }

        return outcomes[outcomes.Count - 1];
    }

    void ApplyOutcome(JobOutcome outcome)
    {
        foreach (StatChange change in outcome.statChanges)
        {
            if (change.targetCharacter != null)
                change.targetCharacter.ModifyStat(change.statName, change.amount);
        }

        if (messageUI != null)
            StartCoroutine(ShowMessage(outcome.message));
    }

    void ProcessPostCompletionObjects()
    {
        foreach (var postObj in postCompletionObjects)
        {
            if (postObj.obj == null) continue;

            float roll = Random.Range(0f, 100f);
            if (roll <= postObj.activationChance)
            {
                postObj.obj.SetActive(!postObj.deactivateInstead);
            }
        }
    }

    System.Collections.IEnumerator ShowMessage(string msg)
    {
        messageUI.text = msg;
        messageUI.gameObject.SetActive(true);

        yield return new WaitForSeconds(messageTime);

        messageUI.gameObject.SetActive(false);
    }
}
