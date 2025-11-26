using UnityEngine;

public class CardSpriteDatabase : MonoBehaviour
{
    [Header("Assign these from Inspector")]
    public Sprite punchSprite;
    public Sprite fireSprite;
    public Sprite moveHorizontallySprite;
    public Sprite moveVerticallySprite;
    public Sprite collectSprite;
    public Sprite moveSherrifSprite;

    // Kart ismine göre ilgili sprite'ı döndür
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
            default:
                Debug.LogWarning("[CardSpriteDatabase] Unknown card name: " + cardName);
                return null;
        }
    }
}