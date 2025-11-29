using UnityEngine;
using TMPro;
using System.Collections;

public class CraftingPopup : MonoBehaviour
{
    public TextMeshProUGUI textElement;
    private Coroutine currentRoutine;

    public void ShowPopup(string msg)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(PopupRoutine(msg));
    }

    private IEnumerator PopupRoutine(string msg)
    {
        textElement.text = msg;
        textElement.alpha = 1f;

        float duration = CraftingManager.Instance.popupDuration;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            textElement.alpha = Mathf.Lerp(1f, 0f, timer / duration);
            yield return null;
        }

        textElement.alpha = 0f;
    }
}
