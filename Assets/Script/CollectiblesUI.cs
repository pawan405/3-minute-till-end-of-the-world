using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class CollectiblesUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;

    [Header("Chip")]
    [SerializeField] private GameObject chipCollected;
    [SerializeField] private GameObject chipLocked;

    [Header("Code")]
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private GameObject codeCollected;
    [SerializeField] private GameObject codeLocked;

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            Debug.Log("G PRESSED");

            TogglePanel();
        }
    }

    private void TogglePanel()
    {
        if (panel == null)
        {
            Debug.LogError("Collectibles Panel is NOT assigned!");
            return;
        }

        bool open = !panel.activeSelf;

        panel.SetActive(open);

        Debug.Log("Collectibles Panel: " + open);

        if (open)
            RefreshUI();
    }

    private void RefreshUI()
    {
        if (CollectibleManager.Instance == null)
        {
            Debug.LogError("CollectibleManager.Instance is NULL!");
            return;
        }

        var manager = CollectibleManager.Instance;

        if (chipCollected != null)
            chipCollected.SetActive(manager.hasChip);

        if (chipLocked != null)
            chipLocked.SetActive(!manager.hasChip);

        if (codeCollected != null)
            codeCollected.SetActive(manager.hasCode);

        if (codeLocked != null)
            codeLocked.SetActive(!manager.hasCode);

        if (codeText != null)
            codeText.text = manager.hasCode
                ? manager.level1Code
                : "??";
    }
}