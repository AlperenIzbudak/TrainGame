using UnityEngine;

[System.Serializable]
public class GameCardSpriteEntry
{
    public string gameCardName;   // GameCardConfig.cardName ile birebir aynı olacak
    public Sprite sprite;         // O game card’ın görseli
}

public class CardSpriteDatabase : MonoBehaviour
{
    [Header("Assign these from Inspector")]
    public Sprite punchSprite;
    public Sprite fireSprite;
    public Sprite moveHorizontallySprite;
    public Sprite moveVerticallySprite;
    public Sprite collectSprite;
    public Sprite moveSherrifSprite;

    [Header("Special sprites")]
    public Sprite bulletSprite;        // Bullet elde görünür ama oynanamaz
    public Sprite tunnelBackSprite;    // Tunnel turundaki kapalı kart görseli

    [Header("GameCard Sprites")]
    [Tooltip("Buraya 6 giriş ekle: gameCardName = GameCardConfig.cardName ile aynı olacak.")]
    public GameCardSpriteEntry[] gameCardSprites;

    // --- NORMAL KARTLAR ---
    public Sprite GetSprite(string cardName)
    {
        Debug.Log("[CardSpriteDatabase] Sprite requested for: " + cardName);

        switch (cardName)
        {
            case "punch":
                return punchSprite;
            case "fire":
                return fireSprite;
            case "moveHorizontally":
                return moveHorizontallySprite;
            case "moveVertically":
                return moveVerticallySprite;
            case "collect":
                return collectSprite;
            case "moveSherrif":
                return moveSherrifSprite;

            case CardDeck.BulletCardKey: // "bullet"
                return bulletSprite;

            case "tunnelBack":
                return tunnelBackSprite;

            default:
                Debug.LogWarning("[CardSpriteDatabase] Unknown card name: " + cardName);
                return null;
        }
    }

    // --- GAME CARD SPRITE ---
    public Sprite GetGameCardSprite(string gameCardName)
    {
        if (gameCardSprites == null) return null;

        foreach (var entry in gameCardSprites)
        {
            if (entry != null && entry.gameCardName == gameCardName)
                return entry.sprite;
            else
            {
                Debug.LogWarning("card ismi yanlış");
            }
        }

        Debug.LogWarning("[CardSpriteDatabase] No GameCard sprite found for: " + gameCardName);
        return null;
    }
}
