using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIManagerBOSS : MonoBehaviour
{
    public TextMeshPro helpText;
    public Boolean check;

    // void Start()
    // {
    //     helpText.gameObject.SetActive(false); // Ascunde textul la început
    // }

    public void ShowHelpMessageBOSS(Vector3 bosspos)
    {
        if(check){
            Debug.Log("AFISEZ TEXT");
            helpText.text = "Heal!";
            helpText.fontSize = 5; // Asigură-te că fontul este suficient de mare
            helpText.fontStyle = FontStyles.Bold; // Setează stilul fontului
            helpText.gameObject.SetActive(true);
            Invoke("HideHelpMessageBOSS", 3f); // Ascunde mesajul după 5 secunde
            
        }
    }

    void HideHelpMessageBOSS()
    {
        check = false;
        helpText.gameObject.SetActive(false);
    }
}
