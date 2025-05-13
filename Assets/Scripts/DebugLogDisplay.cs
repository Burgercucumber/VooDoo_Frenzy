using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Develop proposes???

public class DebugLogDisplay : MonoBehaviour
{
    public string output = "";
    public string stack = "";

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void HandleLog(string logString, string stackTrance, LogType type)
    {
        output = logString;
        stack= stackTrance;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(150, 5, 800, 60), output);
        GUI.Label(new Rect(150, 65, 800, 60), stack);
    }
}
