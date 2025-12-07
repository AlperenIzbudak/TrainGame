using System.Collections.Generic;
using UnityEngine;

public class CardDeck : MonoBehaviour
{
    
    public List<string> baseDeck = new List<string>()
    {
        "punch", "punch",
        "fire", "fire",
        "moveHorizontally", "moveHorizontally",
        "moveVertically",
        "collect", "collect",
        "moveSheriff"
    };
    
    public const string BulletCardKey = "bullet";

    // Bu oyuncunun elindeki kartlar
    public List<string> playerDeck = new List<string>();

    public void GenerateRandomDeck(int count)
    {
        playerDeck.Clear();

        // Geçici liste üzerinden çekiyoruz ki aynı kartı 10’dan fazla kez almayalım
        List<string> temp = new List<string>(baseDeck);

        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            playerDeck.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
    }
    
    public void DrawExtraCardsFromMainDeck(int count)
    {
        // baseDeck'in bir kopyasını al
        List<string> temp = new List<string>(baseDeck);

        // playerDeck'te zaten olan kartları temp'ten düş (kalanlardan çekeceğiz)
        foreach (string card in playerDeck)
        {
            int idx = temp.IndexOf(card);
            if (idx >= 0)
                temp.RemoveAt(idx);
        }

        // Kalanlardan rastgele 'count' tane çek ve eldeki desteye ekle
        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            string cardName = temp[idx];
            playerDeck.Add(cardName);
            temp.RemoveAt(idx);
        }

        Debug.Log($"[CardDeck] DrawExtraCardsFromMainDeck: drew {count} cards. New hand size = {playerDeck.Count}");
    }
    
    public void AddBulletCardToMainDeck()
    {
        baseDeck.Add(BulletCardKey);
        Debug.Log($"[CardDeck] Bullet added. New baseDeck size = {baseDeck.Count}");
    }
}