using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TextMatchTrigger : MonoBehaviour
{
    [Header("Input Settings")]
    public TMP_InputField inputField;
    public string requiredString = "Correct";

    [Header("UI Settings")]
    public TextMeshProUGUI messageUI;
    public float messageTime = 3f;
    public bool clearInputOnSubmit = true;

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

    [Header("Correct Outcomes")]
    public List<JobOutcome> correctOutcomes = new List<JobOutcome>();

    [Header("Incorrect Outcomes")]
    public List<JobOutcome> incorrectOutcomes = new List<JobOutcome>();

    [Header("Post-Completion Objects")]
    public List<PostCompletionObject> postCompletionObjects = new List<PostCompletionObject>();

    void Start()
    {
        if (messageUI != null)
            messageUI.gameObject.SetActive(false);

        // Listen for when user presses Enter or clicks submit button
        inputField.onSubmit.AddListener(OnInputSubmitted);
    }

    void OnDestroy()
    {
        inputField.onSubmit.RemoveListener(OnInputSubmitted);
    }

    private void OnInputSubmitted(string text)
    {
        bool isCorrect = TextMatchesRequired(text);

        if (isCorrect)
        {
            JobOutcome outcome = RunRandomOutcome(correctOutcomes);
            if (outcome != null)
                ApplyOutcome(outcome);
        }
        else
        {
            JobOutcome outcome = RunRandomOutcome(incorrectOutcomes);
            if (outcome != null)
                ApplyOutcome(outcome);
        }

        ProcessPostCompletionObjects();

        if (clearInputOnSubmit)
            inputField.text = "";
    }

    bool TextMatchesRequired(string text)
    {
        return text.Trim().ToLower() == requiredString.Trim().ToLower();
    }

    JobOutcome RunRandomOutcome(List<JobOutcome> list)
    {
        if (list.Count == 0) return null;

        float totalChance = 0;
        foreach (var o in list) totalChance += o.chance;

        float roll = Random.Range(0f, totalChance);
        float cumulative = 0f;

        foreach (var o in list)
        {
            cumulative += o.chance;
            if (roll <= cumulative)
                return o;
        }

        return list[list.Count - 1];
    }

    void ApplyOutcome(JobOutcome outcome)
    {
        foreach (var change in outcome.statChanges)
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
                postObj.obj.SetActive(!postObj.deactivateInstead);
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
