using UnityEngine;

[System.Serializable]
public class PlannedCard
{
    public PlayerController owner; // Kartı atan oyuncu
    public string cardName;        // "punch", "fire" vb.
    public TurnType turnType;      // Bu kartın ait olduğu turn tipi (Default / Tunnel / Reverse...)

    public PlannedCard(PlayerController owner, string cardName, TurnType turnType)
    {
        this.owner = owner;
        this.cardName = cardName;
        this.turnType = turnType;
    }
}