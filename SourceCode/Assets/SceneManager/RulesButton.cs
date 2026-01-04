using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RulesButtonHandler : MonoBehaviour
{
    public GameRulesSO gameRules; // reference to the object that carries your rules
    public TMP_InputField pointsInputField; // input field where user types the points

    // This will be called by a button
    public void SetEndGameByPoints(int points)
    {
        if (gameRules == null)
        {
            Debug.LogError("GameRules reference not set!");
            return;
        }
        if (points > 0)
        {
            gameRules.rulePointsEnd = true;
            gameRules.pointEndLimit = points;
            Debug.Log($"End game by points enabled. Points to reach: {points}");
        }
    }

    public void SetStartingHand(int value)
    {
        if (gameRules != null) gameRules.ruleStartHand = value;
        Debug.Log("Starting hand set to: " + value);
    }

    public void SetDrawPerTurn(int value)
    {
        if (gameRules != null) gameRules.ruleDraw = value;
        Debug.Log("Draw per turn set to: " + value);
    }

    public void SetMaxHand(int value)
    {
        if (gameRules != null) gameRules.ruleMaxHand = value;
        Debug.Log("Max hand set to: " + value);
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

