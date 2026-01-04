using UnityEngine;

public class RuleChanger : MonoBehaviour
{
    public GameRulesSO gameRules; // drag your ScriptableObject here

    // ---- Jokers ----
    public void ToggleJoker() => gameRules.ruleJoker = !gameRules.ruleJoker;

    // ---- Starting hand ----
    public void SetStartHand(string value)
    {
        if (int.TryParse(value, out int count))
            gameRules.ruleStartHand = count;
    }

    // ---- Reshuffle ----
    public void ToggleReshuffle() => gameRules.ruleReshuffle = !gameRules.ruleReshuffle;

    // ---- Points enabled ----
    public void TogglePointsEnabled() => gameRules.rulePointsEnabled = !gameRules.rulePointsEnabled;

    // ---- Points to end game ----
    public void SetPointsEndLimit(string value)
    {
        if (int.TryParse(value, out int limit))
        {
            gameRules.pointEndLimit = limit;
            gameRules.rulePointsEnd = true; // also enable “end by points”
        }
    }

    // ---- Most points win ----
    public void TogglePointsWin() => gameRules.rulePointsWin = !gameRules.rulePointsWin;

    // ---- Draw each turn ----
    public void SetDrawPerTurn(string value)
    {
        if (int.TryParse(value, out int count))
            gameRules.ruleDraw = count;
    }

    // ---- Max hand ----
    public void SetMaxHand(string value)
    {
        if (int.TryParse(value, out int count))
            gameRules.ruleMaxHand = count;
    }

    // ---- Rules card ----
    public void ToggleRulesCard() => gameRules.rulesCard = !gameRules.rulesCard;
}



