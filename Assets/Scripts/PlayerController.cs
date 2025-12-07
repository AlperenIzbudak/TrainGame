using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;   // <-- Image için

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
    public int bulletsGiven = 0;
    
    [Header("Gold")]
    public int goldBars = 0;   // sahip olduğu bar sayısı
    public int credits = 0;    // toplam kredi değeri

    [Header("Character")]
    [Tooltip("Seçilen karakterin ID'si (0..5)")]
    public int characterId = -1;

    [Tooltip("Prefab üzerindeki cowboy görseli (Image)")]
    public Image characterImage;

    [Header("UI (optional)")]
    public TMP_Text debugLabel; 

    [Header("Player Name ")]
    public TMP_Text nameLabel;


    private void Start()
    {
        if (goldBars < 0) goldBars = 0;
        if (credits < 0) credits = 0;

        UpdateDebugLabel();

        Debug.Log($"{(isBot ? "BOT" : "PLAYER")} {playerName} spawned. " +
                  $"Status: {(isBot ? "Bot" : "NotBot")} (train={trainIndex}, spot={spotIndex}, roof={isOnRoof}, charId={characterId})");
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

    public void ApplyCharacter(CowboyCharacter data)
    {
        if (data == null) return;

        characterId = data.id;

        if (characterImage != null && data.sprite != null)
            characterImage.sprite = data.sprite;

        // İstersen isim label'ına da karakter adını ekleyebilirsin
        if (nameLabel != null)
            nameLabel.text = playerName; // + $" ({data.displayName})";
    }
    
    private void UpdateDebugLabel()
    {
        if (debugLabel != null)
        {
            debugLabel.text = playerName;
        }
    }
}
