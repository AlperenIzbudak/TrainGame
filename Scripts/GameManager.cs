using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class WagonInfo
{
    public string wagonName;
    public int wagonIndex;    // 1..4

    // "Wagon: X gold | Roof: Y gold"
    public TMP_Text goldText;

    public int wagonGold;     // İçerdeki altın
    public int roofGold;      // Çatıdaki altın

    public void RandomizeGold()
    {
        // 1–3 gold içeride
        wagonGold = Random.Range(1, 4);
        // 0–2 gold çatıda
        roofGold = Random.Range(0, 3);

        UpdateGoldText();
    }

    public void UpdateGoldText()
    {
        if (goldText != null)
            goldText.text = $"Wagon: {wagonGold} gold | Roof: {roofGold} gold";
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Board spots (inside) t11..t44")]
    // 16 eleman: t11, t12, t13, t14, t21, ..., t44
    public RectTransform[] boardSpots;

    [Header("Roof spots r11..r44")]
    // 16 eleman: r11, r12, r13, r14, r21, ..., r44
    public RectTransform[] roofSpots;

    [Header("Player spawn & UI")]
    public RectTransform playerPrefab;
    public Canvas canvas;

    public RectTransform handPanel;
    public GameObject cardButtonPrefab;
    public CardSpriteDatabase cardDatabase;

    [Header("Wagons & Gold")]
    public WagonInfo[] wagons;  // Inspector’da 4 eleman

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 1) Vagon ve çatı gold’larını randomla
        InitWagonsGold();

        // 2) Oyuncuları spawn et (RoundManager.Instance.RegisterPlayer(...) burada çağrılıyor)
        SpawnPlayers();

        // 3) SCOREBOARD’U KUR (satırları oluştur, paneli aç)
        if (ScoreboardManager.Instance != null && RoundManager.Instance != null)
        {
            ScoreboardManager.Instance.Setup(RoundManager.Instance.players);
        }
        else
        {
            Debug.LogError("[GameManager] Scoreboard veya RoundManager eksik, Setup çağrılamadı");
        }

        // 4) İlk round için planning phase’i başlat
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.BeginPlanningPhase();
        }
        else
        {
            Debug.LogError("[GameManager] RoundManager.Instance is null, cannot start planning phase!");
        }
    }

    
    private void InitWagonsGold()
    {
        if (wagons == null) return;

        foreach (var w in wagons)
        {
            if (w != null)
                w.RandomizeGold();
        }
    }

    // 1..4 trainIndex, 1..4 spotIndex
    public RectTransform GetSpot(int trainIndex, int spotIndex)
    {
        int index = (trainIndex - 1) * 4 + (spotIndex - 1); // 0..15

        if (boardSpots == null || index < 0 || index >= boardSpots.Length)
        {
            Debug.LogError("[GameManager] GetSpot (inside) index invalid: " + index);
            return null;
        }

        return boardSpots[index];
    }

    public RectTransform GetRoofSpot(int trainIndex, int spotIndex)
    {
        int index = (trainIndex - 1) * 4 + (spotIndex - 1); // 0..15

        if (roofSpots == null || index < 0 || index >= roofSpots.Length)
        {
            Debug.LogError("[GameManager] GetRoofSpot index invalid: " + index);
            return null;
        }

        return roofSpots[index];
    }

    // Oyuncunun içinde/çatıda olmasına göre doğru spot
    public RectTransform GetSpotForPlayer(PlayerController pc)
    {
        if (pc == null) return null;

        if (pc.isOnRoof)
            return GetRoofSpot(pc.trainIndex, pc.spotIndex);
        else
            return GetSpot(pc.trainIndex, pc.spotIndex);
    }

    void SpawnPlayers()
    {
        for (int i = 0; i < 4; i++) // 4 oyuncu
        {
            int trainIndex = 1;   // hepsi 1. vagon
            int spotIndex = i + 1; // 1,2,3,4

            // Player UI oluştur
            RectTransform playerUI = Instantiate(playerPrefab, canvas.transform, false);
            PlayerController pc = playerUI.GetComponent<PlayerController>();
            CardDeck deck = playerUI.GetComponent<CardDeck>();
            CardHandDisplay display = playerUI.GetComponent<CardHandDisplay>();

            // State
            pc.trainIndex = trainIndex;
            pc.spotIndex = spotIndex;
            pc.isOnRoof = false;  // içeride başlıyor

            // Pozisyon
            RectTransform spawnSpot = GetSpotForPlayer(pc);
            if (spawnSpot != null)
                playerUI.anchoredPosition = spawnSpot.anchoredPosition;

            // İnsan mı bot mu?
            if (i == 0)
            {
                pc.isBot = false;
                pc.SetPlayerName("Player");
            }
            else
            {
                pc.isBot = true;
                pc.SetPlayerName("Bot " + i);
            }

            // Başlangıç gold (PlayerController.Start’ta da 1 set ediliyor ama net olsun)
            if (pc.goldBars <= 0)
                pc.goldBars = 1;

            // Deste
            deck.GenerateRandomDeck(6);
            Debug.Log($"{pc.playerName} deck: {string.Join(", ", deck.playerDeck)}");
            
            // El UI
            display.Setup(handPanel, cardButtonPrefab, cardDatabase, deck);
            if (!pc.isBot)
                display.DisplayCards();

            // RoundManager’a register et
            RoundManager.Instance.RegisterPlayer(pc);
        }

        // Scoreboard Setup, Start’ta yapıyoruz
    }

    public WagonInfo GetWagon(int trainIndex)
    {
        if (wagons == null) return null;
        foreach (var w in wagons)
        {
            if (w != null && w.wagonIndex == trainIndex)
                return w;
        }
        return null;
    }

    // Collect kartı burayı çağırıyor
    public bool TryCollectGold(PlayerController pc)
    {
        if (pc == null)
        {
            Debug.LogError("[GameManager] TryCollectGold: player null");
            return false;
        }

        WagonInfo wagon = GetWagon(pc.trainIndex);
        if (wagon == null)
        {
            Debug.LogError("[GameManager] TryCollectGold: wagon not found for train " + pc.trainIndex);
            return false;
        }

        if (!pc.isOnRoof)
        {
            // İçerde
            if (wagon.wagonGold <= 0)
                return false;

            wagon.wagonGold--;
        }
        else
        {
            // Çatı
            if (wagon.roofGold <= 0)
                return false;

            wagon.roofGold--;
        }

        wagon.UpdateGoldText();
        pc.AddGold(1);

        Debug.Log($"[GameManager] {pc.playerName} collected 1 gold. Now has {pc.goldBars}.");

        if (ScoreboardManager.Instance != null)
            ScoreboardManager.Instance.RefreshScoreboard();

        return true;
    }

    // Punch kartında vurulan oyuncu gold düşürürken burayı kullanıyoruz
    public bool TryDropGoldToWagon(PlayerController pc)
    {
        if (pc == null)
        {
            Debug.LogError("[GameManager] TryDropGoldToWagon: player null");
            return false;
        }

        if (pc.goldBars <= 0)
            return false;

        WagonInfo wagon = GetWagon(pc.trainIndex);
        if (wagon == null)
        {
            Debug.LogError("[GameManager] TryDropGoldToWagon: wagon not found for train " + pc.trainIndex);
            return false;
        }

        // Oyuncudan 1 gold eksilt
        pc.AddGold(-1);

        // Dökülen gold içerde mi, çatıda mı?
        if (!pc.isOnRoof)
            wagon.wagonGold++;
        else
            wagon.roofGold++;

        wagon.UpdateGoldText();

        Debug.Log($"[GameManager] {pc.playerName} dropped 1 gold to {(pc.isOnRoof ? "roof" : "wagon")} of train {wagon.wagonIndex}.");

        if (ScoreboardManager.Instance != null)
            ScoreboardManager.Instance.RefreshScoreboard();

        return true;
    }
}
