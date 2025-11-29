using UnityEngine;
using System;

public class PhoneDrugInteraction : MonoBehaviour
{
    [Serializable]
    public class DrugOptionObjects
    {
        public GameObject[] objectsToActivate;
    }

    [Header("Raycast Settings")]
    public float interactDistance = 3f;
    public LayerMask interactLayer;
    public Camera playerCamera;

    [Header("UI Settings")]
    public GameObject drugMenuUI;
    public GameObject[] drugOptionsUI;
    private int currentIndex = 0;

    [Header("Player Control Reference")]
    public Behaviour playerInputComponent;

    [Header("Objects Disabled During Menu")]
    public GameObject[] disableWhileMenu;

    [Header("Objects Enabled During Menu")]
    public GameObject[] enableWhileMenu;

    [Header("Per-Drug Activation Objects")]
    public DrugOptionObjects[] optionActivationObjects;

    private bool menuOpen = false;

    void Start()
    {
        CloseMenu();
    }

    void Update()
    {
        if (!menuOpen)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    OpenMenu();
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.A)) CycleOption(-1);
            if (Input.GetKeyDown(KeyCode.D)) CycleOption(1);

            if (Input.GetKeyDown(KeyCode.E)) ConfirmSelection();
            if (Input.GetKeyDown(KeyCode.Q)) CloseMenu();
        }
    }

    void OpenMenu()
    {
        menuOpen = true;
        currentIndex = 0;

        drugMenuUI.SetActive(true);
        UpdateUI();

        if (playerInputComponent != null)
            playerInputComponent.enabled = false;

        foreach (var obj in disableWhileMenu)
            obj.SetActive(false);
        foreach (var obj in enableWhileMenu)
            obj.SetActive(true);
    }

    void CloseMenu()
    {
        menuOpen = false;
        drugMenuUI.SetActive(false);

        if (playerInputComponent != null)
            playerInputComponent.enabled = true;

        foreach (var obj in disableWhileMenu)
            obj.SetActive(true);
        foreach (var obj in enableWhileMenu)
            obj.SetActive(false);

        DisableAllOptionActivators();

        for (int i = 0; i < drugOptionsUI.Length; i++)
            drugOptionsUI[i].SetActive(false);
    }

    void CycleOption(int direction)
    {
        DisableAllOptionActivators();

        currentIndex += direction;
        if (currentIndex < 0) currentIndex = drugOptionsUI.Length - 1;
        if (currentIndex >= drugOptionsUI.Length) currentIndex = 0;

        UpdateUI();
    }

    void UpdateUI()
    {
        for (int i = 0; i < drugOptionsUI.Length; i++)
            drugOptionsUI[i].SetActive(i == currentIndex);

        EnableOptionActivators(currentIndex);
    }

    void ConfirmSelection()
    {
        Debug.Log("Player selected drug: " + currentIndex);
        // Insert drug effect logic here
    }

    void DisableAllOptionActivators()
    {
        if (optionActivationObjects == null) return;

        foreach (var group in optionActivationObjects)
        {
            foreach (var obj in group.objectsToActivate)
                obj.SetActive(false);
        }
    }

    void EnableOptionActivators(int index)
    {
        if (optionActivationObjects == null || index >= optionActivationObjects.Length) return;

        foreach (var obj in optionActivationObjects[index].objectsToActivate)
            obj.SetActive(true);
    }
}
