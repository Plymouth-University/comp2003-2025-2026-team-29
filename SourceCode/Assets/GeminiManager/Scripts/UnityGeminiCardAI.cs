/* 10-12-2025 Code with debug */

// Unity Gemini Card AI - Clean Full Version
using System;
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
    public string action;          // "takeDiscard" or "takeStack"
    public string discardReturn;   // the card placed onto discard pile AFTER action
    public List<string> updatedHand; // player hand AFTER the go
}

public class UnityGeminiCardAI : MonoBehaviour
{

    public GeminiResponse latestResponse { get; private set; }

    // -------------------- API KEY FROM JSON FILE --------------------
    public TextAsset jsonApi; // Assign JSON_KEY_TEMPLATE.json here

    [Serializable]
    private class ApiKeyWrapper { public string key; }

    private string apiKey;

    void Awake()
    {
        if (jsonApi != null)
            apiKey = JsonUtility.FromJson<ApiKeyWrapper>(jsonApi.text).key; 
        else
            Debug.LogError("API key JSON file missing. Assign jsonApi in Inspector.");
    }

    // -------------------- UI --------------------
    public TMP_Text uiText;
    public TMP_Text debugConsole;
    private StringBuilder debugLog = new StringBuilder();

    private void Log(string msg)
    {
        debugLog.AppendLine(msg);
        if (debugConsole != null)
            debugConsole.text = debugLog.ToString();
        Debug.Log(msg);
    }

    // -------------------- TEST CALL --------------------
    public void CallGemini()
    {
        
        Log("Button clicked — sending request to Gemini...");

        GeminiRequest req = new GeminiRequest
        {
            gameId = "GAME-001",
            instruction = "You are a player in a card game."+
                          "The gameId is an identifier for the game." +
                          "Using the rules listed in 'rules', and the cards in your hand, denoted by"+
                          "'playerHand' and the card shown on the discard pile, denoted as 'discardTop',"+
                          "you need to take your go and return the details.",
            rules = new GeminiRules
            {
                rules = new List<string> {
                    "Highest card wins",
                    "Jokers are wild",
                    "Do not use value of stack card in decision",
                    "Card can be taken from discard pile or first card from the stack, not from both",
                    "A card must be discarded from your hand to end your go.",
                    "The discarded card cannot be the one just selected in this turn."
                }
            },
            playerHand = new List<string> { "5H", "9C", "JD", "3H", "2S", "4D" },
            discardTop = "7S",
            stack = new List<string> { "3D", "4S", "JH", "QD" }
        };

        SendToGemini(req);
    }

    // -------------------- SEND REQUEST --------------------
    public void SendToGemini(GeminiRequest req)
    {
        // Enforce rule: stack values must be hidden
        List<string> hiddenStack = new List<string>();
        foreach (var _ in req.stack) hiddenStack.Add("unknown");
        req.stack = hiddenStack;

        string prompt = BuildPrompt(req);
        StartCoroutine(SendGeminiRequest(prompt));
    }

    private string BuildPrompt(GeminiRequest req)
    {
        return "You are an AI card decision engine. Return ONLY JSON with fields 'action', 'discardReturn' and 'updatedHand." +
               JsonUtility.ToJson(req);
    }

    private IEnumerator<UnityWebRequestAsyncOperation> SendGeminiRequest(string prompt)
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + apiKey;

        string sendJson = $"{{ \"contents\": [ {{ \"parts\": [ {{ \"text\": \"{EscapeJson(prompt)}\" }} ] }} ] }}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(sendJson);

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Log("HTTP Error: " + req.error);
            uiText.text = "Error contacting Gemini";
            yield break;
        }

        HandleGeminiResponse(req.downloadHandler.text);
    }

    // -------------------- HANDLE RESPONSE --------------------
    private void HandleGeminiResponse(string apiResponseJson)
    {
        Log("RAW RESPONSE:\n" + apiResponseJson);

        // 1) Try to extract the model 'text' field directly from the JSON wrapper by scanning for "text": "...."
        string modelText = null;
        string needle = "\"text\"";
        int idx = apiResponseJson.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            // find the colon after "text"
            int colon = apiResponseJson.IndexOf(':', idx);
            if (colon >= 0)
            {
                // find the first double quote that starts the string value
                int quoteStart = apiResponseJson.IndexOf('"', colon);
                // If quoteStart points to the quote that ends the property name, find the next quote
                // Move to the first quote that starts the value (skip whitespace and possible ':' and spaces)
                while (quoteStart >= 0 && (quoteStart <= colon || apiResponseJson[quoteStart - 1] == '\\'))
                {
                    quoteStart = apiResponseJson.IndexOf('"', quoteStart + 1);
                }

                if (quoteStart >= 0)
                {
                    // Now find the matching closing quote, handling escaped quotes (\")
                    int i = quoteStart + 1;
                    StringBuilder sb = new StringBuilder();
                    bool closed = false;
                    while (i < apiResponseJson.Length)
                    {
                        char c = apiResponseJson[i];
                        if (c == '\\' && i + 1 < apiResponseJson.Length)
                        {
                            // take escape sequence as literal (\" , \\ , \n etc.)
                            char next = apiResponseJson[i + 1];
                            if (next == 'n') sb.Append('\n');
                            else if (next == 'r') sb.Append('\r');
                            else if (next == 't') sb.Append('\t');
                            else sb.Append(next); // \" \\ or others
                            i += 2;
                            continue;
                        }
                        if (c == '"')
                        {
                            closed = true;
                            break;
                        }
                        sb.Append(c);
                        i++;
                    }
                    if (closed)
                        modelText = sb.ToString().Trim();
                }
            }
        }

        // 2) Fallback: if we didn't find a modelText via "text": ..., try to extract the first { ... } block from the full response
        if (string.IsNullOrEmpty(modelText))
        {
            int s = apiResponseJson.IndexOf('{');
            int e = apiResponseJson.LastIndexOf('}');
            if (s >= 0 && e > s)
            {
                modelText = apiResponseJson.Substring(s, e - s + 1);
                Log("Fallback extracted JSON from raw response.");
            }
            else
            {
                Log("No model text and no JSON block found in response.");
                if (uiText != null) uiText.text = "Malformed Gemini response.";
                return;
            }
        }
        else
        {
            Log("Extracted model text from wrapper:\n" + modelText);
        }

        // 3) Remove Markdown fences like ```json and ```
        string cleaned = modelText.Replace("```json", "")
                                  .Replace("```", "")
                                  .Trim();

        // 4) Find the JSON object within the cleaned text
        int start = cleaned.IndexOf('{');
        int end = cleaned.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            Log("Could not find JSON object boundaries after cleaning. Cleaned text:\n" + cleaned);
            if (uiText != null) uiText.text = "Could not parse model output.";
            return;
        }

        string innerJson = cleaned.Substring(start, end - start + 1);
        Log("CLEAN JSON TO PARSE:\n" + innerJson);

        // 5) Parse into your response type. (Make sure your class has fields action & discardReturn)
        GeminiResponse parsed = null;
        try
        {
            parsed = JsonUtility.FromJson<GeminiResponse>(innerJson);
            latestResponse = parsed; // store latest response for game code
        }
        catch (Exception ex)
        {
            Log("JsonUtility.FromJson failed: " + ex.Message);
        }

        if (parsed == null)
        {
            Log("Parsed object is null. JSON was:\n" + innerJson);
            if (uiText != null) uiText.text = "Invalid model response.";
            return;
        }

        if (string.IsNullOrEmpty(parsed.action))
        {
            Log("Parsed response missing 'action'. JSON:\n" + innerJson);
            if (uiText != null) uiText.text = "Invalid model response (no action).";
            return;
        }

        // 6) Display concise result and log full details
        string handList = (parsed.updatedHand != null)
            ? string.Join(", ", parsed.updatedHand)
            : "(no data)";

        string uiOut =
            $"Action: {parsed.action}\n" +
            $"Return Card: {parsed.discardReturn}\n" +
            $"Updated Hand: {handList}";


        // string uiOut = $"Action: {parsed.action}\nReturn Card: {parsed.discardReturn}";


        if (uiText != null) uiText.text = uiOut;
        Log("Parsed game response: " + uiOut);
    }

    public void ResetLatestResponse()
    {
        latestResponse = null;
    }


    // -------------------- JSON ESCAPER --------------------


    private string EscapeJson(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n");
    }
}


/* ----------------------------------------------------------------------------------------- */


/* 08-12-25 code  - start  */

/* 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// ---------------------------------------------
// GAME STRUCTURES
// ---------------------------------------------

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
    public List<string> stack; // Will be masked ("unknown")
}

[Serializable]
public class GeminiGameResponse
{
    public string action; // "take_discard" or "take_stack"
    public string card;   // card placed back onto discard pile
}

// ---------------------------------------------
// GEMINI REQUEST PAYLOAD STRUCTURES (clean JSON)
// ---------------------------------------------

[Serializable]
public class GeminiPart
{
    public string text;
}

[Serializable]
public class GeminiContent
{
    public GeminiPart[] parts;
}

[Serializable]
public class GeminiRequestWrapper
{
    public GeminiContent[] contents;
}

// ---------------------------------------------
// GEMINI RESPONSE STRUCTURES
// ---------------------------------------------

[Serializable]
public class GeminiCandidateText
{
    public GeminiContent content;
}

[Serializable]
public class GeminiTextResponse
{
    public GeminiCandidateText[] candidates;
}

// ---------------------------------------------
// MAIN CLASS
// ---------------------------------------------

public class UnityGeminiCardAI : MonoBehaviour
{
    public string apiKey = "";      // Set in Inspector
    public TMP_Text uiText;         // UI Text output field

    private const string GEMINI_URL =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=";

    void Awake()
    {
        // No file loading anymore — everything is internal
    }

    // -------------------------------------------------
    // TEST BUTTON — appears in Inspector
    // -------------------------------------------------
    [ContextMenu("Run Test Move")]
    public void RunTestMove()
    {
        GeminiRequest request = new GeminiRequest
        {
            gameId = "game_001",
            instruction = "take your go",
            rules = new GeminiRules
            {
                rules = new List<string> {
                    "Highest card wins",
                    "Jokers are wild",
                    "Do not use the value of the stack card in the decision"
                }
            },
            playerHand = new List<string> { "5H", "9C", "JD" },
            discardTop = "7S",
            stack = new List<string> { "KH", "3C", "JD" } // WILL BE MASKED
        };

        SendToGemini(request);
    }

    // -------------------------------------------------
    // Build sanitized prompt
    // -------------------------------------------------

    private string BuildPrompt(GeminiRequest request)
    {

        // Mask stack values BEFORE sending to the model
        List<string> maskedStack = new List<string>();
        foreach (var _ in request.stack)
            maskedStack.Add("unknown");
        request.stack = maskedStack;

        return
            "You are an AI deciding the player's next move in a card game. " +
            "Return ONLY a pure JSON object with fields 'action' and 'card'.\n" +
            "Do not explain. Do not add text.\n\n" +
            JsonUtility.ToJson(request);
    }

    // -------------------------------------------------
    // Send to Gemini
    // -------------------------------------------------

    public void SendToGemini(GeminiRequest request)
    {

        string prompt = BuildPrompt(request);

        GeminiRequestWrapper wrapper = new GeminiRequestWrapper
        {
            contents = new[] {
                new GeminiContent {
                    parts = new[] {
                        new GeminiPart { text = prompt }
                    }
                }
            }
        };

        string jsonPayload = JsonUtility.ToJson(wrapper);

        StartCoroutine(SendGeminiRequest(jsonPayload));
    }

    // -------------------------------------------------
    // HTTP REQUEST (clean)
    // -------------------------------------------------

    private IEnumerator SendGeminiRequest(string jsonPayload)
    {

        string url = GEMINI_URL + apiKey;

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            uiText.text = "Gemini Error:\n" + req.error;
            yield break;
        }

        HandleGeminiResponse(req.downloadHandler.text);
    }

    // -------------------------------------------------
    // RESPONSE HANDLER
    // -------------------------------------------------

    private void HandleGeminiResponse(string json)
    {

        GeminiTextResponse response =
            JsonUtility.FromJson<GeminiTextResponse>(json);

        if (response == null ||
            response.candidates == null ||
            response.candidates.Length == 0)
        {

            uiText.text = "Malformed Gemini response:\n" + json;
            return;
        }

        string modelText = response.candidates[0]
            .content.parts[0].text;

        // Extract only the JSON inside model text
        int start = modelText.IndexOf('{');
        int end = modelText.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            uiText.text = "Could not extract JSON:\n" + modelText;
            return;
        }

        string extracted = modelText.Substring(start, end - start + 1);

        GeminiGameResponse gameResponse =
            JsonUtility.FromJson<GeminiGameResponse>(extracted);

        uiText.text =
            $"Action: {gameResponse.action}\n" +
            $"Card: {gameResponse.card}";
    }
}

*/

/* new code - end  */

/* ----------------------------------------------------------------------------------------- */




/* Original demo code. Left in for reference.
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using TMPro;
using System.IO; 
using System;

[System.Serializable]
public class UnityAndGeminiKey
{
    public string key;
}



[System.Serializable]
public class InlineData
{
    public string mimeType;
    public string data;
}

// Text-only part
[System.Serializable]
public class TextPart
{
    public string text;
}

// Image-capable part
[System.Serializable]
public class ImagePart
{
    public string text;
    public InlineData inlineData;
}

[System.Serializable]
public class TextContent
{
    public string role;
    public TextPart[] parts;
}

[System.Serializable]
public class TextCandidate
{
    public TextContent content;
}

[System.Serializable]
public class TextResponse
{
    public TextCandidate[] candidates;
}

[System.Serializable]
public class ImageContent
{
    public string role;
    public ImagePart[] parts;
}

[System.Serializable]
public class ImageCandidate
{
    public ImageContent content;
}

[System.Serializable]
public class ImageResponse
{
    public ImageCandidate[] candidates;
}


// For text requests
[System.Serializable]
public class ChatRequest
{
    public TextContent[] contents;
    public TextContent system_instruction;
}


public class UnityAndGeminiV3: MonoBehaviour
{
    [Header("JSON API Configuration")]
    public TextAsset jsonApi;

    
    private string apiKey = ""; 
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent"; // Edit it and choose your prefer model
    private string imageEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent"; //End point for image generation

    [Header("ChatBot Function")]
    public TMP_InputField inputField;
    public TMP_Text uiText;
    public string botInstructions;
    private TextContent[] chatHistory;


    [Header("Prompt Function")]
    public string prompt = "";

    // Image Generation is now a paid feature. The generation of images can produce charges at your credit card.
    // [Header("Image Prompt Function")]
    // public string imagePrompt = "";
    // public Material skyboxMaterial; 

    [Header("Media Prompt Function")]
    // Receives files with a maximum of 20 MB
    public string mediaFilePath = "";
    public string mediaPrompt = "";
    public enum MediaType
    {
        Video_MP4 = 0,
        Audio_MP3 = 1,
        PDF = 2,
        JPG = 3,
        PNG = 4
    }
    public MediaType mimeType = MediaType.Video_MP4;
    

    public string GetMimeTypeString()
    {
        switch (mimeType)
        {
            case MediaType.Video_MP4:
                return "video/mp4";
            case MediaType.Audio_MP3:
                return "audio/mp3";
            case MediaType.PDF:
                return "application/pdf";
            case MediaType.JPG:
                return "image/jpeg";
            case MediaType.PNG:
                return "image/png";
            default:
                return "error";
        }
    }


    void Start()
    {
        UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        apiKey = jsonApiKey.key;   
        chatHistory = new TextContent[] { };
        if (prompt != ""){StartCoroutine( SendPromptRequestToGemini(prompt));};

        // Image Generation is now a paid feature. The generation of images can produce charges at your credit card.
        // if (imagePrompt != ""){StartCoroutine( SendPromptRequestToGeminiImageGenerator(imagePrompt));};

        if (mediaPrompt != "" && mediaFilePath != ""){StartCoroutine(SendPromptMediaRequestToGemini(mediaPrompt, mediaFilePath));};
    }

    private IEnumerator SendPromptRequestToGemini(string promptText)
    {
        string url = $"{apiEndpoint}?key={apiKey}";
     
        string jsonData = "{\"contents\": [{\"parts\": [{\"text\": \"{" + promptText + "}\"}]}]}";

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Create a UnityWebRequest with the JSON data
        using (UnityWebRequest www = new UnityWebRequest(url, "POST")){
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            } else {
                Debug.Log("Request complete!");
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                    {
                        //This is the response to your request
                        string text = response.candidates[0].content.parts[0].text;
                        Debug.Log(text);
                    }
                else
                {
                    Debug.Log("No text found.");
                }
            }
        }
    }

    public void SendChat()
    {
        string userMessage = inputField.text;
        StartCoroutine( SendChatRequestToGemini(userMessage));
    }

    private IEnumerator SendChatRequestToGemini(string newMessage)
    {

        string url = $"{apiEndpoint}?key={apiKey}";
     
        TextContent userContent = new TextContent
        {
            role = "user",
            parts = new TextPart[]
            {
                new TextPart { text = newMessage }
            }
        };

        TextContent instruction = new TextContent
        {
            parts = new TextPart[]
            {
                new TextPart {text = botInstructions}
            }
        }; 

        List<TextContent> contentsList = new List<TextContent>(chatHistory);
        contentsList.Add(userContent);
        chatHistory = contentsList.ToArray(); 

        ChatRequest chatRequest = new ChatRequest { contents = chatHistory, system_instruction = instruction };

        string jsonData = JsonUtility.ToJson(chatRequest);

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Create a UnityWebRequest with the JSON data
        using (UnityWebRequest www = new UnityWebRequest(url, "POST")){
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            } else {
                Debug.Log("Request complete!");
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                    {
                        //This is the response to your request
                        string reply = response.candidates[0].content.parts[0].text;
                        TextContent botContent = new TextContent
                        {
                            role = "model",
                            parts = new TextPart[]
                            {
                                new TextPart { text = reply }
                            }
                        };

                        Debug.Log(reply);
                        //This part shows the text in the Canvas
                        uiText.text = reply;
                        //This part adds the response to the chat history, for your next message
                        contentsList.Add(botContent);
                        chatHistory = contentsList.ToArray();
                    }
                else
                {
                    Debug.Log("No text found.");
                }
             }
        }  
    }

    // Image Generation is now a paid feature. The generation of images can produce charges at your credit card.
    
    // private IEnumerator SendPromptRequestToGeminiImageGenerator(string promptText)
    // {
    //     string url = $"{imageEndpoint}?key={apiKey}";
        
    //     // Create the proper JSON structure with model specification
    //     string jsonData = $@"{{
    //         ""contents"": [{{
    //             ""parts"": [{{
    //                 ""text"": ""{promptText}""
    //             }}]
    //         }}],
    //         ""generationConfig"": {{
    //             ""responseModalities"": [""Text"", ""Image""]
    //         }}
    //     }}";

    //     byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

    //     // Create a UnityWebRequest with the JSON data
    //     using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
    //     {
    //         www.uploadHandler = new UploadHandlerRaw(jsonToSend);
    //         www.downloadHandler = new DownloadHandlerBuffer();
    //         www.SetRequestHeader("Content-Type", "application/json");

    //         yield return www.SendWebRequest();

    //         if (www.result != UnityWebRequest.Result.Success) 
    //         {
    //             Debug.LogError(www.error);
    //         } 
    //         else 
    //         {
    //             Debug.Log("Request complete!");
    //             Debug.Log("Full response: " + www.downloadHandler.text); // Log full response for debugging
                
    //             // Parse the JSON response
    //             try 
    //             {
    //                 ImageResponse response = JsonUtility.FromJson<ImageResponse>(www.downloadHandler.text);
                    
    //                 if (response.candidates != null && response.candidates.Length > 0 && 
    //                     response.candidates[0].content != null && 
    //                     response.candidates[0].content.parts != null)
    //                 {
    //                     foreach (var part in response.candidates[0].content.parts)
    //                     {
    //                         if (!string.IsNullOrEmpty(part.text))
    //                         {
    //                             Debug.Log("Text response: " + part.text);
    //                         }
    //                         else if (part.inlineData != null && !string.IsNullOrEmpty(part.inlineData.data))
    //                         {
    //                             // This is the base64 encoded image data
    //                             byte[] imageBytes = System.Convert.FromBase64String(part.inlineData.data);
                                
    //                             // Create a texture from the bytes
    //                             Texture2D tex = new Texture2D(2, 2);
    //                             tex.LoadImage(imageBytes);
    //                             byte[] pngBytes = tex.EncodeToPNG();
    //                             string path = Application.persistentDataPath + "/gemini-image.png";
    //                             File.WriteAllBytes(path, pngBytes);
    //                             Debug.Log("Saved to: " + path);
    //                             Debug.Log("Image received successfully!");

    //                             // Load the saved image back as Texture2D
    //                             string imagePath = Path.Combine(Application.persistentDataPath, "gemini-image.png");
                                
    //                             Texture2D panoramaTex = new Texture2D(2, 2);
    //                             panoramaTex.LoadImage(File.ReadAllBytes(imagePath));

    //                             Texture2D properlySizedTex = ResizeTexture(panoramaTex, 1024, 512);
                                
    //                             // Apply to a panoramic skybox material
    //                             if (skyboxMaterial != null)
    //                             {
    //                                 // Switch to panoramic shader
    //                                 skyboxMaterial.shader = Shader.Find("Skybox/Panoramic");
    //                                 skyboxMaterial.SetTexture("_MainTex", properlySizedTex);
    //                                 DynamicGI.UpdateEnvironment();
    //                                 Debug.Log("Skybox updated with panoramic image!");
    //                             }
    //                             else
    //                             {
    //                                 Debug.LogError("Skybox material not assigned!");
    //                             }

    //                             // Another approach but might cause distorsion

                                
    //                             // Texture2D savedTex = new Texture2D(2, 2);
    //                             // savedTex.LoadImage(File.ReadAllBytes(path));

    //                             // // Convert to Cubemap (simplified approach - may distort)
    //                             // Cubemap newCubemap = new Cubemap(savedTex.width, TextureFormat.RGBA32, false);
    //                             // for (int i = 0; i < 6; i++)
    //                             // {
    //                             //     newCubemap.SetPixels(savedTex.GetPixels(), (CubemapFace)i);
    //                             // }
    //                             // newCubemap.Apply();

    //                             // // Apply to skybox
    //                             // if (skyboxMaterial != null)
    //                             // {
    //                             //     skyboxMaterial.SetTexture("_Tex", newCubemap);
    //                             //     DynamicGI.UpdateEnvironment();
    //                             //     Debug.Log("Skybox updated with new image!");
    //                             // }                            

    //                         }
    //                     }
    //                 }
    //                 else
    //                 {
    //                     Debug.Log("No valid response parts found.");
    //                 }
    //             }
    //             catch (Exception e)
    //             {
    //                 Debug.LogError("JSON Parse Error: " + e.Message);
    //             }
    //         }
    //     }
    // }

    // Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    // {
    //     RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
    //     Graphics.Blit(source, rt);
    //     Texture2D result = new Texture2D(newWidth, newHeight);
    //     RenderTexture.active = rt;
    //     result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
    //     result.Apply();
    //     RenderTexture.ReleaseTemporary(rt);
    //     return result;
    // }

    private IEnumerator SendPromptMediaRequestToGemini(string promptText, string mediaPath)
    {
        // Read video file and convert to base64
        byte[] mediaBytes = File.ReadAllBytes(mediaPath);
        string base64Media = System.Convert.ToBase64String(mediaBytes);

        string url = $"{apiEndpoint}?key={apiKey}";

        string mimeTypeMedia = GetMimeTypeString();



        string jsonBody = $@"
        {{
        ""contents"": [
            {{
            ""parts"": [
                {{
                ""text"": ""{promptText}""
                }},
                {{
                ""inline_data"": {{
                    ""mime_type"": ""{mimeTypeMedia}"",
                    ""data"": ""{base64Media}""
                }}
                }}
            ]
            }}
        ]
        }}";


        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonBody);


        // Create and send the request
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) 
            {
                Debug.LogError(www.error);
                Debug.LogError("Response: " + www.downloadHandler.text);
            } 
            else 
            {
                Debug.Log("Request complete!");
                TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                {
                    string text = response.candidates[0].content.parts[0].text;
                    Debug.Log(text);
                }
                else
                {
                    Debug.Log("No text found.");
                }
            }
        }
    }

}


*/
