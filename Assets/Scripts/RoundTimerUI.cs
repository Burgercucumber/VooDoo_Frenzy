using TMPro;
using UnityEngine;

public class RoundTimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    private RoundManager roundManager;

    void Start()
    {
        roundManager = FindObjectOfType<RoundManager>();
    }

    void Update()
    {
        if (roundManager == null || !roundManager.isClient) return;

        float time = roundManager.GetTimeRemaining();
        timerText.text = $"{Mathf.CeilToInt(time)}s";
    }
}
