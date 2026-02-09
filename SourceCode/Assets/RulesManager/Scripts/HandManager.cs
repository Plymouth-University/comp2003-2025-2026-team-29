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
    public List<Rule> rules;                //List of Rules

    // ---- AI ----
    public UnityGeminiCardAI geminiAI;

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
        rules = new List<Rule> {
            new Rule { Name = "Starting hand size", Enabled = ruleStartHand != 5, OnEnable = () =>
                {
                    ruleStartHand = UnityEngine.Random.Range(1, 10); // random 1–10 cards
                    Debug.Log($"Starting hand size set to {ruleStartHand}");
                }
            },
            new Rule
            {
                Name = "Draw each turn",
                Enabled = ruleDraw > 0,
                OnEnable = () =>
                {
                    ruleDraw = UnityEngine.Random.Range(1, 5); // set draw to random 1–5
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
                    ruleMaxHand = UnityEngine.Random.Range(1, 8); // set max to random 1–8
                    Debug.Log($"Max hand size set to {ruleMaxHand}");
                }
            },
            new Rule { Name = "Reshuffle deck when empty", Enabled = ruleReshuffle, OnEnable = () => ruleReshuffle = true },
            new Rule { Name = "Jokers enabled", Enabled = ruleJoker, OnEnable = () => { ruleJoker = true; deck.AddJoker(); deck.Shuffle(); } },
            new Rule { Name = "Rules Card enabled", Enabled = rulesCard, OnEnable = () => { rulesCard = true; deck.AddRules(); deck.Shuffle(); } }
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
    }

    // ---- UI Updates ----
    public void UpdateHandUI()
    {
        foreach (Transform child in cardParent)
            Destroy(child.gameObject);

        float spacing = 550f / (playerHand.Count - 1);
        float startX = -((playerHand.Count - 1) * spacing) / 2;
        float AIspacing = 550f / (AIHand.Count - 1);
        float AIstartX = -((AIHand.Count - 1) * AIspacing) / 2;

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
        PlaySelectedCards();
    }

    // ---- End button pressed ----
    void OnEndTurnButtonPressed()
    {
        if (selectedCards.Count == 0)
        {
            Debug.Log("No cards selected!");
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
        PlaySelectedCards();
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
            UpdateHandUI();
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
        }
        selectedCards.Clear();
        foreach (var c in playedCards)
            deck.AddToDiscard(c);

        UpdateDiscardTopCard();

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

        string[] Parts = AIIndices.Split('/');
        List<int> AISelectedCards = Parts.Select(s => int.Parse(s)).ToList();
        playedCards.Clear();
        AISelectedCards.Sort();
        AISelectedCards.Reverse();

        foreach (int idx in AISelectedCards)
        {
            Debug.Log("Processing card index: " + idx);

            if (idx >= 0 && idx < AIHand.Count)
            {
                UpdateHandUI();
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
            }
        }
        foreach (var c in playedCards) deck.AddToDiscard(c);
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
                Debug.Log($"Turn points: {turnPoints}, AI Total points: {totalAIPoints}");
            }
            else
            {
                totalPoints += turnPoints;
                Debug.Log($"Turn points: {turnPoints}, Total points: {totalPoints}");
            }
            UpdateDiscardTopCard();

            if (rulePointsEnd && (totalPoints >= pointEndLimit || totalAIPoints >= pointEndLimit))
            {
                EndGame();
            }
        }
        if (!isAITurn) StartCoroutine(AITurn());
        isAITurn = false;
    }

    System.Collections.IEnumerator AITurn()
    {
        if (turn != 0) DrawAICards(ruleDraw);
        UpdateHandUI();
        GeminiRequest req = new GeminiRequest
        {
            // gameId = "GAME-001",
            gameId = "CARDSRULE-2026-02-06-13-14-00",
            instruction = "You are a player in a card game." +
                          "The gameId is an identifier for the game." +
                          "Using the rules listed in 'rules', and the cards in your hand, denoted by" +
                          "'playerHand' and the card shown on the discard pile, denoted as 'discardTop'," +
                          "you need to take your go and return the details." +
                          "For discarded card, give ONLY the number for location of the card for your response." +
                          "You can play any number of cards at once, separate each card played with a /.",
            rules = new GeminiRules
            {
                rules = GetActiveRulesForAI() // <-- only active rules
            },
            playerHand = AIHand.Select(c => c.ToString()).ToList(),
            discardTop = deck.DiscardCount > 0 ? deck.PeekDiscard().ToString() : "",
            stack = new List<string>()
        };
        Debug.Log("Button clicked — sending request to Gemini...");
        geminiAI.SendToGemini(req);
        
        endTurnButton.interactable = false;
        yield return new WaitUntil(() => geminiAI.latestResponse != null);
        Debug.Log(geminiAI.latestResponse);
        if (geminiAI.latestResponse != null)
        {
            string AICards = geminiAI.latestResponse.discardReturn;
            string[] AIHand = geminiAI.latestResponse.updatedHand.ToArray();
            Debug.Log("AI response");
            Debug.Log("Action = " + AICards);
            Debug.Log("Hand = " + string.Join(", ", AIHand));
            PlayAICards(AICards);
        }
        geminiAI.ResetLatestResponse();
        turn += 1;
        if (turn != 0) DrawCards(ruleDraw);
        UpdateHandUI();
        selectedCards.Clear();
        endTurnButton.interactable = true;
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
        if (rulePointsWin)
        {
            if (totalPoints > totalAIPoints) Debug.Log($"Player Wins!");
            else if (totalAIPoints > totalPoints) Debug.Log($"AI Wins!");
            else Debug.Log($"It was a tie!");
        }
        endTurnButton.interactable = false;
        foreach (Transform child in cardParent)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
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
                if (ruleReshuffle && deck.DiscardCount > 0)
                {
                    Reshuffle();
                }
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
                if (ruleReshuffle && deck.DiscardCount > 0)
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
