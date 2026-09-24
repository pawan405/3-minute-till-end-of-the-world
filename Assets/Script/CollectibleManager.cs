using System;

using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance;

    public bool hasChip;
    public bool hasCode;
    public string level1Code;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GiveLevel1Reward()
    {
        if (hasChip && hasCode)
            return;

        hasChip = true;

        if (!hasCode)
        {
            level1Code = UnityEngine.Random.Range(10, 100).ToString();
            hasCode = true;
        }

        Debug.Log("Level 1 Reward → Chip + Code: " + level1Code);
    }
}