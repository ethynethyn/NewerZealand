/// <summary>
/// Dead simple. Just holds which morning tea we're up to.
///
/// Read it:   MorningTeaManager.morningTeaNumber
/// Set it:    MorningTeaManager.morningTeaNumber = 2;
/// Bump it:   MorningTeaManager.morningTeaNumber++;
///
/// Doesn't go on a GameObject. It's static, so it survives scene loads
/// and resets to 1 when you stop playing.
/// </summary>
public static class MorningTeaManager
{
    public static int morningTeaNumber = 1;
}
