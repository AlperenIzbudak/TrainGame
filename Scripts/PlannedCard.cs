using UnityEngine;

[System.Serializable]
public class PlannedCard
{
    public PlayerController owner; // Kartı atan oyuncu
    public string cardName;        // "punch", "fire" vb.

    public PlannedCard(PlayerController owner, string cardName)
    {
        this.owner = owner;
        this.cardName = cardName;
    }
}