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

        Time.timeScale = 0f;

        StartCoroutine(FocusInputNextFrame());
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null; // wait 1 frame so UI fully loads

        if (inputField != null)
        {
            inputField.text = "";

            inputField.ActivateInputField();
            inputField.Select();

            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SubmitName();
        }

        // keep forcing focus if player clicks away / deselects
        if (inputField != null &&
            EventSystem.current.currentSelectedGameObject != inputField.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
            inputField.ActivateInputField();
        }
    }

    public void SubmitName()
    {
        if (inputField == null) return;

        string name = inputField.text;

        if (!string.IsNullOrWhiteSpace(name))
        {
            PlayerName = name.Trim();
        }

        isActive = false;

        if (nameCanvas != null)
            nameCanvas.SetActive(false);

        Time.timeScale = 1f;
    }
}