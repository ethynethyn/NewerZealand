using System.Collections.Generic;
using UnityEngine;
using TMPro;

// A cup that catches sugar.
//
// SETUP: This component goes on a GameObject whose OWN collider is a TRIGGER
// sized to the cup's interior (this is the counting zone — make it as tall as
// the walls so a full pile is fully counted). The physical walls + floor are
// SEPARATE child colliders (solid, no Rigidbody) so the sugar actually stacks.
[RequireComponent(typeof(Collider2D))]
public class SugarCup : MonoBehaviour
{
    [Header("Goal")]
    [Min(0), Tooltip("How many grains this cup needs to be considered full.")]
    public int target = 10;

    [Header("Optional UI")]
    [Tooltip("Drag a TMP text here to show 'current / target' for this cup.")]
    public TMP_Text label;
    public string labelFormat = "{0} / {1}";

    private readonly HashSet<SugarGrain> inside = new HashSet<SugarGrain>();

    public int CurrentCount
    {
        get
        {
            inside.RemoveWhere(g => g == null); // drop any destroyed grains
            return inside.Count;
        }
    }

    public bool IsFull => CurrentCount >= target;

    void Reset()
    {
        // Default this object's collider to a trigger when first added.
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var grain = other.GetComponent<SugarGrain>();
        if (grain == null) grain = other.GetComponentInParent<SugarGrain>();
        if (grain == null) return;
        inside.Add(grain);
        grain.currentCup = this;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var grain = other.GetComponent<SugarGrain>();
        if (grain == null) grain = other.GetComponentInParent<SugarGrain>();
        if (grain == null) return;
        inside.Remove(grain);
        if (grain.currentCup == this) grain.currentCup = null;
    }

    void Update()
    {
        if (label != null) label.text = string.Format(labelFormat, CurrentCount, target);
    }
}
