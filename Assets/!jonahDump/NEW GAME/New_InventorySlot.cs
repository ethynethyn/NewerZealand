using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One square on screen. Goes on the slot prefab.
/// </summary>
public class New_InventorySlot : MonoBehaviour
{
    [Tooltip("The Image showing the MS Paint square/frame.")]
    public Image squareImage;

    [Tooltip("Child Image showing the item art. Leave the sprite empty in the prefab.")]
    public Image iconImage;

    [Header("Juice")]
    public bool popOnSpawn = true;
    public float popTime = 0.18f;

    New_ItemID myItem;
    Vector3 baseScale = Vector3.one;
    bool cachedScale;

    public New_ItemID Item { get { return myItem; } }

    void Awake()
    {
        if (!cachedScale)
        {
            baseScale = transform.localScale;
            cachedScale = true;
        }
    }

    public void Setup(New_ItemDatabase db, New_ItemID id)
    {
        if (!cachedScale)
        {
            baseScale = transform.localScale;
            cachedScale = true;
        }

        myItem = id;
        gameObject.name = "New_Slot_" + id;

        New_ItemDatabase.Entry e = (db != null) ? db.Get(id) : null;

        if (squareImage != null)
        {
            Sprite sq = null;
            if (e != null && e.squareOverride != null) sq = e.squareOverride;
            else if (db != null) sq = db.defaultSquare;

            if (sq != null) squareImage.sprite = sq;
        }

        if (iconImage != null)
        {
            iconImage.sprite = (e != null) ? e.icon : null;
            iconImage.enabled = (iconImage.sprite != null);
        }

        if (e == null)
        {
            Debug.LogWarning("New_InventorySlot: no database entry for " + id + ", square will be blank.", this);
        }

        if (popOnSpawn && isActiveAndEnabled)
        {
            StopAllCoroutines();
            StartCoroutine(Pop());
        }
    }

    IEnumerator Pop()
    {
        float t = 0f;
        transform.localScale = Vector3.zero;

        while (t < popTime)
        {
            // unscaled so it still plays if you froze time during the pickup
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / popTime);
            float ease = Mathf.Sin(p * Mathf.PI * 0.5f);          // ease out
            float bump = 1f + 0.25f * Mathf.Sin(p * Mathf.PI);    // little overshoot
            transform.localScale = baseScale * ease * bump;
            yield return null;
        }

        transform.localScale = baseScale;
    }
}
