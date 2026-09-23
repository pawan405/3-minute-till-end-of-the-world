using UnityEngine;

/// <summary>
/// Stores persistent chip rewards earned by the player.
/// </summary>
public static class ChipProgress
{
    private const string Level4ChipUnlockKey = "Level4ChipUnlocked";
    private const int UnlockedValue = 1;

    /// <summary>
    /// Returns whether the Level 4 chip has been earned.
    /// </summary>
    public static bool IsLevel4ChipUnlocked()
    {
        return PlayerPrefs.GetInt(Level4ChipUnlockKey, 0) == UnlockedValue;
    }

    /// <summary>
    /// Persists the Level 4 chip reward as earned.
    /// </summary>
    public static void UnlockLevel4Chip()
    {
        PlayerPrefs.SetInt(Level4ChipUnlockKey, UnlockedValue);
        PlayerPrefs.Save();
    }
}
