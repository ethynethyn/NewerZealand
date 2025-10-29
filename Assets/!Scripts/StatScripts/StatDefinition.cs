using UnityEngine;

[CreateAssetMenu(menuName = "Character/Stat Definition")]
public class StatDefinition : ScriptableObject
{
    public string statName = "New Stat";
    [TextArea]
    public string description = "Describe what this stat does.";
}
