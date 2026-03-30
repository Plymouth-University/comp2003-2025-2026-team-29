using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Presets : MonoBehaviour
{
    public GameRulesSO gameRules;
    public TextMeshProUGUI descriptionText;

    public Button UnoRulesButton;

    public void EnableUno()
    {
        descriptionText.text = "Uno rules have been applied.";
        gameRules.ruleJoker = true;
        gameRules.rulesCard = false;
        gameRules.ruleReshuffle = true;
        gameRules.ruleStartHand = 5;
        gameRules.ruleDraw = 0;
        gameRules.ruleMaxHand = 0;
        gameRules.rulePointsEnabled = false;
        gameRules.rulePointsEnd = false;
        gameRules.rulePointsWin = false;
        gameRules.pointEndLimit = 0;
        gameRules.ruleDrawHand = false;
        gameRules.ruleTurnLimit = 0;
        gameRules.ruleDeckout = false;
        gameRules.ruleOutofCards = true;
        gameRules.ruleLeastCardsWin = true;
        gameRules.rulePlayAmount = 1;
        gameRules.rulePlayMatch = true;
        gameRules.ruleDrawEarlyEnd = 1;
    }
}
