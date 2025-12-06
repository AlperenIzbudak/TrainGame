using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviour
{
    [Header("State")]
    public bool isBot = false;
    public string playerName = "Player";

    // 1..4
    public int trainIndex = 1;
    // 1..4
    public int spotIndex = 1;

    // false => vagon içi, true => çatı
    public bool isOnRoof = false;
    
    [Header("Bullets")]
    public int maxBulletCount = 6;   // Toplam mermi sayısı
    public int bulletsUsed = 0;
    
    [Header("Bullet Stats")]
    public int bulletsGiven = 0;   // başkalarına verdiği mermi kartı sayısı
    
    [Header("Gold")]
    public int goldBars = 0;   // sahip olduğu bar sayısı
    public int credits = 0;    // toplam kredi değeri


    [Header("UI (optional)")]
    public TMP_Text debugLabel; // İstersen canvas üstüne koy, boş bırakabilirsin

    [Header("Player Name ")]
    public TMP_Text nameLabel;
    
    [Header("Character")]
    public int selectedCharacterId = -1;  
    
    private void Start()
    {
        // Negatif olmasın, asıl başlangıç değerlerini GameManager verecek
        if (goldBars < 0) goldBars = 0;
        if (credits < 0) credits = 0;

        UpdateDebugLabel();

        Debug.Log($"{(isBot ? "BOT" : "PLAYER")} {playerName} spawned. " +
                  $"Status: {(isBot ? "Bot" : "NotBot")} (train={trainIndex}, spot={spotIndex}, roof={isOnRoof})");
    }



    public void SetPlayerName(string name)
    {
        playerName = name;
        UpdateDebugLabel();
    }

    public void AddGold(int deltaCredits)
    {
        credits += deltaCredits;
        if (credits < 0) credits = 0;

        UpdateDebugLabel();
    }

    
    private void UpdateDebugLabel()
    {
        if (debugLabel != null)
        {
            debugLabel.text = playerName;
        }
    }

}