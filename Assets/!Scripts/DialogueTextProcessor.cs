using UnityEngine;

public static class DialogueTextProcessor
{
    public static string PlayerName = "Player";

    public static string Process(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Replace tokens
        text = text.Replace("{Player}", PlayerName);

        return text;
    }
}