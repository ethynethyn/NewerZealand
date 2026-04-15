using UnityEngine;

[CreateAssetMenu(menuName = "Hand UI/Animation Set")]
public class HandAnimationSet : ScriptableObject
{
    public Sprite[] frames;
    public float frameRate = 0.2f;
}