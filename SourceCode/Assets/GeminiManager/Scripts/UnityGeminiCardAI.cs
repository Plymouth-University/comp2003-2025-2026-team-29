using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[Serializable]
public class GeminiRules
{
    public List<string> rules;
}

[Serializable]
public class GeminiRequest
{
    public string gameId;
    public string instruction;
    public GeminiRules rules;
    public List<string> playerHand;
    public string discardTop;
    public List<string> stack; // Hidden from AI
}

[Serializable]
public class GeminiResponse
{
    public string action;
    public string discardReturn;
    public List<string> updatedHand;
}

public class UnityGeminiCardAI : MonoBehaviour
{
    public GeminiResponse latestResponse { get; private set; }

    [Header("Cloudflare Worker Proxy (xAI)")]
    public string workerUrl = "https://grok-proxy.myname.workers.dev";
    public string appSharedSecret = "";
    public string grokModel = "grok-2-latest";

    [Header("Debug UI (optional)")]
    public TMP_Text uiText;
    public TMP_Text debugConsole;

    private readonly StringBuilder debugLog = new StringBuilder();

    private void Log(string msg)
    {
        debugLog.AppendLine(msg);
        if (debugConsole != null) debugConsole.text = debugLog.ToString();
        Debug.Log(msg);
    }

    [Serializable]
    private class WorkerRequest
    {
        public string model;
        public string prompt;
    }

    public void SendToGemini(GeminiRequest req)
    {
        // Hide stack values
        if (req.stack != null)
        {
            List<string> hidden = new List<string>(req.stack.Count);
            foreach (var _ in req.stack) hidden.Add("unknown");
            req.stack = hidden;
        }

        string prompt = BuildPrompt(req);
        StartCoroutine(SendWorkerRequest(prompt));
    }

    private string BuildPrompt(GeminiRequest req)
    {
        // Keep this lean for speed
        return "Return ONLY JSON with keys action, discardReturn, updatedHand. No markdown. "
             + JsonUtility.ToJson(req);
    }

    private IEnumerator SendWorkerRequest(string prompt)
    {
        if (string.IsNullOrWhiteSpace(workerUrl))
        {
            Log("Worker URL missing.");
            if (uiText != null) uiText.text = "Worker URL missing.";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(appSharedSecret))
        {
            Log("App Shared Secret missing.");
            if (uiText != null) uiText.text = "Secret missing.";
            yield break;
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            Log("Prompt was empty.");
            if (uiText != null) uiText.text = "Prompt empty.";
            yield break;
        }

        WorkerRequest payload = new WorkerRequest
        {
            model = string.IsNullOrWhiteSpace(grokModel) ? "grok-2-latest" : grokModel.Trim(),
            prompt = prompt
        };

        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest w = new UnityWebRequest(workerUrl.Trim(), "POST"))
        {
            w.uploadHandler = new UploadHandlerRaw(body);
            w.downloadHandler = new DownloadHandlerBuffer();
            w.timeout = 15;

            w.SetRequestHeader("Content-Type", "application/json");
            w.SetRequestHeader("x-app-secret", appSharedSecret.Trim());

            yield return w.SendWebRequest();

            if (w.result != UnityWebRequest.Result.Success)
            {
                Log("Worker HTTP Error: " + w.error);
                Log("Body: " + w.downloadHandler.text);
                if (uiText != null) uiText.text = "Worker error.";
                yield break;
            }

            string resp = w.downloadHandler.text;
            Log("Worker OK (bytes=" + resp.Length + ")");

            GeminiResponse parsed = null;
            try
            {
                parsed = JsonUtility.FromJson<GeminiResponse>(resp);
            }
            catch (Exception ex)
            {
                Log("Parse error: " + ex.Message);
            }

            if (parsed == null || string.IsNullOrEmpty(parsed.action))
            {
                Log("Invalid JSON response: " + resp);
                if (uiText != null) uiText.text = "Invalid AI response.";
                yield break;
            }

            latestResponse = parsed;
        }
    }

    public void ResetLatestResponse()
    {
        latestResponse = null;
    }
}