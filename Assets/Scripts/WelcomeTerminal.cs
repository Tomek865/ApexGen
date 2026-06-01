using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class WelcomeTerminal : MonoBehaviour
{
    private TextMeshProUGUI welcomeText;
    private Coroutine typingCoroutine;
    private string welcomeMessage = 
        "> APEXGEN SYSTEM INITIALIZED\n" +
        "> STATUS: AWAITING TRACK GEOMETRY...\n\n" +
        "INPUT REQUIRED: Draw track vector on the main panel.\n\n" +
        "TELEMETRY & CONTROLS:\n" +
        "[ W ] - THROTTLE\n" +
        "[ S ] - BRAKE / REVERSE\n" +
        "[ A ] [ D ] - STEERING\n" +
        "[ SPACE ] - HANDBRAKE\n\n" +
        "> SYSTEM READY_";

    void Start()
    {
        welcomeText = GetComponent<TextMeshProUGUI>();
        typingCoroutine = StartCoroutine(TypeText(welcomeMessage));
    }

    private IEnumerator TypeText(string textToType)
    {
        welcomeText.text = "";
        foreach (char letter in textToType)
        {
            welcomeText.text += letter;
            yield return new WaitForSeconds(Random.Range(0.01f, 0.03f)); 
        }
    }

    public void HideMessage()
    {
        // Przerywamy pisanie (jeśli ktoś zaczął rysować zanim tekst się skończył)
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        // Czyścimy tekst i wyłączamy cały obiekt
        welcomeText.text = ""; 
        gameObject.SetActive(false); 
    }
}