using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BreakerPuzzle : MonoBehaviour
{
    [SerializeField] private Button breaker1;
    [SerializeField] private TMP_Text breaker1Text;

    [SerializeField] private Button breaker2;
    [SerializeField] private TMP_Text breaker2Text;

    [SerializeField] private Button breaker3;
    [SerializeField] private TMP_Text breaker3Text;

    [SerializeField] private Button breaker4;
    [SerializeField] private TMP_Text breaker4Text;

    [SerializeField] private Button breaker5;
    [SerializeField] private TMP_Text breaker5Text;

    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text accessNumberText;
    [SerializeField] private TMP_Text indicatorText;

    [SerializeField] private Image indicatorImage;

    [SerializeField] private Sprite offlineSprite;
    [SerializeField] private Sprite onlineSprite;

    [SerializeField] private MeshRenderer indicatorRenderer;

    [SerializeField] private Material offlineMaterial;
    [SerializeField] private Material onlineMaterial;

    private bool breaker1On = false;
    private bool breaker2On = false;
    private bool breaker3On = false;
    private bool breaker4On = false;
    private bool breaker5On = false;

    private int accessCode;
    private bool puzzleSolved = false;

    private void Awake()
    {
        breaker1.onClick.AddListener(ToggleBreaker1);
        breaker2.onClick.AddListener(ToggleBreaker2);
        breaker3.onClick.AddListener(ToggleBreaker3);
        breaker4.onClick.AddListener(ToggleBreaker4);
        breaker5.onClick.AddListener(ToggleBreaker5);

        breaker1Text.text = "OFF";
        breaker2Text.text = "OFF";
        breaker3Text.text = "OFF";
        breaker4Text.text = "OFF";
        breaker5Text.text = "OFF";

        statusText.text = "Status : Offline";
        statusText.color = Color.red;

        indicatorText.text = "Configure all breakers.";

        accessNumberText.text = "--";

        indicatorImage.sprite = offlineSprite;
        indicatorRenderer.material = offlineMaterial;
    }

    private void ToggleBreaker1()
    {
        Debug.Log("Breaker 1 Clicked");
        breaker1On = !breaker1On;

        if (breaker1On)
        {
            breaker1Text.text = "ON";
        }
        else
        {
            breaker1Text.text = "OFF";
        }

        CheckBreakers();
    }

    private void ToggleBreaker2()
    {
        breaker2On = !breaker2On;

        if (breaker2On)
            breaker2Text.text = "ON";
        else
            breaker2Text.text = "OFF";

        CheckBreakers();
    }

    private void ToggleBreaker3()
    {
        breaker3On = !breaker3On;

        if (breaker3On)
            breaker3Text.text = "ON";
        else
            breaker3Text.text = "OFF";

        CheckBreakers();
    }

    private void ToggleBreaker4()
    {
        breaker4On = !breaker4On;

        if (breaker4On)
            breaker4Text.text = "ON";
        else
            breaker4Text.text = "OFF";

        CheckBreakers();
    }

    private void ToggleBreaker5()
    {
        breaker5On = !breaker5On;

        if (breaker5On)
            breaker5Text.text = "ON";
        else
            breaker5Text.text = "OFF";

        CheckBreakers();
    }

    private void CheckBreakers()
    {
        if (puzzleSolved)
            return;

        if (breaker1On &&
            !breaker2On &&
            breaker3On &&
            breaker4On &&
            !breaker5On)
        {
            statusText.text = "Status : Online";
            statusText.color = Color.green;

            indicatorText.text = "Circuit Stable!";

            indicatorImage.sprite = onlineSprite;

            accessCode = Random.Range(10, 100);
            accessNumberText.text = accessCode.ToString();
            indicatorRenderer.material = onlineMaterial;
            puzzleSolved = true;
        }
    }
    public bool PuzzleSolved
    {
        get { return puzzleSolved; }
    }


}