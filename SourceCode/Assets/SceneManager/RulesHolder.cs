using UnityEngine;

[CreateAssetMenu(fileName = "GameRules", menuName = "Game/GameRules")]
public class GameRulesSO : ScriptableObject
{
    public bool ruleReshuffle = false;
    public bool ruleJoker = false;
    public bool rulesCard = false;
    public int ruleStartHand = 5;
    public int ruleDraw = 0;
    public int ruleMaxHand = 0;
    public bool rulePointsEnabled = false;
    public bool rulePointsEnd = false;
    public bool rulePointsWin = false;
    public int pointEndLimit = 0;
}

