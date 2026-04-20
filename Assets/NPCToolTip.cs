using UnityEngine;

public class NPCTooltip : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName = "John";

    [Header("Relationship")]
    public bool isFriend = false;

    [TextArea]
    public string description = "A mysterious person.";

    public string GetTooltipText()
    {
        string nameText = "<b><color=yellow>" + npcName + "</color></b>";

        string statusColor = isFriend ? "green" : "red";
        string statusText = "<color=" + statusColor + ">" + (isFriend ? "Friend" : "Not Friends") + "</color>";

        return nameText + "\n" + statusText + "\n\n" + description;
    }
}