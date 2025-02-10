using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    public TextMeshPro helpText;
    public Boolean check;

    // void Start()
    // {
    //     helpText.gameObject.SetActive(false); // Ascunde textul la început
    // }

    public void ShowHelpMessage(Vector3 enemypos)
    {
        if(check){
            Debug.Log("AFISEZ TEXT");
            helpText.text = "Help!";
            helpText.fontSize = 5; // Asigură-te că fontul este suficient de mare
            helpText.fontStyle = FontStyles.Bold; // Setează stilul fontului
            helpText.gameObject.SetActive(true);
            Invoke("HideHelpMessage", 3f); // Ascunde mesajul după 5 secunde
            
        }
    }

    void HideHelpMessage()
    {
        check = false;
        helpText.gameObject.SetActive(false);
    }
}
