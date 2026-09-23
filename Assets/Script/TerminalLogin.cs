using UnityEngine;
using TMPro;
using System.Collections;

public class TerminalLogin : MonoBehaviour
{
    public TMP_InputField passwordInput;

    public TMP_Text statusText;
    public TMP_Text terminalText;

    public GameObject passwordUI;
    public GameObject terminalUI;
    private void Start()
    {
        passwordUI.SetActive(true);
        terminalUI.SetActive(false);

        statusText.text = "";
        terminalText.text = "";
    }

    public void CheckPassword()
    {
        if (passwordInput.text == "1234")
        {
            StartCoroutine(StartTerminal());
        }
        else
        {
            statusText.text = "ACCESS DENIED\nWRONG PASSWORD";
        }
    }


    IEnumerator StartTerminal()
    {
        passwordUI.SetActive(false);

        statusText.text = "ACCESS GRANTED";

        yield return new WaitForSeconds(1);


        statusText.text = "";

        terminalText.text = "> Initializing system...";
        yield return new WaitForSeconds(1);


        terminalText.text += "\n> Checking security...";
        yield return new WaitForSeconds(1);


        terminalText.text += "\n> Loading modules...";
        yield return new WaitForSeconds(1);


        terminalText.text += "\n> Connecting reactor control...";
        yield return new WaitForSeconds(1);


        terminalText.text += "\n> System ready.";

        yield return new WaitForSeconds(1);


        terminalText.text += "\n\nNEXUS TERMINAL v2.4";
        terminalText.text += "\nType command...";
    }
}