using TMPro;
using UnityEngine;

public class PasswordManager : MonoBehaviour
{
    public TMP_InputField input;

    public GameObject miniGamePanel;
    public GameObject passwordPanel;

    public TMP_Text errorText;


    string correctPassword = "7391";


    public void CheckPassword()
    {
        if (input.text == correctPassword)
        {
            errorText.text = "";

            passwordPanel.SetActive(false);
            miniGamePanel.SetActive(true);
        }
        else
        {
            errorText.text = "ACCESS DENIED - WRONG PASSWORD";

            input.text = "";
        }
    }
}