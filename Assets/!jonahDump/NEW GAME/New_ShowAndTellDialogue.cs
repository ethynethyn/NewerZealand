using System.Collections.Generic;
using UnityEngine;
using DialogueEditor;

/// <summary>
/// The only script here that touches the DialogueEditor plugin.
/// If the plugin's API doesn't match, this is the one file to fix.
/// Everything else works without it.
///
/// Hook New_ShowAndTell.onItemPresented to PlayReaction, and this picks
/// which conversation to run: a specific one for that item if you wrote one,
/// otherwise the generic reaction for its rarity.
/// </summary>
public class New_ShowAndTellDialogue : MonoBehaviour
{
    [System.Serializable]
    public class ItemReaction
    {
        public New_ItemID item;
        public NPCConversation conversation;
    }

    public New_ShowAndTell showAndTell;

    [Header("Specific reactions, checked first")]
    public List<ItemReaction> itemReactions = new List<ItemReaction>();

    [Header("Fallback reaction per rarity")]
    public NPCConversation commonReaction;
    public NPCConversation rareReaction;
    public NPCConversation legendaryReaction;

    [Header("Conversation parameters")]
    [Tooltip("Int parameter set before the conversation starts, so nodes can branch on it. Leave blank to skip.")]
    public string starsParameterName = "ShowAndTellStars";

    [Tooltip("Int parameter for rarity: 0 common, 1 rare, 2 legendary. Leave blank to skip.")]
    public string rarityParameterName = "ShowAndTellRarity";

    /// <summary>Drag this into onItemPresented. Takes the item as a dynamic argument.</summary>
    public void PlayReaction(New_ItemID id)
    {
        NPCConversation convo = FindConversation(id);

        if (convo == null)
        {
            Debug.LogWarning("New_ShowAndTellDialogue: no conversation set for " + id + " or its rarity.", this);
            return;
        }

        if (ConversationManager.Instance == null)
        {
            Debug.LogWarning("New_ShowAndTellDialogue: no ConversationManager in the scene.", this);
            return;
        }

        if (showAndTell != null)
        {
            if (!string.IsNullOrEmpty(starsParameterName))
            {
                ConversationManager.Instance.SetInt(starsParameterName, showAndTell.LastStars);
            }

            if (!string.IsNullOrEmpty(rarityParameterName))
            {
                ConversationManager.Instance.SetInt(rarityParameterName, (int)showAndTell.LastRarity);
            }
        }

        ConversationManager.Instance.StartConversation(convo);
    }

    /// <summary>
    /// Same thing with no argument, in case the Inspector won't offer the
    /// dynamic version. Reads whatever was presented last.
    /// </summary>
    public void PlayReactionForLastItem()
    {
        if (showAndTell == null)
        {
            Debug.LogWarning("New_ShowAndTellDialogue: showAndTell not assigned.", this);
            return;
        }

        PlayReaction(showAndTell.LastItem);
    }

    NPCConversation FindConversation(New_ItemID id)
    {
        for (int i = 0; i < itemReactions.Count; i++)
        {
            if (itemReactions[i] == null) continue;
            if (!itemReactions[i].item.Equals(id)) continue;
            if (itemReactions[i].conversation == null) continue;
            return itemReactions[i].conversation;
        }

        New_ItemRarity rarity = (showAndTell != null) ? showAndTell.GetRarity(id) : New_ItemRarity.Common;

        switch (rarity)
        {
            case New_ItemRarity.Legendary: return legendaryReaction;
            case New_ItemRarity.Rare:      return rareReaction;
            default:                       return commonReaction;
        }
    }
}
