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

        foreach (string card in deck.playerDeck)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab, handPanel);

            // 1) SPRITE ATA
            if (cardDatabase != null)
            {
                Sprite s = cardDatabase.GetSprite(card);

                // PREFAB HİYERARŞİSİNE GÖRE DOĞRUDAN "CardImage" ÇOCUĞUNU BUL
                Transform imgTr = cardObj.transform.Find("CardImage");
                if (imgTr != null)
                {
                    Image img = imgTr.GetComponent<Image>();
                    if (img != null && s != null)
                    {
                        img.sprite = s;
                        img.preserveAspect = true;
                    }
                    else
                    {
                        if (img == null)
                            Debug.LogWarning("[CardHandDisplay] CardImage üzerinde Image component yok.");
                        if (s == null)
                            Debug.LogWarning("[CardHandDisplay] Sprite bulunamadı: " + card);
                    }
                }
                else
                {
                    Debug.LogWarning("[CardHandDisplay] CardPrefab içinde 'CardImage' child'ını bulamadım.");
                }
            }

            // 2) İSİM YAZ
            string displayName = GetPrettyName(card);
            TMP_Text tmp = cardObj.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.text = displayName;

            // 3) BUTON TIKLAMA
            // 3) BUTONU TIKLANABİLİR YAP (insan oyuncu için, BULLET hariç)
            Button btn = cardObj.GetComponent<Button>();
            bool isBullet = (card == CardDeck.BulletCardKey);   // "bullet"

            if (btn != null)
            {
                // Bullet veya bot ise tıklanamaz
                if (pc.isBot || isBullet)
                {
                    btn.interactable = false;
                    // Listener eklemiyoruz
                }
                else
                {
                    btn.interactable = true;

                    string cardCopy = card;
                    GameObject cardObjCopy = cardObj;

                    btn.onClick.AddListener(() =>
                    {
                        RoundManager.Instance.OnHumanCardSelected(deck, cardObjCopy, cardCopy);
                    });
                }
            }

        }
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
