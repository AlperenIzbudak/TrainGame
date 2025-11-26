using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardHandDisplay : MonoBehaviour
{
    RectTransform handPanel;
    GameObject cardButtonPrefab;
    CardSpriteDatabase cardDatabase;
    CardDeck deck;

    public void Setup(RectTransform panel, GameObject prefab, CardSpriteDatabase db, CardDeck d)
    {
        handPanel = panel;
        cardButtonPrefab = prefab;
        cardDatabase = db;
        deck = d;
    }
    public void DisplayCards()
    {
        if (handPanel == null || cardButtonPrefab == null || deck == null)
        {
            Debug.LogError("[CardHandDisplay] Setup eksik.");
            return;
        }

        // Eski kartları sil
        foreach (Transform child in handPanel)
            Destroy(child.gameObject);

        PlayerController pc = deck.GetComponent<PlayerController>();

        // ELDEN KART BUTONLARINI ÇİZ
        foreach (string card in deck.playerDeck)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab, handPanel);

            // 1) SPRITE ATA
            if (cardDatabase != null)
            {
                Sprite s = cardDatabase.GetSprite(card);
                Image img = cardObj.GetComponent<Image>();          // Button'ın Image'ı
                if (img != null && s != null)
                {
                    img.sprite = s;
                    img.preserveAspect = true;                       // İstersen
                }
            }

            // 2) İSİM YAZ
            string displayName = GetPrettyName(card);

            TMP_Text tmp = cardObj.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.text = displayName;
            else
            {
                Text uText = cardObj.GetComponentInChildren<Text>();
                if (uText != null)
                    uText.text = displayName;
            }

            // 3) BUTONU TIKLANABİLİR YAP (insan oyuncu için)
            Button btn = cardObj.GetComponent<Button>();
            if (btn != null && !pc.isBot)
            {
                string cardCopy = card;
                GameObject cardObjCopy = cardObj;

                btn.onClick.AddListener(() =>
                {
                    RoundManager.Instance.OnHumanCardSelected(deck, cardObjCopy, cardCopy);
                });
            }
        }

        // En sona Draw & Pass butonunu ekliyorsan, onu aynen bırakabilirsin;
        // istersen ona ayrı bir sprite veya sadece düz arka plan kullan.
    }




    
    

    string GetPrettyName(string key)
    {
        switch (key)
        {
            case "punch":            return "Punch";
            case "fire":             return "Fire";
            case "moveHorizontally": return "Move Horiz.";
            case "moveVertically":   return "Move Vert.";
            case "collect":          return "Collect";
            case "moveSherrif":      return "Move Sheriff";
            case "bullet":           return "Bullet";
            default:                 return key;
        }
    }
}
