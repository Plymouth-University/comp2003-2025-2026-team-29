using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Presets : MonoBehaviour
{
    public GameRulesSO gameRules;
    public TextMeshProUGUI descriptionText;

    public Button UnoRulesButton;
    public Button UnoRulesX2Button;
    public Button OneCardButton;
    public Button ResetButton;

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

    public void EnableUnoX2()
    {
        descriptionText.text = "UnoX2 rules have been applied.";
        gameRules.ruleJoker = true;
        gameRules.rulesCard = false;
        gameRules.ruleReshuffle = true;
        gameRules.ruleStartHand = 9;
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
        gameRules.rulePlayAmount = 2;
        gameRules.rulePlayMatch = true;
        gameRules.ruleDrawEarlyEnd = 1;
    }

    public void EnableOneCard()
    {
        descriptionText.text = "One card rules applied.";
        gameRules.ruleJoker = false;
        gameRules.rulesCard = false;
        gameRules.ruleReshuffle = false;
        gameRules.ruleStartHand = 1;
        gameRules.ruleDraw = 0;
        gameRules.ruleMaxHand = 0;
        gameRules.rulePointsEnabled = true;
        gameRules.rulePointsEnd = false;
        gameRules.rulePointsWin = true;
        gameRules.pointEndLimit = 0;
        gameRules.ruleDrawHand = false;
        gameRules.ruleTurnLimit = 1;
        gameRules.ruleDeckout = false;
        gameRules.ruleOutofCards = false;
        gameRules.ruleLeastCardsWin = false;
        gameRules.rulePlayAmount = 0;
        gameRules.rulePlayMatch = false;
        gameRules.ruleDrawEarlyEnd = 0;
    }

    public void ResetRules()
    {
        descriptionText.text = "Rules reset.";
        gameRules.ruleJoker = false;
        gameRules.rulesCard = false;
        gameRules.ruleReshuffle = false;
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
        gameRules.ruleOutofCards = false;
        gameRules.ruleLeastCardsWin = false;
        gameRules.rulePlayAmount = 0;
        gameRules.rulePlayMatch = false;
        gameRules.ruleDrawEarlyEnd = 0;
    }
}
