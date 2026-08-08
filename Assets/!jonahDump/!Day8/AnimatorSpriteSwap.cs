using UnityEngine;

/// <summary>
/// Little helper for billboard sprite characters.
/// Drop it on the character, point it at the Animator, then from ANY UnityEvent slot
/// (Dialogue Editor node events, DialogueCycler entry events, buttons, whatever)
/// pick AnimatorSpriteSwap.Play and type the state name in the little text field.
/// </summary>
public class AnimatorSpriteSwap : MonoBehaviour
{
    public Animator animator;

    void Reset() { animator = GetComponentInChildren<Animator>(); }
    void Awake() { if (animator == null) animator = GetComponentInChildren<Animator>(); }

    /// <summary> Jump the animator straight to this state (i.e. show this sprite/expression). </summary>
    public void Play(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        animator.Play(stateName, 0, 0f);
    }

    /// <summary> Fire an animator trigger instead, if your controller works off triggers. </summary>
    public void FireTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;
        animator.SetTrigger(triggerName);
    }
}
