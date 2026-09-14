using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class GeneratorPuzzle : MonoBehaviour
{
    [SerializeField] private Button slot1Button;
    [SerializeField] private TMP_Text slot1Text;
    [SerializeField] private Button slot2Button;
    [SerializeField] private TMP_Text slot2Text;

    [SerializeField] private Button slot3Button;
    [SerializeField] private TMP_Text slot3Text;

    [SerializeField] private TMP_Text currentOutputText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text accessNumberText;
    [SerializeField] private TMP_Text targetNumberText;
    [SerializeField] private MeshRenderer indicatorRenderer;

    [SerializeField] private Material offlineMaterial;
    [SerializeField] private Material onlineMaterial;
    private int slot1Value =0;
    private int slot2Value = 0;
    private int slot3Value = 0;

    [SerializeField] private int targetVoltage = 120;
    private int accessCode;
    private bool puzzleSolved = false;

    public bool PuzzleSolved
    {
        get { return puzzleSolved; }
    }

    private void Awake()
    {
        slot1Button.onClick.AddListener(ChangeSlot1Value);
        slot2Button.onClick.AddListener(ChangeSlot2Value);
        slot3Button.onClick.AddListener(ChangeSlot3Value);

        targetNumberText.text = targetVoltage + "V";
        currentOutputText.text = "0V";
        statusText.text = "Status: Offline";
        statusText.color = Color.red;
        accessNumberText.text = "--";

        indicatorRenderer.material = offlineMaterial;
    }
   
    private void ChangeSlot1Value()
    {
        slot1Value += 10;
        if (slot1Value > 100)
        {
            slot1Value = 0;
        }
        slot1Text.text = slot1Value + "V";
        CheckGenerator();
    }
    private void ChangeSlot2Value()
    {
        slot2Value += 10;
        if (slot2Value > 100)
        {
            slot2Value = 0;
        }
        slot2Text.text = slot2Value + "V";
        CheckGenerator();
    }
    private void ChangeSlot3Value()
    {
        slot3Value += 10;
        if(slot3Value > 100)
        {
            slot3Value = 0;
        }
        slot3Text.text = slot3Value + "V";
        CheckGenerator();
    }

    private void CheckGenerator()
    {
        int currentVoltage = slot1Value + slot2Value - slot3Value;
        currentOutputText.text = currentVoltage + "V";

        if (puzzleSolved)
            return;

        if (currentVoltage == targetVoltage)
        {
            statusText.text = "Status: Online";
            statusText.color = Color.green;
            accessCode = Random.Range(10, 100);
            accessNumberText.text = accessCode.ToString();
            indicatorRenderer.material = onlineMaterial;
            puzzleSolved = true;
        }
        else
        {
            statusText.text = "Status: Offline";
            statusText.color = Color.red;

            accessNumberText.text = "--";
        }
    }
}
