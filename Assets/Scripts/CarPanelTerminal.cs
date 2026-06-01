using UnityEngine;
using TMPro;
using System.Collections;

public class CarPanelTerminal : MonoBehaviour
{
    private TextMeshProUGUI panelText;
    private Coroutine typingCoroutine;

    // Surowe dane inżynieryjne wyświetlane na panelu w stylu retro
    private string specsTemplate = 
        "> UPLINK ESTABLISHED\n" +
        "> FETCHING VEHICLE DATA...\n\n" +
        "CHASSIS : PROMETHEUS V1\n" +
        "ENGINE  : 2.0 TSI (CCZB)\n" +
        "MAPPING : 95 RON (FACTORY)\n" +
        "CLUTCH  : REINFORCED (500Nm)\n" +
        "TIRES   : HANKOOK R-SPEC\n" +
        "WEIGHT  : 950 KG\n\n" +
        "> SYSTEM READY.";

    void Start()
    {
        // Omijamy Inspector - skrypt sam szuka tekstu po nazwie
        GameObject textObj = GameObject.Find("CarSpecsText"); 
        if (textObj != null)
        {
            panelText = textObj.GetComponent<TextMeshProUGUI>();
            panelText.text = "_"; // Migający kursor na start
        }
    }

    public void BootUpPanel()
    {
        if (panelText == null) return;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(specsTemplate));
    }

    public void ResetPanel()
    {
        if (panelText != null)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            panelText.text = "_";
        }
    }

    private IEnumerator TypeText(string textToType)
    {
        panelText.text = "";
        foreach (char letter in textToType)
        {
            panelText.text += letter;
            // Losowe opóźnienie (0.01 - 0.04s) dla efektu wczytywania starego komputera
            yield return new WaitForSeconds(Random.Range(0.01f, 0.04f)); 
        }
    }
}