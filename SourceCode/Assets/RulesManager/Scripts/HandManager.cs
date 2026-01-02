using System;
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
    public GameObject jokerPanel; // Assign your JokerPanel here
    public TMP_Text discardTopCardText;

    // ---- Game Logic ----
    public Deck deck;
    public List<Card> playerHand = new List<Card>();
    private List<int> selectedCards = new List<int>();
    private List<Card> playedCards = new List<Card>();
    private Dictionary<Card, int> jokerValues = new Dictionary<Card, int>();

    private int currentJokerIndex = -1;

    // ---- Rules & Scoring ----
    private int totalPoints = 0;
    public int pointEndLimit = 0;
    public int ruleStartHand = 5;           //rule 1
    public int ruleDraw = 0;                //rule 2
    public int ruleMaxHand = 0;             //rule 3
    public bool rulePointsEnabled = false;   //rule 4
    public bool rulePointsEnd = false;       //rule 5
    public bool rulePointsWin = false;      //rule 6
    public bool ruleReshuffle = false;      //rule 7
    public bool ruleJoker = false;           //rule 8
    public bool rulesCard = false;           //rule 9
    public List<Rule> rules;

    void Start()
    {
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
                    ruleMaxHand = UnityEngine.Random.Range(1, 8); // set max to random 1–12
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

        // Draw starting hand
        DrawCards(ruleStartHand);

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

        float spacing = 130f;
        float startX = -((playerHand.Count - 1) * spacing) / 2;

        for (int i = 0; i < playerHand.Count; i++)
        {
            GameObject cardButton = Instantiate(cardPrefab, cardParent);

            TMP_Text tmpText = cardButton.GetComponentInChildren<TMP_Text>();
            if (tmpText != null) tmpText.text = playerHand[i].ToString();

            UnityEngine.UI.Text uiText = cardButton.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null) uiText.text = playerHand[i].ToString();

            RectTransform rt = cardButton.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * spacing, 0);
            rt.sizeDelta = new Vector2(120, 180);

            int index = i;
            cardButton.GetComponent<Button>().onClick.AddListener(() => OnCardClicked(index));
        }
    }

    void OnCardClicked(int index)
    {
        RectTransform rt = cardParent.GetChild(index).GetComponent<RectTransform>();

        if (selectedCards.Contains(index))
        {
            selectedCards.Remove(index);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0);
            rt.localScale = Vector3.one;
        }
        else
        {
            selectedCards.Add(index);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 20);
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

        // Now play all selected cards
        PlaySelectedCards();
    }

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

    void PlaySelectedCards()
    {
        playedCards.Clear();
        selectedCards.Sort();
        selectedCards.Reverse();

        foreach (int idx in selectedCards)
        {
            Card c = playerHand[idx];
            playedCards.Add(c);
            playerHand.RemoveAt(idx);
        }

        selectedCards.Clear();

        foreach (var c in playedCards)
            deck.AddToDiscard(c);

        FinishTurn();
    }

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
                    chosen.Enabled = true;
                    chosen.OnEnable?.Invoke();
                    Debug.Log($"Rule enabled: {chosen.Name}");
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
                    val = jokerValues.ContainsKey(c) ? jokerValues[c] : 25;
                }
                else if (c.Rank == Rank.Rules) val = 0;
                else
                {
                    val = GetCardValue(c);
                }
                turnPoints += val;
            }
            totalPoints += turnPoints;
            Debug.Log($"Turn points: {turnPoints}, Total points: {totalPoints}");
            UpdateDiscardTopCard();

            if (rulePointsEnd && totalPoints >= pointEndLimit)
            {
                EndGame();
            }
        }

        // Draw new cards using the fixed method
        DrawCards(ruleDraw);

        UpdateHandUI();
    }

    void UpdateDiscardTopCard()
    {
        if (discardTopCardText == null) return;

        if (deck.DiscardCount > 0)
        {
            Card topCard = deck.PeekDiscard();
            discardTopCardText.text = topCard.ToString();
        }
        else
        {
            discardTopCardText.text = "Discard pile empty";
        }
    }

    void EndGame()
    {
        Debug.Log($"Game Over!");
        endTurnButton.interactable = false;
        foreach (Transform child in cardParent)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
    }

    void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
            {
                if (ruleReshuffle && deck.DiscardCount > 0)
                {
                    deck.Reshuffle();
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
                if (c != null) playerHand.Add(c);
            }
        }
        Debug.Log($"There are {deck.Count} cards left in the deck");
    }

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
            case Rank.Rules: return 25;
            default: return 0;
        }
    }

    // ---- Classes ----
    public enum Suit { Hearts, Diamonds, Clubs, Spades, None }
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
            if (discardPile.Count == 0) return;
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

        public void Toggle()
        {
            if (Enabled) Disable();
            else Enable();
        }
    }
}
