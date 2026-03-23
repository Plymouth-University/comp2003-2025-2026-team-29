using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RulesButtonHandler : MonoBehaviour
{
    public GameRulesSO gameRules; // reference to the object that carries your rules

    public Button jokerButton;
    public Button reshuffleButton;
    public Button rulesCardButton;
    public Button pointsButton;
    public Button pointsWinButton;
    public Button cardOutButton;
    public Button drawHandButton;
    public Button deckoutButton;
    public Button leastCardButton;
    public TMP_InputField pointsEndButton;
    public TMP_InputField startHandButton;
    public TMP_InputField turnLimitButton;
    public TMP_InputField drawButton;
    public TMP_InputField maxHandButton;
    public TMP_InputField playLimitButton;

    // This will be called by a button
    public void SetEndGameByPoints(string points)
    {
        int number;
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
        if (gameRules != null)
        {
            gameRules.rulePointsEnabled = !gameRules.rulePointsEnabled;
            SetButtonColor(pointsButton, gameRules.rulePointsEnabled);
        }
        Debug.Log("Points enabled: " + gameRules.rulePointsEnabled);
    }

    public void EnableWinByPoints()
    {
        if (gameRules != null)
        {
            gameRules.rulePointsWin = !gameRules.rulePointsWin;
            SetButtonColor(pointsWinButton, gameRules.rulePointsWin);
        }
        Debug.Log("Win by points enabled: " + gameRules.rulePointsWin);
    }

    public void EnableReshuffle()
    {
        if (gameRules != null)
        {
            gameRules.ruleReshuffle = !gameRules.ruleReshuffle;
            SetButtonColor(reshuffleButton, gameRules.ruleReshuffle);
        }
        Debug.Log("Reshuffle enabled: " + gameRules.ruleReshuffle);
    }

    public void EnableJokers()
    {
        if (gameRules != null)
        {
            gameRules.ruleJoker = !gameRules.ruleJoker;
            SetButtonColor(jokerButton, gameRules.ruleJoker);
        }
        Debug.Log("Jokers enabled: " + gameRules.ruleJoker);
    }

    public void EnableRulesCard()
    {
        if (gameRules != null)
        {
            gameRules.rulesCard = !gameRules.rulesCard;
            SetButtonColor(rulesCardButton, gameRules.rulesCard);
        }
        Debug.Log("Rules card enabled: " + gameRules.rulesCard);
    }

    public void EnableDrawHand()
    {
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
        if (gameRules != null)
        {
            gameRules.ruleDeckout = !gameRules.ruleDeckout;
            SetButtonColor(deckoutButton, gameRules.ruleDeckout);
        }
        Debug.Log("Deckout enabled: " + gameRules.ruleDeckout);
    }

    public void EnableOutofCards()
    {
        if (gameRules != null)
        {
            gameRules.ruleOutofCards = !gameRules.ruleOutofCards;
            SetButtonColor(cardOutButton, gameRules.ruleOutofCards);
        }
        Debug.Log("Out of cards enabled: " + gameRules.ruleOutofCards);
    }

    public void EnableLeastCards()
    {
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

        // Input fields (based on values)
        SetInputFieldColor(pointsEndButton, gameRules.rulePointsEnd);
        SetInputFieldColor(startHandButton, gameRules.ruleStartHand != 5);
        SetInputFieldColor(turnLimitButton, gameRules.ruleTurnLimit > 0);
        SetInputFieldColor(drawButton, gameRules.ruleDraw > 0);
        SetInputFieldColor(maxHandButton, gameRules.ruleMaxHand > 0);
        SetInputFieldColor(playLimitButton, gameRules.rulePlayAmount > 0);
    }

    void OnEnable()
    {
        RefreshUI();
    }
}

