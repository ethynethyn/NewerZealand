using System.Text;

/// <summary>
/// Small shared helpers. No dependency on Dialogue Editor.
/// </summary>
public static class DAudio_TextUtils
{
    private const int kMaxTagLength = 32;

    /// <summary>
    /// Removes rich text tags (&lt;color&gt;, &lt;b&gt;, &lt;size&gt;...) so they don't get counted as
    /// spoken characters. Also drops a half-typed tag at the end of the string, which happens
    /// constantly while Dialogue Editor is scrolling text out character by character.
    /// </summary>
    public static string StripRichText(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        if (input.IndexOf('<') < 0) return input;

        StringBuilder sb = new StringBuilder(input.Length);

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '<')
            {
                int close = input.IndexOf('>', i + 1);

                // A complete tag -> skip it entirely.
                if (close > i && close - i <= kMaxTagLength)
                {
                    i = close;
                    continue;
                }

                // An unterminated tag right at the end -> it's still being typed out, drop the tail.
                if (close < 0 && input.Length - i <= kMaxTagLength)
                    break;

                // Otherwise it's just a stray '<' in the prose, keep it.
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Turns a speech node's Name into a stable lookup key: tags removed, trimmed, lowercased.
    /// </summary>
    public static string NormaliseSpeaker(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        return StripRichText(raw).Trim().ToLowerInvariant();
    }
}
