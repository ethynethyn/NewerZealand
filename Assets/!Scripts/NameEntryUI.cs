using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class PlayerNameManager : MonoBehaviour
{
    public static string PlayerName = "Player";

    [Header("UI")]
    public GameObject nameCanvas;
    public TMP_InputField inputField;

    private bool isActive = true;

    private void Start()
    {
        if (nameCanvas != null)
            nameCanvas.SetActive(true);

  

        StartCoroutine(FocusInputNextFrame());
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;

        if (inputField != null)
        {
            inputField.text = "";

            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
            inputField.ActivateInputField();

            MoveCaretToEnd();
        }
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Only refocus if NOT clicking UI
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                inputField.ActivateInputField();
                MoveCaretToEnd();
            }
        }
    }

    private void MoveCaretToEnd()
    {
        int end = inputField.text.Length;
        inputField.caretPosition = end;
        inputField.selectionAnchorPosition = end;
        inputField.selectionFocusPosition = end;
    }

    // 🔥 Called by your UI Button
    public void SubmitName()
    {
        if (inputField == null) return;

        string name = inputField.text;

        if (!string.IsNullOrWhiteSpace(name))
            PlayerName = name.Trim();

        isActive = false;

        if (nameCanvas != null)
            nameCanvas.SetActive(false);

        Time.timeScale = 1f;
    }
}