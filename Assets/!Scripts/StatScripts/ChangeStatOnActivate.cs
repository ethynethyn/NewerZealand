using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class StatEffectOnEnable : MonoBehaviour
{
    [Header("Target & Effect")]
    public Character targetCharacter;
    public string statToModify = "Health";
    public float statChangeAmount = 10f;

    [Header("Optional Cost")]
    public bool requireCost = false;
    public string costStat = "Energy";
    public float costAmount = 5f;

    [Header("Timing")]
    public float delayBeforeEffect = 1f;
    public bool onlyTriggerOnce = true;
    public bool repeatOnEnable = false;

    [Header("Auto Deactivation")]
    public bool autoDisableAfterTime = false;
    public float autoDisableDelay = 5f;

    [Header("Events")]
    public UnityEvent onEffectSuccess;
    public UnityEvent onEffectFail;

    private bool hasTriggered = false;
    private Coroutine activeRoutine = null;
    private Coroutine disableRoutine = null;

    void OnEnable()
    {
        if (repeatOnEnable)
        {
            activeRoutine = StartCoroutine(TriggerEffectLoop());
        }
        else
        {
            if (!onlyTriggerOnce || !hasTriggered)
                activeRoutine = StartCoroutine(TriggerOnce());
        }

        if (autoDisableAfterTime)
            disableRoutine = StartCoroutine(AutoDisable());
    }

    void OnDisable()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        if (disableRoutine != null) StopCoroutine(disableRoutine);
    }

    IEnumerator TriggerOnce()
    {
        yield return new WaitForSeconds(delayBeforeEffect);
        TryApplyEffect();
    }

    IEnumerator TriggerEffectLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(delayBeforeEffect);
            TryApplyEffect();
        }
    }

    IEnumerator AutoDisable()
    {
        yield return new WaitForSeconds(autoDisableDelay);
        gameObject.SetActive(false);
    }

    void TryApplyEffect()
    {
        if (targetCharacter == null) return;

        if (requireCost)
        {
            float currentCostStat = targetCharacter.GetStatValue(costStat);
            if (currentCostStat < costAmount)
            {
                Debug.Log($"[StatEffectOnEnable] Not enough {costStat} to pay cost.");
                onEffectFail.Invoke();
                return;
            }
            else
            {
                targetCharacter.ModifyStat(costStat, -costAmount);
            }
        }

        targetCharacter.ModifyStat(statToModify, statChangeAmount);
        onEffectSuccess.Invoke();
        hasTriggered = true;

        Debug.Log($"[StatEffectOnEnable] {statToModify} changed by {statChangeAmount} on {targetCharacter.characterName}.");
    }
}
