using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance;

    [Header("Crafting Recipes")]
    public List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    [Header("Crafting Feedback")]
    public GameObject craftVFX;
    public AudioClip craftSFX;
    public CraftingPopup popupUI;
    public float popupDuration = 2f;

    [Header("Optional Progress UI")]
    public Image progressBar; // Assign a UI Image (type: Filled)

    [Header("Crafting Timing")]
    public float anticipationTime = 0.15f; // delay before shrinking
    public float shrinkTime = 0.15f;       // how long ingredients shrink
    public float fadeInTime = 0.3f;        // how long the result scales/bounces in


    private void Awake()
    {
        Instance = this;
    }

    public bool TryCraft(GameObject a, GameObject b, out GameObject result)
    {
        foreach (var recipe in recipes)
        {
            bool match =
                (a.name.Contains(recipe.ingredientA.name) && b.name.Contains(recipe.ingredientB.name)) ||
                (a.name.Contains(recipe.ingredientB.name) && b.name.Contains(recipe.ingredientA.name));

            if (match)
            {
                result = recipe.resultPrefab;
                return true;
            }
        }

        result = null;
        return false;
    }

    public void SpawnCraftFeedback(GameObject crafted, Vector3 spawnPos)
    {
        if (craftVFX != null)
            Instantiate(craftVFX, spawnPos, Quaternion.identity);

        if (craftSFX != null)
            AudioSource.PlayClipAtPoint(craftSFX, spawnPos);

        if (popupUI != null)
        {
            string itemName = crafted.name.Replace("(Clone)", "");
            popupUI.ShowPopup(itemName + " CRAFTED");
        }
    }
}
