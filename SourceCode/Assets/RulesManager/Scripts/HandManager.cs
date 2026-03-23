using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HandManager;

public class HandManager : MonoBehaviour
{
    // ---- UI ----
    public GameObject cardPrefab;
    public Transform cardParent;
    public Button endTurnButton;
    public GameObject jokerPanel;
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI playerPoints;
    public TextMeshProUGUI AIPoints;
    public Image discardTopCardImage;
    public Texture2D[] cardTextures;
    public Dictionary<string, Texture2D> cardTextureDict = new Dictionary<string, Texture2D>();
    [Header("Card Art")]
    public Texture2D cardBackTexture;
    public GameObject cardVisualPrefab;
    public Transform playAreaParent;
    public Animator deckShuffler;
    public Animator cardShuffler;


    // ---- Game Logic ----
    public Deck deck;
    public List<Card> playerHand = new List<Card>();
    public List<Card> AIHand = new List<Card>();
    private List<int> selectedCards = new List<int>();
    private List<Card> playedCards = new List<Card>();
    private Dictionary<Card, int> jokerValues = new Dictionary<Card, int>();
    private int currentJokerIndex = -1;
    private int turn = 0;
    private bool isAITurn = false;
    private bool gameEnd = false;

    // ---- Rules & Scoring ----
    public GameRulesSO gameRules;
    private int totalPoints = 0;
    private int totalAIPoints = 0;
    public int pointEndLimit = 0;
    public int ruleStartHand = 5;           //Rule 1
    public int ruleDraw = 0;                //Rule 2
    public int ruleMaxHand = 0;             //Rule 3
    public bool rulePointsEnabled = false;  //Rule 4
    public bool rulePointsEnd = false;      //Rule 5
    public bool rulePointsWin = false;      //Rule 6
    public bool ruleReshuffle = false;      //Rule 7
    public bool ruleJoker = false;          //Rule 8
    public bool rulesCard = false;          //Rule 9
    public bool ruleDrawHand = false;       //Rule 10
    public int ruleTurnLimit = 0;           //Rule 11
    public bool ruleDeckout = false;        //Rule 12
    public bool ruleOutofCards = false;     //Rule 13
    public bool ruleLeastCardsWin = false;  //Rule 14
    public int rulePlayAmount = 0;          //Rule 15
    public List<Rule> rules;                //List of Rules

    // ---- AI ----
    public UnityGeminiCardAI geminiAI;
    [Header("Turn Messages")]
    public TMP_Text turnMessageText;

    private string currentGameId;
    private const int aiMaxAttempts = 3;
    private const float aiResponseTimeoutSeconds = 15f;
    private const float aiRetryDelaySeconds = 0.75f;

    void Start()
    {
        // Use the values from the ScriptableObject
        ruleJoker = gameRules.ruleJoker;
        rulesCard = gameRules.rulesCard;
        ruleReshuffle = gameRules.ruleReshuffle;
        ruleStartHand = gameRules.ruleStartHand;
        ruleDraw = gameRules.ruleDraw;
        ruleMaxHand = gameRules.ruleMaxHand;
        rulePointsEnabled = gameRules.rulePointsEnabled;
        rulePointsEnd = gameRules.rulePointsEnd;
        rulePointsWin = gameRules.rulePointsWin;
        pointEndLimit = gameRules.pointEndLimit;
        ruleDrawHand = gameRules.ruleDrawHand;
        ruleTurnLimit = gameRules.ruleTurnLimit;
        ruleDeckout = gameRules.ruleDeckout;
        ruleOutofCards = gameRules.ruleOutofCards;
        ruleLeastCardsWin = gameRules.ruleLeastCardsWin;
        rulePlayAmount = gameRules.rulePlayAmount;
        currentGameId = CreateUniqueGameId();

        rules = new List<Rule> {
            new Rule { Name = "Starting hand size", Enabled = ruleStartHand != 5, OnEnable = () =>
                {
                    ruleStartHand = UnityEngine.Random.Range(1, 10); // random 110 cards
                    Debug.Log($"Starting hand size set to {ruleStartHand}");
                }
            },
            new Rule
            {
                Name = "Draw each turn",
                Enabled = ruleDraw > 0,
                OnEnable = () =>
                {
                    ruleDraw = UnityEngine.Random.Range(1, 5); // set draw to random 15
                    Debug.Log($"Draw each turn set to {ruleDraw}");
                }
            },
            new Rule { Name = "Points enabled", Enabled = rulePointsEnabled, OnEnable = () => rulePointsEnabled = true },
            new Rule { Name = "Most points win", Enabled = rulePointsWin, RequiresNames = new List<string> { "Points enabled" }, OnEnable = () => rulePointsWin = true },
            new Rule {
                Name = "Game ends on points",
                Enabled = rulePointsEnd,
                RequiresNames = new List<string> { "Points enabled" },
                OnEnable = () => {
                    rulePointsEnd = true;
                    pointEndLimit = UnityEngine.Random.Range(50, 300); // set point end to random 50-300
                    Debug.Log($"Points to reach set to {pointEndLimit}");
                }
            },
            new Rule
            {
                Name = "Max hand size",
                Enabled = ruleMaxHand > 0,
                OnEnable = () =>
                {
                    ruleMaxHand = UnityEngine.Random.Range(1, 8); // set max to random 18
                    Debug.Log($"Max hand size set to {ruleMaxHand}");
                }
            },
            new Rule { Name = "Reshuffle deck when empty", Enabled = ruleReshuffle, OnEnable = () => ruleReshuffle = true },
            new Rule { Name = "Jokers enabled", Enabled = ruleJoker, OnEnable = () => { ruleJoker = true; deck.AddJoker(); deck.Shuffle(); } },
            new Rule { Name = "Rules Card enabled", Enabled = rulesCard, OnEnable = () => { rulesCard = true; deck.AddRules(); deck.Shuffle(); } },
            new Rule { Name = "Draw up to hand enabled", Enabled = ruleDrawHand, OnEnable = () => ruleDrawHand = true },
            new Rule
            {
                Name = "Turn limit",
                Enabled = ruleTurnLimit > 0,
                OnEnable = () =>
                {
                    ruleTurnLimit = UnityEngine.Random.Range(turn + 3, turn + 8); // set limit to random 38 from current turn
                    Debug.Log($"Turn limit set to turn {ruleTurnLimit}");
                }
            },
            new Rule { Name = "End game on deckout", Enabled = ruleDeckout, OnEnable = () => ruleDeckout = true },
            new Rule { Name = "End game when out of cards", Enabled = ruleOutofCards, OnEnable = () => ruleOutofCards = true },
            new Rule { Name = "Win game with least cards in hand", Enabled = ruleLeastCardsWin, OnEnable = () => ruleLeastCardsWin = true },
            new Rule
            {
                Name = "Card play amount",
                Enabled = rulePlayAmount > 0,
                OnEnable = () =>
                {
                    rulePlayAmount = UnityEngine.Random.Range(1, 3); // set amount to random 13
                    Debug.Log($"Card play max set to {rulePlayAmount}");
                }
            }
        };
        deck = new Deck();
        if (ruleJoker) deck.AddJoker();
        if (rulesCard) deck.AddRules();
        deck.Shuffle();

        LoadCardTextures();

        // Draw starting hand
        DrawCards(ruleStartHand);
        DrawAICards(ruleStartHand);

        UpdateHandUI();

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnButtonPressed);

        if (jokerPanel != null)
            jokerPanel.SetActive(false); // hide panel initially
        if (winPanel != null)
            winPanel.SetActive(false);
        if (losePanel != null)
            losePanel.SetActive(false);
        if (!rulePointsEnabled)
        {
            playerPoints.text = "";
            AIPoints.text = "";
        }
        else
        {
            playerPoints.text = "Player: 0 points";
            AIPoints.text = "AI: 0 points";
        }

        StartCoroutine(PrimeAIOnStartup());
    }

    // ---- UI Updates ----
    public void UpdateHandUI()
    {
        foreach (Transform child in cardParent)
            Destroy(child.gameObject);

        float spacing = 0f;
        float startX = 0;
        float AIspacing = 0f;
        float AIstartX = 0;

        if (playerHand.Count > 1)
        {
            spacing = 550f / (playerHand.Count - 1);
            startX = -((playerHand.Count - 1) * spacing) / 2;
        }
        else
        {
            spacing = 0f;
            startX = 0;
        }
        if (AIHand.Count > 1)
        {
            AIspacing = 550f / (AIHand.Count - 1);
            AIstartX = -((AIHand.Count - 1) * AIspacing) / 2;
        }
        else
        {
            AIspacing = 0f;
            AIstartX = 0;
        }
        for (int i = 0; i < playerHand.Count; i++)
        {
            GameObject cardButton = Instantiate(cardPrefab, cardParent);
            string cardKey = GetCardKey(playerHand[i]);
            if (cardTextureDict.TryGetValue(cardKey, out Texture2D tex))
            {
                // --- Step 3: convert the texture to a Sprite and apply to the button ---
                Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                cardButton.GetComponent<Image>().sprite = s; // assign sprite to button
            }

            TMP_Text tmpText = cardButton.GetComponentInChildren<TMP_Text>();
            if (tmpText != null) tmpText.text = playerHand[i].ToString();

            RectTransform rt = cardButton.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * spacing, 60);
            rt.sizeDelta = new Vector2(120, 180);

            int index = i;
            cardButton.GetComponent<Button>().onClick.AddListener(() => OnCardClicked(index));
        }
        for (int i = 0; i < AIHand.Count; i++)
        {

            GameObject AICardButton = Instantiate(cardPrefab, cardParent);

            Image img = AICardButton.GetComponent<Image>();
            Sprite backSprite = GetCardBackSprite();

            if (img != null && backSprite != null)
                img.sprite = backSprite;

            RectTransform rt = AICardButton.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(AIstartX + i * AIspacing, 1000);
            rt.sizeDelta = new Vector2(120, 180);

            Button btn = AICardButton.GetComponent<Button>();
            btn.interactable = false;

            int AIindex = i;
        }
    }

    void OnCardClicked(int index)
    {
        RectTransform rt = cardParent.GetChild(index).GetComponent<RectTransform>();

        if (selectedCards.Contains(index))
        {
            selectedCards.Remove(index);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 60);
            rt.localScale = Vector3.one;
        }
        else
        {
            selectedCards.Add(index);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 80);
            rt.localScale = Vector3.one * 1.1f;
        }
    }

    // ---- Joker button click ----
    public void OnJokerValueSelected(int value)
    {
        if (currentJokerIndex == -1) return;
        Card jokerCard = playerHand[currentJokerIndex];
        jokerValues[jokerCard] = value;
        currentJokerIndex = -1;
        jokerPanel.SetActive(false);
        StartCoroutine(ValidateAndPlayPlayerTurn());
    }

    // ---- End button pressed ----
    void OnEndTurnButtonPressed()
    {
        if (selectedCards.Count == 0)
        {
            Debug.Log("No cards selected!");
            return;
        }
        if (rulePlayAmount > 0 && selectedCards.Count != rulePlayAmount)
        {
            Debug.Log($"You must play {rulePlayAmount} cards.");
            return;
        }

        // Check for Joker
        int jokerIdx = selectedCards.FirstOrDefault(idx =>
            playerHand[idx].Rank == Rank.Joker || playerHand[idx].Rank == Rank.Joker2);

        if (selectedCards.Any(idx => playerHand[idx].Rank == Rank.Joker || playerHand[idx].Rank == Rank.Joker2))
        {
            currentJokerIndex = jokerIdx;
            jokerPanel.SetActive(true);
            return; // Wait for Joker value
        }
        StartCoroutine(ValidateAndPlayPlayerTurn());
    }

    // ---- Playing cards ----
    void PlaySelectedCards()
    {
        StartCoroutine(PlaySelectedCardsCoroutine());
    }

    private IEnumerator PlaySelectedCardsCoroutine()
    {
        playedCards.Clear();
        selectedCards.Sort();
        selectedCards.Reverse();

        foreach (int idx in selectedCards)
        {
            Card c = playerHand[idx];
            playedCards.Add(c);
            playerHand.RemoveAt(idx);

            if (cardVisualPrefab != null && playAreaParent != null)
            {
                GameObject visualCard = Instantiate(
                    cardVisualPrefab,
                    playAreaParent.position,
                    Quaternion.identity,
                    playAreaParent
                );

                if (cardTextureDict.TryGetValue(GetCardKey(c), out Texture2D tex))
                {
                    Transform cardFront = visualCard.transform.Find("CardFront");
                    if (cardFront != null)
                    {
                        MeshRenderer mr = cardFront.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {
                            mr.material = new Material(mr.material); // clone so it doesn't overwrite shared material
                            mr.material.mainTexture = tex;
                        }
                        else
                        {
                            Debug.LogError("MeshRenderer not found on CardFront!");
                        }
                    }
                    else
                    {
                        Debug.LogError("CardFront child not found on visualCard prefab!");
                    }
                }

                visualCard.transform.localScale = Vector3.one * 0.2f;
                visualCard.transform.rotation = Quaternion.Euler(90, 0, 0);
                Animator anim = visualCard.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("playerPlay");
                }
                // Wait for a short time before spawning the next card
                yield return new WaitForSeconds(0.2f);
                Destroy(visualCard, 1.0f); // destroy after animation finishes
            }
            UpdateHandUI();
        }
        selectedCards.Clear();
        foreach (var c in playedCards)
            deck.AddToDiscard(c);

        UpdateDiscardTopCard();
        UpdateHandUI();

        if (ruleOutofCards && playerHand.Count == 0) EndGame();

        FinishTurn();
    }

    void PlayAICards(string AIIndices)
    {
        StartCoroutine(PlayAICardsoroutine(AIIndices));
    }

    private IEnumerator PlayAICardsoroutine(string AIIndices)
    {
        Debug.Log("Coroutine started");
        if (string.IsNullOrEmpty(AIIndices))
        {
            Debug.Log("AIIndices empty, exiting");
            yield break;
        }

        string[] Parts = AIIndices.Split('-');
        List<int> AISelectedCards = Parts.Select(s => int.Parse(s)).ToList();
        playedCards.Clear();
        AISelectedCards.Sort();
        AISelectedCards.Reverse();

        foreach (int idx in AISelectedCards)
        {
            Debug.Log("Processing card index: " + idx);

            if (idx >= 0 && idx < AIHand.Count)
            {
                Card c = AIHand[idx];
                playedCards.Add(c);
                AIHand.RemoveAt(idx);
                if (cardVisualPrefab != null && playAreaParent != null)
                {
                    GameObject visualCard = Instantiate(
                        cardVisualPrefab,
                        playAreaParent.position,
                        Quaternion.identity,
                        playAreaParent
                    );

                    if (cardTextureDict.TryGetValue(GetCardKey(c), out Texture2D tex))
                    {
                        Transform cardFront = visualCard.transform.Find("CardFront");
                        if (cardFront != null)
                        {
                            MeshRenderer mr = cardFront.GetComponent<MeshRenderer>();
                            if (mr != null)
                            {
                                mr.material = new Material(mr.material); // clone so it doesn't overwrite shared material
                                mr.material.mainTexture = tex;
                            }
                            else
                            {
                                Debug.LogError("MeshRenderer not found on CardFront!");
                            }
                        }
                        else
                        {
                            Debug.LogError("CardFront child not found on visualCard prefab!");
                        }
                    }

                    visualCard.transform.localScale = Vector3.one * 0.2f;
                    visualCard.transform.rotation = Quaternion.Euler(90, 0, 0);
                    Animator anim = visualCard.GetComponent<Animator>();
                    if (anim != null)
                    {
                        anim.SetTrigger("enemyPlay");
                    }
                    // Wait for a short time before spawning the next card
                    yield return new WaitForSeconds(0.5f);
                    Destroy(visualCard, 2.2f);
                }
                UpdateHandUI();
            }
        }
        foreach (var c in playedCards) deck.AddToDiscard(c);
        UpdateDiscardTopCard();
        UpdateHandUI();
        isAITurn = true;
        FinishTurn();
    }

    // ---- Ending turn ----
    void FinishTurn()
    {
        // ---- Handle Rules Cards ----
        foreach (var c in playedCards)
        {
            if (c.Rank == Rank.Rules)
            {
                var eligibleRules = rules
                    .Where(r => !r.Enabled && (r.RequiresNames == null || r.RequiresNames.All(n => rules.First(x => x.Name == n).Enabled)))
                    .ToList();

                if (eligibleRules.Count > 0)
                {
                    Rule chosen = eligibleRules[UnityEngine.Random.Range(0, eligibleRules.Count)];
                    chosen.Enable();
                }
                else
                    Debug.Log("All rules already enabled!");
            }
        }

        // ---- Handle Scoring ----
        if (rulePointsEnabled)
        {
            int turnPoints = 0;
            foreach (var c in playedCards)
            {
                int val = 0;
                if (c.Rank == Rank.Joker || c.Rank == Rank.Joker2)
                {
                    val = jokerValues.ContainsKey(c) ? jokerValues[c] : 11;
                }
                else if (c.Rank == Rank.Rules) val = 0;
                else
                {
                    val = GetCardValue(c);
                }
                turnPoints += val;
            }
            if (isAITurn)
            {
                totalAIPoints += turnPoints;
                AIPoints.text = $"AI: {totalAIPoints} points";
                Debug.Log($"Turn points: {turnPoints}, AI Total points: {totalAIPoints}");
            }
            else
            {
                totalPoints += turnPoints;
                playerPoints.text = $"Player: {totalPoints} points";
                Debug.Log($"Turn points: {turnPoints}, Total points: {totalPoints}");
            }
            UpdateDiscardTopCard();

            if (rulePointsEnd && (totalPoints >= pointEndLimit || totalAIPoints >= pointEndLimit)) EndGame();

            if (ruleTurnLimit != 0 && turn >= ruleTurnLimit - 1 && isAITurn) EndGame();
        }
        if (!isAITurn) StartCoroutine(AITurn());
        isAITurn = false;
    }

    System.Collections.IEnumerator AITurn()
    {
        yield return new WaitForSeconds(0.5f);

        if (turn != 0)
        {
            if ((ruleDrawHand) && (ruleStartHand - AIHand.Count) > 0) DrawAICards(ruleStartHand - AIHand.Count);
            DrawAICards(ruleDraw);
            yield return new WaitForSeconds(0.5f * ruleDraw);
        }

        if (gameEnd)
            yield break;

        endTurnButton.interactable = false;

        GeminiRequest req = BuildAITurnRequest();
        GeminiResponse aiMoveResponse = null;
        yield return StartCoroutine(SendAIRequestWithRetry(req, aiMaxAttempts, response => aiMoveResponse = response));

        string aiCardsToPlay = null;
        List<int> aiIndices = new List<int>();

        if (aiMoveResponse != null && TryParseCardIndices(aiMoveResponse.discardReturn, AIHand.Count, out aiIndices))
        {
            GeminiResponse aiValidationResponse = null;
            yield return StartCoroutine(ValidateMoveWithAI(false, AIHand, aiIndices, "AI", response => aiValidationResponse = response));

            if (IsValidationSuccessful(aiValidationResponse, out string validationMessage))
            {
                aiCardsToPlay = BuildIndicesString(aiIndices);
                Debug.Log("AI response accepted: " + aiCardsToPlay);
            }
            else
            {
                Debug.LogWarning("AI move failed validation: " + validationMessage);
            }
        }

        if (string.IsNullOrWhiteSpace(aiCardsToPlay))
        {
            Debug.LogWarning("AI error or invalid move. Falling back to random valid card selection.");
            aiCardsToPlay = BuildFallbackAICards();
        }

        PlayAICards(aiCardsToPlay);

        int playedCount = string.IsNullOrWhiteSpace(aiCardsToPlay) ? 0 : aiCardsToPlay.Split('-').Length;
        yield return new WaitForSeconds(0.6f * playedCount);

        if (ruleOutofCards && AIHand.Count == 0)
            EndGame();

        turn += 1;
        UpdateHandUI();

        if (turn != 0 && !gameEnd)
        {
            if ((ruleDrawHand) && (ruleStartHand - playerHand.Count) > 0) DrawCards(ruleStartHand - playerHand.Count);
            DrawCards(ruleDraw);
            selectedCards.Clear();
            endTurnButton.interactable = true;
        }
    }


    private IEnumerator PrimeAIOnStartup()
    {
        if (geminiAI == null)
        {
            Debug.LogWarning("Gemini AI reference is missing. Startup priming skipped.");
            yield break;
        }

        GeminiRequest primeRequest = new GeminiRequest
        {
            gameId = currentGameId,
            instruction = "Startup connectivity check only. Respond with JSON only. Set action to PRIME_OK, discardReturn to READY, and updatedHand to an empty array.",
            rules = new GeminiRules
            {
                rules = new List<string> { "This is a warmup request. Do not take a turn." }
            },
            playerHand = new List<string>(),
            discardTop = "",
            stack = new List<string>()
        };

        GeminiResponse primeResponse = null;
        yield return StartCoroutine(SendAIRequestWithRetry(primeRequest, 1, response => primeResponse = response));

        if (primeResponse != null)
        {
            Debug.Log("AI startup prime successful.");
        }
        else
        {
            Debug.LogWarning("AI startup prime failed. Gameplay will still continue and retry later.");
        }
    }

    private string CreateUniqueGameId()
    {
        return "Game" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
    }

    private GeminiRequest BuildAITurnRequest()
    {
        List<string> aiRules = GetActiveRulesForAI();
        aiRules.Add("IMPORTANT RESPONSE FORMAT: discardReturn may contain MULTIPLE card indexes.");
        aiRules.Add("If you want to play more than one card, return all chosen indexes separated with '-' such as 0-2 or 1-3-4.");
        aiRules.Add(rulePlayAmount > 0
            ? $"You must return exactly {rulePlayAmount} indexes in discardReturn."
            : "You may return one or more indexes in discardReturn. Prefer a valid multi-card play when that is sensible under the current rules.");
        aiRules.Add("updatedHand should list the hand after removing every played card, not just one card.");

        return new GeminiRequest
        {
            gameId = currentGameId,
            instruction = "You are a player in a card game. The gameId uniquely identifies the current match. Using the rules listed in rules, the cards in your hand in playerHand, and the top discard in discardTop, choose your move. Return ONLY JSON. For discardReturn, return the card index or indexes from your current hand, separated with '-' and starting from 0. Examples: 2 or 0-2 or 1-3-4. If multiple cards are played, include ALL played card indexes in discardReturn.",
            rules = new GeminiRules
            {
                rules = aiRules
            },
            playerHand = AIHand.Select(c => c.ToString()).ToList(),
            discardTop = deck.DiscardCount > 0 ? deck.PeekDiscard().ToString() : "",
            stack = new List<string>()
        };
    }

    private IEnumerator ValidateAndPlayPlayerTurn()
    {
        if (gameEnd || isAITurn)
            yield break;

        endTurnButton.interactable = false;

        GeminiResponse validationResponse = null;
        yield return StartCoroutine(ValidateMoveWithAI(true, playerHand, selectedCards.OrderBy(i => i).ToList(), "Player", response => validationResponse = response));

        if (!IsValidationSuccessful(validationResponse, out string validationMessage))
        {
            string message = string.IsNullOrWhiteSpace(validationMessage)
                ? "Invalid move. Please take your go again."
                : "Invalid move: " + validationMessage + " Please take your go again.";

            ShowTurnMessage(message);
            Debug.LogWarning(message);
            endTurnButton.interactable = true;
            yield break;
        }

        ShowTurnMessage(string.Empty);
        PlaySelectedCards();
    }

    private IEnumerator ValidateMoveWithAI(bool isPlayerMove, List<Card> hand, List<int> indices, string actorName, Action<GeminiResponse> onComplete)
    {
        if (geminiAI == null)
        {
            Debug.LogWarning("Gemini AI reference is missing. Treating move as valid because no validator is available.");
            onComplete?.Invoke(new GeminiResponse { action = "VALID", discardReturn = "No validator attached.", updatedHand = new List<string>() });
            yield break;
        }

        List<string> selectedCardDescriptions = indices
            .Where(idx => idx >= 0 && idx < hand.Count)
            .Select(idx => DescribeCardForValidation(hand[idx], idx))
            .ToList();

        GeminiRequest validationRequest = new GeminiRequest
        {
            gameId = currentGameId,
            instruction = actorName + " intends to play these card indexes: " + BuildIndicesString(indices) + ". " +
                          "The selected cards are: " + string.Join(", ", selectedCardDescriptions) + ". " +
                          "Validate ONLY whether this move follows the active rules. Return ONLY JSON. " +
                          "Set action to VALID or INVALID. Put a short reason in discardReturn. Leave updatedHand empty.",
            rules = new GeminiRules
            {
                rules = GetValidationRulesForAI(isPlayerMove, hand.Count)
            },
            playerHand = hand.Select(c => c.ToString()).ToList(),
            discardTop = deck.DiscardCount > 0 ? deck.PeekDiscard().ToString() : "",
            stack = new List<string>()
        };

        yield return StartCoroutine(SendAIRequestWithRetry(validationRequest, aiMaxAttempts, onComplete));
    }

    private IEnumerator SendAIRequestWithRetry(GeminiRequest req, int maxAttempts, Action<GeminiResponse> onComplete)
    {
        if (geminiAI == null)
        {
            Debug.LogWarning("Gemini AI reference is missing.");
            onComplete?.Invoke(null);
            yield break;
        }

        GeminiResponse finalResponse = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            geminiAI.ResetLatestResponse();
            geminiAI.SendToGemini(CloneGeminiRequest(req));

            float elapsed = 0f;
            while (elapsed < aiResponseTimeoutSeconds)
            {
                if (geminiAI.latestResponse != null && !string.IsNullOrWhiteSpace(geminiAI.latestResponse.action))
                {
                    finalResponse = geminiAI.latestResponse;
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (finalResponse != null)
            {
                break;
            }

            Debug.LogWarning($"AI request attempt {attempt} of {maxAttempts} failed or timed out.");

            if (attempt < maxAttempts)
                yield return new WaitForSeconds(aiRetryDelaySeconds);
        }

        geminiAI.ResetLatestResponse();
        onComplete?.Invoke(finalResponse);
    }

    private GeminiRequest CloneGeminiRequest(GeminiRequest req)
    {
        return new GeminiRequest
        {
            gameId = req.gameId,
            instruction = req.instruction,
            rules = new GeminiRules
            {
                rules = req.rules != null && req.rules.rules != null ? new List<string>(req.rules.rules) : new List<string>()
            },
            playerHand = req.playerHand != null ? new List<string>(req.playerHand) : new List<string>(),
            discardTop = req.discardTop,
            stack = req.stack != null ? new List<string>(req.stack) : new List<string>()
        };
    }

    private bool IsValidationSuccessful(GeminiResponse validationResponse, out string message)
    {
        message = validationResponse != null ? validationResponse.discardReturn : "No validation response received.";
        if (validationResponse == null || string.IsNullOrWhiteSpace(validationResponse.action))
            return false;

        string action = validationResponse.action.Trim().ToUpperInvariant();
        return action == "VALID" || action == "OK" || action == "PASS";
    }

    private string DescribeCardForValidation(Card card, int index)
    {
        string description = "[" + index + "] " + card;
        if ((card.Rank == Rank.Joker || card.Rank == Rank.Joker2) && jokerValues.TryGetValue(card, out int jokerValue))
            description += $" with declared joker value {jokerValue}";
        return description;
    }

    private List<string> GetValidationRulesForAI(bool isPlayerMove, int handCount)
    {
        List<string> validationRules = new List<string>(GetActiveRulesForAI());

        validationRules.Add($"This is a move validation request for the {(isPlayerMove ? "human player" : "AI player")}.");
        validationRules.Add($"Current hand size before the move is {handCount}.");
        validationRules.Add("A move is INVALID if any selected card index does not exist in the current hand.");
        validationRules.Add("A move is INVALID if no cards are selected.");
        validationRules.Add("If a fixed card play amount rule is active, the number of selected cards must match it exactly.");
        validationRules.Add("If jokers are selected, they must already have a declared value.");
        validationRules.Add("Ignore future draw actions. Only validate the cards being played right now.");

        return validationRules;
    }

    private bool TryParseCardIndices(string value, int handCount, out List<int> indices)
    {
        indices = new List<int>();

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] parts = value.Split('-');
        foreach (string part in parts)
        {
            if (!int.TryParse(part.Trim(), out int index))
                return false;

            if (index < 0 || index >= handCount)
                return false;

            if (indices.Contains(index))
                return false;

            indices.Add(index);
        }

        if (indices.Count == 0)
            return false;

        if (rulePlayAmount > 0 && indices.Count != rulePlayAmount)
            return false;

        return true;
    }

    private string BuildIndicesString(IEnumerable<int> indices)
    {
        return string.Join("-", indices);
    }

    private string BuildFallbackAICards()
    {
        if (AIHand.Count == 0)
            return string.Empty;

        int playAmount;
        if (rulePlayAmount > 0)
        {
            playAmount = Mathf.Min(rulePlayAmount, AIHand.Count);
        }
        else
        {
            // When there is no fixed play limit, allow the fallback to choose multiple cards too.
            playAmount = UnityEngine.Random.Range(1, AIHand.Count + 1);
        }

        List<int> fallback = new List<int>();

        while (fallback.Count < playAmount)
        {
            int candidate = UnityEngine.Random.Range(0, AIHand.Count);
            if (!fallback.Contains(candidate))
                fallback.Add(candidate);
        }

        fallback.Sort();
        return BuildIndicesString(fallback);
    }

    private void ShowTurnMessage(string message)
    {
        if (turnMessageText != null)
            turnMessageText.text = message;
        else if (geminiAI != null && geminiAI.uiText != null)
            geminiAI.uiText.text = message;
    }

    // ---- Displaying top of discard ----
    void UpdateDiscardTopCard()
    {
        if (discardTopCardImage == null) return;

        if (deck.DiscardCount == 0)
        {
            discardTopCardImage.sprite = null;
            return;
        }

        Card topCard = deck.PeekDiscard();
        string cardKey = GetCardKey(topCard);

        Debug.Log($"Discard card key: {cardKey}");

        if (cardTextureDict.TryGetValue(cardKey, out Texture2D tex))
        {
            Sprite s = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            discardTopCardImage.sprite = s;
        }
        else
        {
            Debug.LogWarning($"Discard texture not found for key: {cardKey}");
        }
    }


    // ---- Ending the game ----
    void EndGame()
    {
        Debug.Log($"Game Over!");
        bool playerWin = false;
        if (rulePointsWin)
        {
            if (totalPoints > totalAIPoints)
            {
                Debug.Log($"Player Wins!");
                playerWin = true;
            }
            else if (totalAIPoints > totalPoints) Debug.Log($"AI Wins!");
            else if (ruleLeastCardsWin)
            {
                if (playerHand.Count < AIHand.Count)
                {
                    Debug.Log($"Player Wins!");
                    playerWin = true;
                }
                else if (playerHand.Count > AIHand.Count) Debug.Log($"AI Wins!");
                else Debug.Log($"It was a tie!");
            }
            else Debug.Log($"It was a tie!");
        }
        else if (ruleLeastCardsWin)
        {
            if (playerHand.Count > AIHand.Count)
            {
                Debug.Log($"Player Wins!");
                playerWin = true;
            }
            else if (playerHand.Count < AIHand.Count) Debug.Log($"AI Wins!");
            else Debug.Log($"It was a tie!");
        }
        endTurnButton.interactable = false;
        ruleDraw = 0;
        ruleDrawHand = false;
        foreach (Transform child in cardParent)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
        if (playerWin == true)
        {
            winPanel.SetActive(true);
        }
        else
        {
            losePanel.SetActive(true);
        }
        gameEnd = true;
    }

    // ---- Drawing cards ----
    void DrawCards(int count)
    {
        StartCoroutine(DrawCardsCoroutine(count));
    }

    private IEnumerator DrawCardsCoroutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
            {
                if (ruleDeckout)
                {
                    EndGame();
                    break;
                }
                else if (ruleReshuffle && deck.DiscardCount > 0) Reshuffle();
                else
                {
                    // Can't draw more cards
                    break;
                }
            }

            if (playerHand.Count < ruleMaxHand || ruleMaxHand == 0)
            {
                Card c = deck.Draw();
                if (c != null)
                {
                    playerHand.Add(c);

                    // ---- Spawn visual card ----
                    if (cardVisualPrefab != null && playAreaParent != null)
                    {
                        GameObject visualCard = Instantiate(
                            cardVisualPrefab,
                            playAreaParent.position, // spawn at deck position
                            Quaternion.identity,
                            playAreaParent
                        );
                        // Assign the correct texture
                        if (cardTextureDict.TryGetValue(GetCardKey(c), out Texture2D tex))
                        {
                            Transform cardFront = visualCard.transform.Find("CardFront");
                            if (cardFront != null)
                            {
                                MeshRenderer mr = cardFront.GetComponent<MeshRenderer>();
                                if (mr != null)
                                {
                                    mr.material = new Material(mr.material); // clone so it doesn't overwrite shared material
                                    mr.material.mainTexture = tex;
                                }
                                else
                                {
                                    Debug.LogError("MeshRenderer not found on CardFront!");
                                }
                            }
                            else
                            {
                                Debug.LogError("CardFront child not found on visualCard prefab!");
                            }

                        }

                        visualCard.transform.localScale = Vector3.one * 0.2f;
                        visualCard.transform.rotation = Quaternion.Euler(90, 0, 0);
                        Animator anim = visualCard.GetComponent<Animator>();
                        if (anim != null)
                        {
                            anim.SetTrigger("playerDraw");
                        }
                        Destroy(visualCard, 1.0f);
                    }
                    yield return new WaitForSeconds(0.2f);
                    UpdateHandUI();
                }
            }
        }

        Debug.Log($"There are {deck.Count} cards left in the deck");

        // Update UI after all cards drawn
        UpdateHandUI();
    }

    void DrawAICards(int count)
    {
        StartCoroutine(DrawAICardsCoroutine(count));
    }

    private IEnumerator DrawAICardsCoroutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
            {
                if (ruleDeckout)
                {
                    EndGame();
                    break;
                }
                else if (ruleReshuffle && deck.DiscardCount > 0)
                {
                    Reshuffle();
                }
                else
                {
                    // Can't draw more cards
                    break;
                }
            }
            if (AIHand.Count < ruleMaxHand || ruleMaxHand == 0)
            {
                Card c = deck.Draw();
                if (c != null)
                {
                    AIHand.Add(c);
                    if (cardVisualPrefab != null && playAreaParent != null)
                    {
                        GameObject visualCard = Instantiate(
                            cardVisualPrefab,
                            playAreaParent.position, // spawn at deck position
                            Quaternion.identity,
                            playAreaParent
                        );
                        visualCard.transform.localScale = Vector3.one * 0.2f;
                        visualCard.transform.rotation = Quaternion.Euler(90, 0, 0);
                        Animator anim = visualCard.GetComponent<Animator>();
                        if (anim != null)
                        {
                            anim.SetTrigger("enemyDraw");
                        }
                        Destroy(visualCard, 1.0f);
                    }
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }
        Debug.Log($"There are {deck.Count} cards left in the deck");
        UpdateHandUI();
    }

    Sprite ConvertTextureToSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height), // full texture
            new Vector2(0.5f, 0.5f) // pivot in center
        );
    }

    Sprite GetCardBackSprite()
    {
        if (cardBackTexture == null)
        {
            Debug.LogWarning("Card back texture not assigned!");
            return null;
        }

        return Sprite.Create(
            cardBackTexture,
            new Rect(0, 0, cardBackTexture.width, cardBackTexture.height),
            new Vector2(0.5f, 0.5f)
        );
    }


    void LoadCardTextures()
    {
        cardTextureDict.Clear();
        foreach (Texture2D tex in cardTextures)
        {
            if (tex == null) continue;
            string key;
            if (tex.name.StartsWith("Joker"))
            {
                // Keep Joker names as-is
                key = tex.name; // e.g., "Joker cards-R"
            }
            else if (tex.name.StartsWith("Rules"))
            {
                // Keep Rules card name as-is
                key = tex.name; // e.g., "Rules card"
            }
            else
            {
                // Normal cards
                key = tex.name.Replace(" cards-", "-"); // "Spade cards-6" -> "Spade-6"
            }
            cardTextureDict[key] = tex;
        }
    }

    string GetCardKey(Card c)
    {
        if (c.Rank == Rank.Joker) return "Joker cards-R";
        if (c.Rank == Rank.Joker2) return "Joker cards-B";
        if (c.Rank == Rank.Rules) return "Rules card";

        string rankString;
        switch (c.Rank)
        {
            case Rank.Jack: rankString = "J"; break;
            case Rank.Queen: rankString = "Q"; break;
            case Rank.King: rankString = "K"; break;
            case Rank.Ace: rankString = "A"; break;
            default: rankString = ((int)c.Rank).ToString(); break;
        }

        return $"{c.Suit}-{rankString}";
    }

    public void Reshuffle()
    {
        StartCoroutine(ReshuffleCoroutine());
    }
    private IEnumerator ReshuffleCoroutine()
    {
        if (deck.DiscardCount == 0) yield break;

        if (deckShuffler != null) deckShuffler.SetTrigger("deckShuffle");
        if (cardShuffler != null) cardShuffler.SetTrigger("cardShuffle");

        yield return new WaitForSeconds(4.0f); // Wait for animation duration

        deck.Reshuffle();
    }

    // ---- Getting value ----
    int GetCardValue(Card c)
    {
        switch (c.Rank)
        {
            case Rank.Two: return 2;
            case Rank.Three: return 3;
            case Rank.Four: return 4;
            case Rank.Five: return 5;
            case Rank.Six: return 6;
            case Rank.Seven: return 7;
            case Rank.Eight: return 8;
            case Rank.Nine: return 9;
            case Rank.Ten:
            case Rank.Jack:
            case Rank.Queen:
            case Rank.King: return 10;
            case Rank.Ace: return 11;
            case Rank.Joker:
            case Rank.Joker2:
            case Rank.Rules:
            default: return 0;
        }
    }

    public List<string> GetActiveRulesForAI()
    {
        List<string> aiRules = new List<string>();

        if (ruleStartHand != 5) // assume default 5 means inactive
            aiRules.Add($"Starting hand size is {ruleStartHand} cards.");

        if (ruleDraw > 0)
            aiRules.Add($"Draw {ruleDraw} card(s) each turn.");

        if (ruleMaxHand > 0)
            aiRules.Add($"Maximum hand size is {ruleMaxHand} card(s).");

        if (rulePointsEnabled)
            aiRules.Add("You gain points equal to value of cards.");

        if (rulePointsEnd)
            aiRules.Add($"The game ends when a player reaches {pointEndLimit} points.");

        if (rulePointsWin)
            aiRules.Add("The player with the most points wins.");

        if (ruleReshuffle)
            aiRules.Add("The deck reshuffles automatically when empty.");

        if (ruleJoker)
            aiRules.Add("Jokers are enabled and can be played with custom values (A-K).");

        if (rulesCard)
            aiRules.Add("Special Rules cards are enabled in the game. Rules cards add a rule to the game when played and are worth 0 points (if points are on).");

        if (ruleDrawHand)
            aiRules.Add($"On turn start, you draw cards up to the starting hand amount {ruleStartHand}.");

        if (ruleTurnLimit > 0)
            aiRules.Add($"Game will end after turn {ruleTurnLimit}.");

        if (ruleDeckout)
            aiRules.Add("Game will end when the deck runs out of cards.");

        if (ruleOutofCards)
            aiRules.Add("Game will end when you run out of cards in your hand.");

        if (ruleLeastCardsWin)
            aiRules.Add("Win the game by having the least cards in hand when the game ends.");

        if (rulePlayAmount > 0)
            aiRules.Add($"You MUST play EXACTLY {rulePlayAmount} cards per turn. Seperate each card played with a '-' (example for 3 cards: 0-2-3 etc).");
        else
            aiRules.Add($"You can play any number of cards per turn. You MUST play at least 1 card. Seperate each card played with a '-' (example for 3 cards: 0-2-3 etc).");

        return aiRules;
    }

    // ---- Classes ----
    public enum Suit { Hearts, Diamond, Clubs, Spade, None }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace, Joker, Joker2, Rules }

    [Serializable]
    public class Card
    {
        public Suit Suit;
        public Rank Rank;
        public int CustomValue = 0;

        public override string ToString()
        {
            string rankString = Rank.ToString();
            string suitString = Suit != Suit.None ? " of " + Suit.ToString() : "";
            return rankString + suitString;
        }
    }

    [Serializable]
    public class Deck
    {
        private List<Card> cards = new List<Card>();
        private List<Card> discardPile = new List<Card>();

        public Deck()
        {
            Reset();
            Shuffle();
        }

        public void Reset()
        {
            cards.Clear();
            for (int s = 0; s < 4; s++)
                for (int r = 2; r <= 14; r++)
                    cards.Add(new Card { Suit = (Suit)s, Rank = (Rank)r });
        }

        public void AddJoker()
        {
            cards.Add(new Card { Suit = Suit.None, Rank = Rank.Joker });
            cards.Add(new Card { Suit = Suit.None, Rank = Rank.Joker2 });
        }

        public void AddRules()
        {
            cards.Add(new Card { Suit = Suit.None, Rank = Rank.Rules });
        }

        public void AddToDiscard(Card c) => discardPile.Add(c);

        public Card PeekDiscard()
        {
            if (discardPile.Count == 0) return null; // empty discard
            return discardPile[discardPile.Count - 1];    // last card = top of discard
        }

        public void Shuffle()
        {
            var rng = new System.Random();
            cards = cards.OrderBy(a => rng.Next()).ToList();
        }

        public Card Draw()
        {
            if (cards.Count == 0) return null;
            Card card = cards[cards.Count - 1];
            cards.RemoveAt(cards.Count - 1);
            return card;
        }

        public void Reshuffle()
        {
            cards.AddRange(discardPile);
            discardPile.Clear();
            Shuffle();
        }

        public int Count => cards.Count;
        public int DiscardCount => discardPile.Count;
    }

    [Serializable]
    public class Rule
    {
        public string Name;
        public bool Enabled;                  // Whether the rule is currently active
        public List<string> RequiresNames;    // Other rules that must be enabled first
        public Action OnEnable;               // Action when the rule is enabled
        public Action OnDisable;              // Action when the rule is disabled

        public void Enable()
        {
            if (!Enabled)
            {
                Enabled = true;
                OnEnable?.Invoke();
                Debug.Log($"Rule enabled: {Name}");
            }
        }

        public void Disable()
        {
            if (Enabled)
            {
                Enabled = false;
                OnDisable?.Invoke();
                Debug.Log($"Rule disabled: {Name}");
            }
        }
    }
}