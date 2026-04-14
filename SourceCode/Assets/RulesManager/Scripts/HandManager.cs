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
    public TextMeshProUGUI turnNumber;
    public TextMeshProUGUI warningMessage;
    public Image discardTopCardImage;
    public Texture2D[] cardTextures;
    public Dictionary<string, Texture2D> cardTextureDict = new Dictionary<string, Texture2D>();

    [Header("Card Art")]
    public Texture2D cardBackTexture;
    public GameObject cardVisualPrefab;
    public Transform playAreaParent;
    public Animator deckShuffler;
    public Animator cardShuffler;

    // ---- Audio ----
    public AudioSource audioSource;
    public AudioClip Swish;
    public AudioClip Shuffle;
    public AudioClip Win;
    public AudioClip Lose;

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
    public int ruleStartHand = 5;           // Rule 1
    public int ruleDraw = 0;                // Rule 2
    public int ruleMaxHand = 0;             // Rule 3
    public bool rulePointsEnabled = false;  // Rule 4
    public bool rulePointsEnd = false;      // Rule 5
    public bool rulePointsWin = false;      // Rule 6
    public bool ruleReshuffle = false;      // Rule 7
    public bool ruleJoker = false;          // Rule 8
    public bool rulesCard = false;          // Rule 9
    public bool ruleDrawHand = false;       // Rule 10
    public int ruleTurnLimit = 0;           // Rule 11
    public bool ruleDeckout = false;        // Rule 12
    public bool ruleOutofCards = false;     // Rule 13
    public bool ruleLeastCardsWin = false;  // Rule 14
    public int rulePlayAmount = 0;          // Rule 15
    public bool rulePlayMatch = false;      // Rule 16
    public int ruleDrawEarlyEnd = 0;        // Rule 17
    public List<Rule> rules;                // List of Rules

    // ---- AI ----
    public UnityGeminiCardAI geminiAI;

    [Header("Turn Messages")]
    public TMP_Text turnMessageText;

    private string currentGameId;
    private const int aiMaxAttempts = 3;
    private const float aiResponseTimeoutSeconds = 15f;
    private const float aiRetryDelaySeconds = 0.75f;

    public MenuManager menuManager;

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
        rulePlayMatch = gameRules.rulePlayMatch;
        ruleDrawEarlyEnd = gameRules.ruleDrawEarlyEnd;
        currentGameId = CreateUniqueGameId();

        rules = new List<Rule>
        {
            new Rule
            {
                Name = "Starting hand size",
                Enabled = ruleStartHand != 5,
                OnEnable = () =>
                {
                    ruleStartHand = UnityEngine.Random.Range(1, 10);
                    Debug.Log($"Starting hand size set to {ruleStartHand}");
                }
            },
            new Rule
            {
                Name = "Draw each turn",
                Enabled = ruleDraw > 0,
                OnEnable = () =>
                {
                    ruleDraw = UnityEngine.Random.Range(1, 5);
                    Debug.Log($"Draw each turn set to {ruleDraw}");
                }
            },
            new Rule { Name = "Points enabled", Enabled = rulePointsEnabled, OnEnable = () => rulePointsEnabled = true },
            new Rule
            {
                Name = "Most points win",
                Enabled = rulePointsWin,
                RequiresNames = new List<string> { "Points enabled" },
                OnEnable = () => rulePointsWin = true
            },
            new Rule
            {
                Name = "Game ends on points",
                Enabled = rulePointsEnd,
                RequiresNames = new List<string> { "Points enabled" },
                OnEnable = () =>
                {
                    rulePointsEnd = true;
                    pointEndLimit = UnityEngine.Random.Range(50, 300);
                    Debug.Log($"Points to reach set to {pointEndLimit}");
                }
            },
            new Rule
            {
                Name = "Max hand size",
                Enabled = ruleMaxHand > 0,
                OnEnable = () =>
                {
                    ruleMaxHand = UnityEngine.Random.Range(1, 8);
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
                    ruleTurnLimit = UnityEngine.Random.Range(turn + 3, turn + 8);
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
                    rulePlayAmount = UnityEngine.Random.Range(1, 3);
                    Debug.Log($"Card play max set to {rulePlayAmount}");
                }
            },
            new Rule { Name = "Must play matching card with discard card", Enabled = rulePlayMatch, OnEnable = () => rulePlayMatch = true },
            new Rule
            {
                Name = $"You draw {ruleDrawEarlyEnd} cards if you end your turn early",
                Enabled = ruleDrawEarlyEnd > 0,
                OnEnable = () =>
                {
                    ruleDrawEarlyEnd = UnityEngine.Random.Range(1, 3);
                    Debug.Log($"Card draw early set to {ruleDrawEarlyEnd}");
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
            jokerPanel.SetActive(false);

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

        if (ruleTurnLimit > 0)
            turnNumber.text = $"turn : {turn + 1}";

        if (warningMessage != null)
            warningMessage.text = "";

        StartCoroutine(PrimeAIOnStartup());
    }

    // ---- UI Updates ----
    public void UpdateHandUI()
    {
        foreach (Transform child in cardParent)
            Destroy(child.gameObject);

        float spacing = 0f;
        float startX = 0f;
        float AIspacing = 0f;
        float AIstartX = 0f;

        if (playerHand.Count > 1)
        {
            spacing = 550f / (playerHand.Count - 1);
            startX = -((playerHand.Count - 1) * spacing) / 2f;
        }

        if (AIHand.Count > 1)
        {
            AIspacing = 550f / (AIHand.Count - 1);
            AIstartX = -((AIHand.Count - 1) * AIspacing) / 2f;
        }

        // ---- Player hand ----
        for (int i = 0; i < playerHand.Count; i++)
        {
            GameObject cardButton = Instantiate(cardPrefab, cardParent);

            string cardKey = GetCardKey(playerHand[i]);
            if (cardTextureDict.TryGetValue(cardKey, out Texture2D tex))
            {
                Sprite s = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f)
                );
                cardButton.GetComponent<Image>().sprite = s;
            }

            TMP_Text tmpText = cardButton.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
                tmpText.text = playerHand[i].ToString();

            RectTransform rt = cardButton.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * spacing, 60);
            rt.sizeDelta = new Vector2(120, 180);

            int index = i;
            Button btn = cardButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnCardClicked(index));
        }

        // ---- AI hand ----
        for (int i = 0; i < AIHand.Count; i++)
        {
            GameObject AICardButton = Instantiate(cardPrefab, cardParent);

            Image img = AICardButton.GetComponent<Image>();
            Sprite backSprite = GetCardBackSprite();
            if (img != null && backSprite != null)
                img.sprite = backSprite;

            TMP_Text aiTmpText = AICardButton.GetComponentInChildren<TMP_Text>();
            if (aiTmpText != null)
                aiTmpText.text = "";

            RectTransform rt = AICardButton.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(AIstartX + i * AIspacing, 1000);
            rt.sizeDelta = new Vector2(120, 180);

            Button btn = AICardButton.GetComponent<Button>();
            if (btn != null)
                btn.interactable = false;

            CanvasGroup cg = AICardButton.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = AICardButton.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
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
        if (selectedCards.Count == 0 && ruleDrawEarlyEnd == 0)
        {
            if (warningMessage != null)
                warningMessage.text = "No cards selected!";
            Debug.Log("No cards selected!");
            return;
        }
        else if (selectedCards.Count == 0 && ruleDrawEarlyEnd > 0)
        {
            DrawCards(ruleDrawEarlyEnd);
            FinishTurn();
            return;
        }

        if (rulePlayAmount > 0 && selectedCards.Count != rulePlayAmount)
        {
            if (warningMessage != null)
                warningMessage.text = $"You must select {rulePlayAmount} cards.";
            Debug.Log($"You must play {rulePlayAmount} cards.");
            return;
        }

        if (rulePlayMatch && deck.PeekDiscard() != null)
        {
            Card discard = deck.PeekDiscard();

            foreach (int idx in selectedCards)
            {
                Card c = playerHand[idx];

                bool isJoker = c.Rank == Rank.Joker || c.Rank == Rank.Joker2;
                bool discardIsJoker = discard.Rank == Rank.Joker || discard.Rank == Rank.Joker2;

                if (!isJoker && !discardIsJoker)
                {
                    bool matchesRank = c.Rank == discard.Rank;
                    bool matchesSuit = c.Suit == discard.Suit;

                    if (!matchesRank && !matchesSuit)
                    {
                        if (warningMessage != null)
                            warningMessage.text = "You must play card(s) matching the discard card.";
                        Debug.Log("You must play a card matching the discard card.");
                        return;
                    }
                }
            }
        }

        int jokerIdx = selectedCards.FirstOrDefault(idx =>
            playerHand[idx].Rank == Rank.Joker || playerHand[idx].Rank == Rank.Joker2);

        if (selectedCards.Any(idx => playerHand[idx].Rank == Rank.Joker || playerHand[idx].Rank == Rank.Joker2))
        {
            currentJokerIndex = jokerIdx;
            jokerPanel.SetActive(true);
            return;
        }

        if (warningMessage != null)
            warningMessage.text = "";

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
                            mr.material = new Material(mr.material);
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
                    anim.SetTrigger("playerPlay");

                yield return new WaitForSeconds(0.2f);
                audioSource.clip = Swish;
                audioSource.Play();
                Destroy(visualCard, 1.0f);
            }

            UpdateHandUI();
        }

        selectedCards.Clear();

        foreach (var c in playedCards)
            deck.AddToDiscard(c);

        UpdateDiscardTopCard();
        UpdateHandUI();

        if (ruleOutofCards && playerHand.Count == 0 && !rulePointsEnabled)
            EndGame();

        FinishTurn();
    }

    void PlayAICards(string AIIndices)
    {
        StartCoroutine(PlayAICardsoroutine(AIIndices));
    }

    private IEnumerator PlayAICardsoroutine(string AIIndices)
    {
        Debug.Log("Coroutine started");
        if (string.IsNullOrEmpty(AIIndices) && ruleDrawEarlyEnd == 0)
        {
            Debug.Log("AIIndices empty, exiting");
            yield break;
        }
        else if (string.IsNullOrEmpty(AIIndices) && ruleDrawEarlyEnd > 0)
        {
            DrawAICards(ruleDrawEarlyEnd);
            UpdateHandUI();
            isAITurn = true;
            FinishTurn();
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
                                mr.material = new Material(mr.material);
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
                        anim.SetTrigger("enemyPlay");

                    yield return new WaitForSeconds(0.5f);
                    audioSource.clip = Swish;
                    audioSource.Play();
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

            if (ruleOutofCards && playerHand.Count == 0) EndGame();
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

        LogAIHand("Before AI chooses move");

        GeminiRequest req = BuildAITurnRequest();

        GeminiResponse aiMoveResponse = null;
        yield return StartCoroutine(SendAIRequestWithRetry(req, aiMaxAttempts, response => aiMoveResponse = response));

        string aiCardsToPlay = null;
        List<int> aiIndices = new List<int>();

        if (aiMoveResponse != null && TryParseCardIndices(aiMoveResponse.discardReturn, AIHand.Count, out aiIndices))
        {
            LogSelectedAIIndices("Parsed Gemini selection", aiIndices);

            if (AreSelectedAICardsLocallyValid(aiIndices, out string localReason))
            {
                aiCardsToPlay = BuildIndicesString(aiIndices);
                Debug.Log("AI move accepted (LOCAL validation): " + aiCardsToPlay);
            }
            else
            {
                Debug.LogWarning("AI move failed LOCAL validation: " + localReason);
            }
        }
        else
        {
            Debug.LogWarning("AI returned invalid format or indices.");
        }

        if (string.IsNullOrWhiteSpace(aiCardsToPlay))
        {
            Debug.LogWarning("AI error or invalid move. Falling back to local valid selection.");
            aiCardsToPlay = BuildFallbackAICards();

            Debug.Log("[AI DEBUG] Fallback move string = " + aiCardsToPlay);

            if (!string.IsNullOrWhiteSpace(aiCardsToPlay) &&
                TryParseCardIndices(aiCardsToPlay, AIHand.Count, out List<int> fallbackIndices))
            {
                LogSelectedAIIndices("Fallback selection", fallbackIndices);
            }
        }

        PlayAICards(aiCardsToPlay);

        int playedCount = string.IsNullOrWhiteSpace(aiCardsToPlay) ? 0 : aiCardsToPlay.Split('-').Length;
        yield return new WaitForSeconds(0.6f * playedCount);

        if (ruleOutofCards && AIHand.Count == 0)
            EndGame();

        turn += 1;
        if (ruleTurnLimit > 0) turnNumber.text = $"turn : {turn + 1}";
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

        Debug.Log("[AI DEBUG] Building AI turn request...");
        Debug.Log("[AI DEBUG] Game ID: " + currentGameId);
        Debug.Log("[AI DEBUG] Discard top: " + (deck.DiscardCount > 0 ? deck.PeekDiscard().ToString() : "(none)"));
        Debug.Log("[AI DEBUG] Active rules: " + string.Join(" | ", aiRules));
        Debug.Log("[AI DEBUG] AI hand being sent: " + string.Join(", ", AIHand.Select((c, i) => $"[{i}] {c}")));

        return new GeminiRequest
        {
            gameId = currentGameId,
            instruction = "You are a player in a card game. Choose a move that is fully legal under the listed rules. Follow the rules exactly. Return ONLY JSON in this exact format: {\"action\":\"PLAY\",\"discardReturn\":\"0\",\"updatedHand\":[\"Card A\",\"Card B\"]}. action must be a string. discardReturn must be a STRING, not an array. Use \"0\" for one card or \"0-2\" for multiple cards. If exactly one card must be played, return exactly one index as a string. Never return an illegal move.",
            rules = new GeminiRules
            {
                rules = aiRules
            },
            playerHand = AIHand.Select(c => c.ToString()).ToList(),
            discardTop = deck.DiscardCount > 0 ? deck.PeekDiscard().ToString() : "",
            stack = new List<string>()
        };
    }

    // ---- HUMAN MOVE VALIDATION: LOCAL ONLY ----
    private IEnumerator ValidateAndPlayPlayerTurn()
    {
        if (gameEnd || isAITurn)
            yield break;

        endTurnButton.interactable = false;

        List<int> orderedIndices = selectedCards.OrderBy(i => i).ToList();

        if (!AreSelectedPlayerCardsLocallyValid(orderedIndices, out string validationMessage))
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

    // Left in place for safety, but no longer used by human move validation
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
                    Debug.Log("[AI DEBUG] Response received:");
                    Debug.Log("[AI DEBUG] action = " + finalResponse.action);
                    Debug.Log("[AI DEBUG] discardReturn = " + finalResponse.discardReturn);
                    Debug.Log("[AI DEBUG] updatedHand = " + (finalResponse.updatedHand != null
                        ? string.Join(", ", finalResponse.updatedHand)
                        : "(null)"));
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
        validationRules.Add("Jokers are valid wildcard plays. If selected, they may require a separate value selection step.");
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

    private bool AreSelectedPlayerCardsLocallyValid(List<int> indices, out string reason)
    {
        reason = "";

        if (indices == null || indices.Count == 0)
        {
            if (ruleDrawEarlyEnd > 0)
                return true;

            reason = "No cards selected.";
            return false;
        }

        if (rulePlayAmount > 0 && indices.Count != rulePlayAmount)
        {
            reason = $"You must play exactly {rulePlayAmount} cards.";
            return false;
        }

        foreach (int idx in indices)
        {
            if (idx < 0 || idx >= playerHand.Count)
            {
                reason = $"Selected out-of-range index {idx}.";
                return false;
            }
        }

        if (rulePlayMatch && deck.PeekDiscard() != null)
        {
            Card discard = deck.PeekDiscard();

            foreach (int idx in indices)
            {
                Card c = playerHand[idx];

                bool isJoker = c.Rank == Rank.Joker || c.Rank == Rank.Joker2;
                bool discardIsJoker = discard.Rank == Rank.Joker || discard.Rank == Rank.Joker2;

                if (!isJoker && !discardIsJoker)
                {
                    bool matchesRank = c.Rank == discard.Rank;
                    bool matchesSuit = c.Suit == discard.Suit;

                    if (!matchesRank && !matchesSuit)
                    {
                        reason = $"Card [{idx}] {c} does not match discard {discard}.";
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private bool AreSelectedAICardsLocallyValid(List<int> indices, out string reason)
    {
        reason = "";

        if (indices == null || indices.Count == 0)
        {
            if (ruleDrawEarlyEnd > 0)
                return true;

            reason = "No cards selected.";
            return false;
        }

        if (rulePlayAmount > 0 && indices.Count != rulePlayAmount)
        {
            reason = $"AI must play exactly {rulePlayAmount} cards.";
            return false;
        }

        foreach (int idx in indices)
        {
            if (idx < 0 || idx >= AIHand.Count)
            {
                reason = $"AI selected out-of-range index {idx}.";
                return false;
            }
        }

        if (rulePlayMatch && deck.PeekDiscard() != null)
        {
            Card discard = deck.PeekDiscard();

            foreach (int idx in indices)
            {
                Card c = AIHand[idx];

                bool isJoker = c.Rank == Rank.Joker || c.Rank == Rank.Joker2;
                bool discardIsJoker = discard.Rank == Rank.Joker || discard.Rank == Rank.Joker2;

                if (!isJoker && !discardIsJoker)
                {
                    bool matchesRank = c.Rank == discard.Rank;
                    bool matchesSuit = c.Suit == discard.Suit;

                    if (!matchesRank && !matchesSuit)
                    {
                        reason = $"AI selected illegal card [{idx}] {c} for discard {discard}.";
                        return false;
                    }
                }
            }
        }

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

        List<int> validIndices = new List<int>();

        for (int i = 0; i < AIHand.Count; i++)
        {
            Card c = AIHand[i];
            bool valid = true;

            if (rulePlayMatch && deck.PeekDiscard() != null)
            {
                Card discard = deck.PeekDiscard();

                bool isJoker = c.Rank == Rank.Joker || c.Rank == Rank.Joker2;
                bool discardIsJoker = discard.Rank == Rank.Joker || discard.Rank == Rank.Joker2;

                if (!isJoker && !discardIsJoker)
                {
                    bool matchesRank = c.Rank == discard.Rank;
                    bool matchesSuit = c.Suit == discard.Suit;

                    if (!matchesRank && !matchesSuit)
                        valid = false;
                }
            }

            if (valid)
                validIndices.Add(i);
        }

        if (validIndices.Count == 0)
        {
            if (ruleDrawEarlyEnd > 0)
                return string.Empty;

            return string.Empty;
        }

        if (rulePlayAmount > 0)
        {
            if (validIndices.Count < rulePlayAmount)
            {
                if (ruleDrawEarlyEnd > 0)
                    return string.Empty;

                return string.Empty;
            }

            List<int> chosen = validIndices.Take(rulePlayAmount).ToList();
            return BuildIndicesString(chosen);
        }
        else
        {
            return BuildIndicesString(new List<int> { validIndices[0] });
        }
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
        Debug.Log("Game Over!");
        bool playerWin = false;

        if (rulePointsWin)
        {
            if (totalPoints > totalAIPoints)
            {
                Debug.Log("Player Wins!");
                playerWin = true;
            }
            else if (totalAIPoints > totalPoints) Debug.Log("AI Wins!");
            else if (ruleLeastCardsWin)
            {
                if (playerHand.Count < AIHand.Count)
                {
                    Debug.Log("Player Wins!");
                    playerWin = true;
                }
                else if (playerHand.Count > AIHand.Count) Debug.Log("AI Wins!");
                else Debug.Log("It was a tie!");
            }
            else Debug.Log("It was a tie!");
        }
        else if (ruleLeastCardsWin)
        {
            if (playerHand.Count < AIHand.Count)
            {
                Debug.Log("Player Wins!");
                playerWin = true;
            }
            else if (playerHand.Count > AIHand.Count) Debug.Log("AI Wins!");
            else Debug.Log("It was a tie!");
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
            audioSource.clip = Win;
            audioSource.Play();
        }
        else
        {
            losePanel.SetActive(true);
            audioSource.clip = Lose;
            audioSource.Play();
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
                    break;
                }
            }

            if (playerHand.Count < ruleMaxHand || ruleMaxHand == 0)
            {
                Card c = deck.Draw();
                if (c != null)
                {
                    playerHand.Add(c);

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
                                    mr.material = new Material(mr.material);
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
                            anim.SetTrigger("playerDraw");

                        Destroy(visualCard, 1.0f);
                    }

                    yield return new WaitForSeconds(0.2f);
                    audioSource.clip = Swish;
                    audioSource.Play();
                    UpdateHandUI();
                }
            }
        }

        Debug.Log($"There are {deck.Count} cards left in the deck");
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
                            playAreaParent.position,
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
                    audioSource.clip = Swish;
                    audioSource.Play();
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
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
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
                key = tex.name;
            }
            else if (tex.name.StartsWith("Rules"))
            {
                key = tex.name;
            }
            else
            {
                key = tex.name.Replace(" cards-", "-");
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

        audioSource.clip = Shuffle;
        audioSource.Play();

        yield return new WaitForSeconds(4.0f);

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

    // ---- SHORTENED RULES ONLY ----
    public List<string> GetActiveRulesForAI()
    {
        List<string> aiRules = new List<string>();

        if (ruleStartHand != 5)
            aiRules.Add($"Start hand: {ruleStartHand}");

        if (ruleDraw > 0)
            aiRules.Add($"Draw each turn: {ruleDraw}");

        if (ruleMaxHand > 0)
            aiRules.Add($"Max hand: {ruleMaxHand}");

        if (rulePointsEnabled)
            aiRules.Add("Points enabled");

        if (rulePointsEnd)
            aiRules.Add($"End at {pointEndLimit} points");

        if (rulePointsWin)
            aiRules.Add("Most points wins");

        if (ruleReshuffle)
            aiRules.Add("Reshuffle when deck empty");

        if (ruleJoker)
            aiRules.Add("Jokers enabled");

        if (rulesCard)
            aiRules.Add("Rules card enabled");

        if (ruleDrawHand)
            aiRules.Add($"Draw to {ruleStartHand}");

        if (ruleTurnLimit > 0)
            aiRules.Add($"Turn limit: {ruleTurnLimit}");

        if (ruleDeckout)
            aiRules.Add("Deckout ends game");

        if (ruleOutofCards)
            aiRules.Add("Out of cards ends game");

        if (ruleLeastCardsWin)
            aiRules.Add("Least cards wins");

        if (rulePlayAmount > 0)
            aiRules.Add($"Play exactly {rulePlayAmount}");
        else
            aiRules.Add("Play at least 1");

        if (rulePlayMatch)
            aiRules.Add("Cards must match discard by suit or rank unless joker");

        if (ruleDrawEarlyEnd > 0)
            aiRules.Add($"No play = draw {ruleDrawEarlyEnd}");

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
            if (discardPile.Count == 0) return null;
            return discardPile[discardPile.Count - 1];
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
        public bool Enabled;
        public List<string> RequiresNames;
        public Action OnEnable;
        public Action OnDisable;

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

    private void LogAIHand(string label)
    {
        string handText = AIHand.Count == 0
            ? "(empty)"
            : string.Join(", ", AIHand.Select((c, i) => $"[{i}] {c}"));

        Debug.Log($"[AI DEBUG] {label} | AI Hand = {handText}");
    }

    private void LogSelectedAIIndices(string label, List<int> indices)
    {
        if (indices == null || indices.Count == 0)
        {
            Debug.Log($"[AI DEBUG] {label} | No indices selected");
            return;
        }

        List<string> selectedCardsText = new List<string>();

        foreach (int idx in indices)
        {
            if (idx >= 0 && idx < AIHand.Count)
                selectedCardsText.Add($"[{idx}] {AIHand[idx]}");
            else
                selectedCardsText.Add($"[{idx}] OUT OF RANGE");
        }

        Debug.Log($"[AI DEBUG] {label} | Selected indices = {string.Join(", ", indices)} | Selected cards = {string.Join(", ", selectedCardsText)}");
    }

    public void ReturnToMenu()
    {
        Debug.Log("Returning to main menu...");

        if (menuManager != null)
        {
            menuManager.ShowMainMenu();
        }
        else
        {
            Debug.LogError("MenuManager reference is missing!");
        }
    }
}