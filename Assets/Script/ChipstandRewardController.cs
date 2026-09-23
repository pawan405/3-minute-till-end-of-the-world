using UnityEngine;

[DisallowMultipleComponent]
public sealed class ChipstandRewardController : MonoBehaviour
{
    [Header("Rewards")]
    [SerializeField] private GameObject level4Chip;

    private void Awake()
    {
        RefreshRewards();
    }

    /// <summary>
    /// Refreshes the chipstand to match the rewards earned by the player.
    /// </summary>
    public void RefreshRewards()
    {
        if (level4Chip != null)
        {
            level4Chip.SetActive(ChipProgress.IsLevel4ChipUnlocked());
        }
    }
}
