using TMPro;
using UnityEngine;

public class ScoreboardRow : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text goldText;

    public void SetData(string playerName, int gold)
    {
        if (nameText != null) nameText.text = playerName;
        if (goldText != null) goldText.text = gold.ToString();
    }
}