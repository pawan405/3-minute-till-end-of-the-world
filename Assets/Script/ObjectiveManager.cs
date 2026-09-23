using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private TMP_Text objectiveText;

    private void Awake()
    {
        Instance = this;
    }

    public void SetObjective(string objective)
    {
        objectivePanel.SetActive(true);
        objectiveText.text = objective;
    }

    public void CompleteObjective()
    {
        objectivePanel.SetActive(false);
    }
}