
using TMPro;
using UnityEngine;

public class PasswordManager : MonoBehaviour
{
    public TMP_InputField input;
    public GameObject miniGamePanel;
    public GameObject passwordPanel;

    string correctPassword = "7391";


    public void CheckPassword()
    {
        if (input.text == correctPassword)
        {
            passwordPanel.SetActive(false);
            miniGamePanel.SetActive(true);
        }
        else
        {
            Debug.Log("Wrong Password");
        }
    }
}