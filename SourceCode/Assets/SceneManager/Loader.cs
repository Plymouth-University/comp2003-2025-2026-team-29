using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    public TextMeshProUGUI warningText;
    public GameRulesSO gameRules;

    public void LoadSceneByName(string sceneName)
    {
        if (!gameRules.rulePointsEnd && gameRules.ruleTurnLimit == 0 && !gameRules.ruleDeckout && !gameRules.ruleOutofCards)
        {
            warningText.text = "You must have a rule that ends the game enabled.";
        }
        else if (!gameRules.rulePointsWin && !gameRules.ruleLeastCardsWin)
        {
            warningText.text = "You must have a rule that decides who wins the game enabled.";
        }
        else if (!gameRules.rulePointsEnabled && (gameRules.rulePointsEnd || gameRules.rulePointsWin))
        {
            warningText.text = "You must also have a points enabled if you have points end/win enabled.";
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        } 
    }
}

