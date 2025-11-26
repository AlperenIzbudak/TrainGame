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
    public int maxBulletCount = 6;   // Toplam kaç tane bullet'ı var
    public int bulletsUsed = 0;      // Kaç tanesini ateşledi (hedefe verdi)

    [Header("Gold")]
    public int goldBars = 0;

    [Header("UI (optional)")]
    public TMP_Text debugLabel; // İstersen canvas üstüne koy, boş bırakabilirsin

    [Header("Player Name ")]
    public TMP_Text nameLabel;
    private void Start()
    {
        if (goldBars <= 0)
            goldBars = 1;

        UpdateDebugLabel();

        Debug.Log($"{(isBot ? "BOT" : "PLAYER")} {playerName} spawned. " + $"Status: {(isBot ? "Bot" : "NotBot")} (train={trainIndex}, spot={spotIndex}, roof={isOnRoof})");
    }

    public void SetPlayerName(string name)
    {
        playerName = name;
        UpdateDebugLabel();
    }

    public void AddGold(int delta)
    {
        goldBars += delta;
        if (goldBars < 0) goldBars = 0;

        UpdateDebugLabel();
    }
    
    private void UpdateDebugLabel()
    {
        if (debugLabel != null)
        {
            debugLabel.text = $"{playerName}\nT{trainIndex} S{spotIndex}\n" +
                              (isOnRoof ? "Roof" : "Inside") + $"\nGold: {goldBars}";
        }
    }
}