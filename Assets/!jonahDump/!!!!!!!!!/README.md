# DAudio — Undertale-style voice blips for Dialogue Editor

Per-speaker text blips that play while dialogue scrolls out. **No Dialogue Editor scripts are modified.**
The system reads `ConversationManager`'s state through reflection, so it survives plugin updates and
works with both `UnityEngine.UI.Text` and TextMeshPro setups.

## Files

| File | What it is |
|---|---|
| `DAudio_VoiceProfile.cs` | ScriptableObject — one character's voice (clips, pitch, cadence) |
| `DAudio_VoiceDatabase.cs` | ScriptableObject — speaker name → profile, plus a default |
| `DAudio_DialogueVoice.cs` | The driver. Watches text reveal, fires blips |
| `DAudio_BlipPlayer.cs` | AudioSource pool so blips can overlap with independent pitch |
| `DAudio_ConversationBridge.cs` | Reflection layer that reads ConversationManager |
| `DAudio_TextUtils.cs` | Rich-text stripping, speaker-name normalising |
| `DAudio_VoicePreview.cs` | Optional. Audition a voice in play mode without a conversation |

## Setup

1. Drop the `DAudio` folder anywhere under `Assets/`.
2. **Create profiles.** `Assets > Create > DAudio > Voice Profile`, one per character.
   Assign a short clip (0.03–0.15s) to `Clips`. Set `Pitch` lower for gravelly characters, higher for squeaky ones.
3. **Create the database.** `Assets > Create > DAudio > Voice Database`.
   - Set `Default Profile` — this covers every speaker you haven't listed.
   - Add an entry per character. **`Speaker Name` must match the `Name` field on the speech node** in the
     Dialogue Editor window. Case and surrounding spaces don't matter.
   - Use `Aliases` for name variants like `Sans (tired)` or `???`.
4. **Add the component.** Select your `ConversationManager` GameObject → `Add Component` → `DAudio > Dialogue Voice`.
   (`DAudio_BlipPlayer` is added automatically.) Drag your database into the `Database` slot.
5. Press play and start a conversation.

That's the whole setup. Because the speaker is read per-node, a single conversation with three
different characters talking will swap voices automatically at each node.

## Tuning

- **`Characters Per Blip`** — `1` blips on every letter (classic Undertale). `2`–`3` is calmer.
- **`Min Seconds Between Blips`** — the rate limiter. Raise it if a fast `Scroll Speed` sounds like a buzzsaw.
- **`Pitch Variance`** — `0` gives a flat mechanical voice. `0.05`–`0.12` sounds more organic.
- **`Skip Punctuation`** — off by default; turning it on makes speech feel more clipped and deliberate.
- **`Skip Whitespace`** — leave on. This is what makes word gaps audible.

Turn on **`Log Speaker Changes`** while setting up. It prints the exact speaker string and which profile
was picked every time a node starts, which makes name mismatches obvious immediately.

## Making the blips

Any short percussive sample works. In FL Studio: a single sine or square blip with a fast decay
envelope, ~60–90ms, rendered dry. One clip per character is plenty — the pitch and cadence settings
do most of the character work. Two or three clips with `Randomise Clips` on adds a bit of texture
if a voice starts sounding too mechanical.

## Notes and gotchas

- **The node's own `Audio` field.** Dialogue Editor already plays that clip once when the node opens.
  Leave it empty if you don't want a one-shot on top of the blips. Alternatively, tick
  `Use Node Audio As Blip` on `DAudio_DialogueVoice` and that clip becomes the blip for that specific
  node — handy for one-off moments without creating a whole profile.
- **Instant text.** If `Scroll Text` is off on the ConversationManager, the whole line appears in one
  frame. `Max Blips Per Frame` keeps that to a single blip instead of a burst.
- **Option nodes** don't blip. Only speech nodes reveal text over time.
- **Runtime voice swaps.** `DAudio_DialogueVoice.Instance.SetSpeakerOverride("Sans", scaryProfile);`
  and `ClearSpeakerOverride("Sans")` when done.
- **Global mute.** `DAudio_DialogueVoice.Instance.SetMasterVolume(0f);`

## Troubleshooting

**"Couldn't find the dialogue text component" warning** — auto-detection missed it. Select the
`DAudio_DialogueVoice` component, expand *Manual overrides*, and drag in the `Text` /
`TextMeshProUGUI` that displays the dialogue body. Everything else keeps working.

**Compile error on `using DialogueEditor;`** — your copy of the asset uses a different namespace.
Change that one line at the top of `DAudio_ConversationBridge.cs`.

**Assembly definition errors** — if Dialogue Editor sits inside an `.asmdef`, either put these
scripts in the same assembly or add a reference to it from yours.

**Right character, wrong voice** — turn on `Log Speaker Changes` and compare the logged string to
your database entry. Trailing spaces and rich-text tags in the node's `Name` field are the usual culprits.

**No sound at all** — check the profile has a clip assigned, check `Master Volume` on the
`DAudio_BlipPlayer`, and confirm there's an `AudioListener` in the scene.
