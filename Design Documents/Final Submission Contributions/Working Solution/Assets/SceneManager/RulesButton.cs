using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RulesButtonHandler : MonoBehaviour
{
    public GameRulesSO gameRules;

    public Button jokerButton;
    public Button reshuffleButton;
    public Button rulesCardButton;
    public Button pointsButton;
    public Button pointsWinButton;
    public Button cardOutButton;
    public Button drawHandButton;
    public Button deckoutButton;
    public Button leastCardButton;
    public Button MatchCardsButton;
    public TMP_InputField pointsEndButton;
    public TMP_InputField startHandButton;
    public TMP_InputField turnLimitButton;
    public TMP_InputField drawButton;
    public TMP_InputField maxHandButton;
    public TMP_InputField playLimitButton;
    public TMP_InputField drawEarlyEndButton;
    public TextMeshProUGUI descriptionText;

    public void SetEndGameByPoints(string points)
    {
        int number;
        descriptionText.text = "The game will end when someone reaches the inputted amount of points. Requires points to be enabled.";
        if (gameRules == null)
        {
            Debug.LogError("GameRules reference not set!");
            return;
        }
        if (int.TryParse(points, out number))
        {
            if (number >= 0)
            {
                gameRules.pointEndLimit = number;
                if (number > 0)
                {
                    gameRules.rulePointsEnd = true;
                    Debug.Log($"End game by points enabled. Points to reach: {number}");
                }
                else
                {
                    gameRules.rulePointsEnd = false;
                    Debug.Log($"End game by points disabled.");
                }
                SetInputFieldColor(pointsEndButton, gameRules.rulePointsEnd);
            }
        }
        else
        {
            Debug.Log($"Input a number.");
        }
    }

    public void SetStartingHand(string value)
    {
        int number;
        descriptionText.text = "Players will start the game with the inputted amount of cards in their hand.";
        if (int.TryParse(value, out number))
        {
            if (number > 0)
            {
                if (gameRules != null)
                {
                    gameRules.ruleStartHand = number;
                    SetInputFieldColor(startHandButton, gameRules.ruleStartHand != 5);
                }
                Debug.Log("Starting hand set to: " + number);
            }
            else
                Debug.Log($"Input a number > 0.");
        }
        else
        {
            Debug.Log($"Input a number.");
        }
    }

    public void SetDrawPerTurn(string value)
    {
        int number;
        descriptionText.text = "Players will draw the inputted amount of cards at the start of each turn except the first.";
        if (int.TryParse(value, out number))
        {
            if (number >= 0)
            {
                if (gameRules != null)
                {
                    gameRules.ruleDraw = number;
                    SetInputFieldColor(drawButton, gameRules.ruleDraw > 0);
                }
                if (number > 0)
                    Debug.Log("Draw per turn set to: " + number);
                else
                    Debug.Log("Draw per turn disabled");
            }
        }
        else
        {
            Debug.Log($"Input a number.");
        }
    }

    public void SetMaxHand(string value)
    {
        int number;
        descriptionText.text = "The number of cards in the players hand cannot exceed the inputted amount.";
        if (int.TryParse(value, out number))
        {
            if (number >= 0)
            {
                if (gameRules != null)
                {
                    gameRules.ruleMaxHand = number;
                    SetInputFieldColor(maxHandButton, gameRules.ruleMaxHand > 0);
                }
                Debug.Log("Max hand set to: " + number);
            }
            else
                Debug.Log($"Input a number > 0.");
        }
        else
        {
            Debug.Log($"Input a number.");
        }
    }

    public void EnablePoints()
    {
        descriptionText.text = "Adds points to the game. You will gain points equal to the value of the played cards.";
        if (gameRules != null)
        {
            gameRules.rulePointsEnabled = !gameRules.rulePointsEnabled;
            SetButtonColor(pointsButton, gameRules.rulePointsEnabled);
        }
        Debug.Log("Points enabled: " + gameRules.rulePointsEnabled);
    }

    public void EnableWinByPoints()
    {
        descriptionText.text = "The player with the most points at the end of the game will win. Requires points to be enabled.";
        if (gameRules != null)
        {
            gameRules.rulePointsWin = !gameRules.rulePointsWin;
            SetButtonColor(pointsWinButton, gameRules.rulePointsWin);
        }
        Debug.Log("Win by points enabled: " + gameRules.rulePointsWin);
    }

    public void EnableReshuffle()
    {
        descriptionText.text = "The deck will reshuffle discarded cards when it is empty.";
        if (gameRules != null)
        {
            gameRules.ruleReshuffle = !gameRules.ruleReshuffle;
            SetButtonColor(reshuffleButton, gameRules.ruleReshuffle);
        }
        Debug.Log("Reshuffle enabled: " + gameRules.ruleReshuffle);
    }

    public void EnableJokers()
    {
        descriptionText.text = "Adds 2 joker cards to the deck. Joker cards are wild cards and can count as any card.";
        if (gameRules != null)
        {
            gameRules.ruleJoker = !gameRules.ruleJoker;
            SetButtonColor(jokerButton, gameRules.ruleJoker);
        }
        Debug.Log("Jokers enabled: " + gameRules.ruleJoker);
    }

    public void EnableRulesCard()
    {
        descriptionText.text = "Adds the rules card to the deck. The rules card adds a random rule to the game when played.";
        if (gameRules != null)
        {
            gameRules.rulesCard = !gameRules.rulesCard;
            SetButtonColor(rulesCardButton, gameRules.rulesCard);
        }
        Debug.Log("Rules card enabled: " + gameRules.rulesCard);
    }

    public void EnableDrawHand()
    {
        descriptionText.text = "Players will draw cards up to the starting hand (default 5) at the start of each turn.";
        if (gameRules != null)
        {
            gameRules.ruleDrawHand = !gameRules.ruleDrawHand;
            SetButtonColor(drawHandButton, gameRules.ruleDrawHand);
        }
        Debug.Log("Rules card enabled: " + gameRules.ruleDrawHand);
    }

    public void SetTurnLimit(string value)
    {
        int number;
        descriptionText.text = "The game will end after the set amount of turns.";
        if (int.TryParse(value, out number))
        {
            if (number >= 0)
            {
                if (gameRules != null)
                {
                    gameRules.ruleTurnLimit = number;
                    SetInputFieldColor(turnLimitButton, gameRules.ruleTurnLimit > 0);
                }
                if (number > 0)
                    Debug.Log("Turn limit set to: " + number);
                else
                    Debug.Log("Turn limit disabled");
            }
        }
        else
        {
            Debug.Log($"Input a number.");
        }
    }

    public void EnableDeckOut()
    {
        descriptionText.text = "The game will end when the deck runs out of cards.";
        if (gameRules != null)
        {
            gameRules.ruleDeckout = !gameRules.ruleDeckout;
            SetButtonColor(deckoutButton, gameRules.ruleDeckout);
        }
        Debug.Log("Deckout enabled: " + gameRules.ruleDeckout);
    }

    public void EnableOutofCards()
    {
        descriptionText.text = "The game will end when a player runs out of cards in their hand.";
        if (gameRules != null)
        {
            gameRules.ruleOutofCards = !gameRules.ruleOutofCards;
            SetButtonColor(cardOutButton, gameRules.ruleOutofCards);
        }
        Debug.Log("Out of cards enabled: " + gameRules.ruleOutofCards);
    }

    public void EnableLeastCards()
    {
        descriptionText.text = "The player with the least cards in their hand at the end of the game will win.";
        if (gameRules != null)
        {
            gameRules.ruleLeastCardsWin = !gameRules.ruleLeastCardsWin;
            SetButtonColor(leastCardButton, gameRules.ruleLeastCardsWin);
        }
        Debug.Log("Win with least cards enabled: " + gameRules.ruleLeastCardsWin);
    }

    public void SetPlayAmount(string value)
    {
        int number;
        descriptionText.text = "Players must play the set amount of cards each turn.";
        if (int.TryParse(value, out number))
        {
            if (number >= 0)
            {
                if (gameRules != null)
                {
                    gameRules.rulePlayAmount = number;
                    SetInputFieldColor(playLimitButton, gameRules.rulePlayAmount > 0);
                }
                if (number > 0)
                    Debug.Log("Card play limit set to: " + number);
                else
                    Debug.Log("Card play limit disabled");
            }
        }
        else
        {
            Debug.Log($"Input a number.");
        }
    }

    public void EnableMatchCards()
    {
        descriptionText.text = "Players can only play cards that match with the top discard card in some way.";
        if (gameRules != null)
        {
            gameRules.rulePlayMatch = !gameRules.rulePlayMatch;
            SetButtonColor(MatchCardsButton, gameRules.rulePlayMatch);
        }
        Debug.Log("Match cards enabled: " + gameRules.rulePlayMatch);
    }

    public void SetEarlyDrawAmount(string value)
    {
        int number;
        descriptionText.text = "Players can end turn without playing cards, with the penalty of drawing the set amount of cards.";
        if (int.TryParse(value, out number))
        {
            if (number >= 0)
            {
                if (gameRules != null)
                {
                    gameRules.ruleDrawEarlyEnd = number;
                    SetInputFieldColor(drawEarlyEndButton, gameRules.ruleDrawEarlyEnd > 0);
                }
                if (number > 0)
                    Debug.Log("Draw amount set to: " + number);
                else
                    Debug.Log("End turn early disabled");
            }
        }
        else
        {
            Debug.Log($"Input a number.");
        }
    }

    void SetButtonColor(Button button, bool isActive)
    {
        Color color = isActive ? Color.green : Color.white;

        ColorBlock cb = button.colors;
        cb.normalColor = color;
        cb.highlightedColor = color;
        cb.pressedColor = color;
        cb.selectedColor = color;

        button.colors = cb;
    }

    void SetInputFieldColor(TMP_InputField input, bool isActive)
    {
        Image img = input.GetComponent<Image>();
        img.color = isActive ? Color.green : Color.white;
    }

    void RefreshUI()
    {
        // Buttons
        SetButtonColor(pointsButton, gameRules.rulePointsEnabled);
        SetButtonColor(pointsWinButton, gameRules.rulePointsWin);
        SetButtonColor(reshuffleButton, gameRules.ruleReshuffle);
        SetButtonColor(jokerButton, gameRules.ruleJoker);
        SetButtonColor(rulesCardButton, gameRules.rulesCard);
        SetButtonColor(cardOutButton, gameRules.ruleOutofCards);
        SetButtonColor(drawHandButton, gameRules.ruleDrawHand);
        SetButtonColor(leastCardButton, gameRules.ruleLeastCardsWin);
        SetButtonColor(deckoutButton, gameRules.ruleDeckout);
        SetButtonColor(MatchCardsButton, gameRules.rulePlayMatch);

        // Input fields
        SetInputFieldColor(pointsEndButton, gameRules.rulePointsEnd);
        SetInputFieldColor(startHandButton, gameRules.ruleStartHand != 5);
        SetInputFieldColor(turnLimitButton, gameRules.ruleTurnLimit > 0);
        SetInputFieldColor(drawButton, gameRules.ruleDraw > 0);
        SetInputFieldColor(maxHandButton, gameRules.ruleMaxHand > 0);
        SetInputFieldColor(playLimitButton, gameRules.rulePlayAmount > 0);
        SetInputFieldColor(drawEarlyEndButton, gameRules.ruleDrawEarlyEnd > 0);
    }

    void OnEnable()
    {
        RefreshUI();
    }
}

