using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using StarterAssets;

public class DetentionActivator : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public Transform detentionSpawnPoint;

    [Header("Detention Objects")]
    public List<GameObject> enableOnDetention = new List<GameObject>();
    public List<GameObject> disableOnDetention = new List<GameObject>();

    private PlayerInput playerInput;
    private StarterAssetsInputs starterInputs;

    void Start()
    {
        playerInput = FindObjectOfType<PlayerInput>();
        starterInputs = FindObjectOfType<StarterAssetsInputs>();
    }

    void OnEnable()
    {
        ActivateDetention();
    }

    // OnDisable intentionally does nothing — state changes are permanent until
    // DeactivateDetention() is called explicitly from outside.

    public void ActivateDetention()
    {
        // TELEPORT PLAYER
        if (player != null && detentionSpawnPoint != null)
        {
            player.position = detentionSpawnPoint.position;
            player.rotation = detentionSpawnPoint.rotation;
        }

        // DISABLE INPUT
        if (playerInput != null)
            playerInput.DeactivateInput();

        // CLEAR INPUT (prevent stuck movement)
        if (starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
        }

        // SET objects — fire and forget, nothing reverts these
        foreach (GameObject obj in enableOnDetention)
            if (obj != null) obj.SetActive(true);

        foreach (GameObject obj in disableOnDetention)
            if (obj != null) obj.SetActive(false);
    }

    public void DeactivateDetention()
    {
        // RE-ENABLE INPUT
        if (playerInput != null)
            playerInput.ActivateInput();

        if (starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
        }

        // No object reverting — leave everything exactly as it is.
        // If you need to undo specific objects, do it from the calling script.
    }
}