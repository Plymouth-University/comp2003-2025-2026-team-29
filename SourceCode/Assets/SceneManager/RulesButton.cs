using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RulesButtonHandler : MonoBehaviour
{
    public GameRulesSO gameRules; // reference to the object that carries your rules
    public TMP_InputField pointsInputField; // input field where user types the points

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
            if (number > 0)
            {
                gameRules.rulePointsEnd = true;
                gameRules.pointEndLimit = number;
                Debug.Log($"End game by points enabled. Points to reach: {number}");
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
                if (gameRules != null) gameRules.ruleStartHand = number;
                Debug.Log("Starting hand set to: " + number);
            }
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
            if (number > 0)
            {
                if (gameRules != null) gameRules.ruleDraw = number;
                Debug.Log("Draw per turn set to: " + number);
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
            if (number > 0)
            {
                if (gameRules != null) gameRules.ruleMaxHand = number;
                Debug.Log("Max hand set to: " + number);
            }
        }
        else
        {
            Debug.Log($"Input a number.");
        }
    }

    public void EnablePoints()
    {
        if (gameRules != null) gameRules.rulePointsEnabled = !gameRules.rulePointsEnabled;
        Debug.Log("Points enabled: " + gameRules.rulePointsEnabled);
    }

    public void EnableWinByPoints()
    {
        if (gameRules != null) gameRules.rulePointsWin = !gameRules.rulePointsWin;
        Debug.Log("Win by points enabled: " + gameRules.rulePointsWin);
    }

    public void EnableReshuffle()
    {
        if (gameRules != null) gameRules.ruleReshuffle = !gameRules.ruleReshuffle;
        Debug.Log("Reshuffle enabled: " + gameRules.ruleReshuffle);
    }

    public void EnableJokers()
    {
        if (gameRules != null) gameRules.ruleJoker = !gameRules.ruleJoker;
        Debug.Log("Jokers enabled: " + gameRules.ruleJoker);
    }

    public void EnableRulesCard()
    {
        if (gameRules != null) gameRules.rulesCard = !gameRules.rulesCard;
        Debug.Log("Rules card enabled: " + gameRules.rulesCard);
    }
}

