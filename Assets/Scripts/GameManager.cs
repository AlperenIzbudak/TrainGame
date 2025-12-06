using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


[System.Serializable]
public class WagonInfo
{
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
    
    [Header("Sheriff")]
    public RectTransform sheriffUI;   // Canvas altındaki sheriff sprite'ının RectTransform'u
    public int sheriffTrainIndex = 4; // Sheriff hangi vagonda
    
    [Header("Sheriff spots (inside) w15,w25,w35,w45")]
    public RectTransform[] sheriffSpots;
    
    [Header("Current Card UI")]
    public GameObject currentCardPanel; 
    public Image currentCardImage;   // Ortadaki kart resmi
    public TMP_Text currentCardText; // Altına yazılacak metin
    
    [Header("GameCard UI")]
    public UnityEngine.UI.Image gameCardImage;
    public TMPro.TMP_Text gameCardText;
    
    [Header("Characters")]
    [Tooltip("Toplam 6 karakter: id = 0..5, sprite ve isimlerini buradan ver.")]
    public CowboyCharacter[] characters;
    

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
        
        // 2.5) Sheriff'i başlat
        InitSheriff();
        
        
        ClearCurrentCardUI();

        
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
            RoundManager.Instance.StartGameFlow();
        }
        else
        {
            Debug.LogError("[GameManager] RoundManager.Instance is null, cannot start planning phase!");
        }
    }
    
    public void ShowGameCard(string gameCardName)
    {
        // Sprite tarafı
        if (gameCardImage != null && cardDatabase != null)
        {
            var s = cardDatabase.GetGameCardSprite(gameCardName);
            if (s != null)
            {
                gameCardImage.sprite = s;
                gameCardImage.enabled = true;
                gameCardImage.preserveAspect = true;
            }
            else
            {
                // Sprite bulunamazsa, en azından image boş olsun
                gameCardImage.enabled = false;
            }
        }

        // Yazı tarafı
        if (gameCardText != null)
        {
            // Basit bir format: "Card1" veya "Card1 - MyCrazyCard"
            gameCardText.text = gameCardName;
        }

        Debug.Log("[GameManager] Showing GameCard: " + gameCardName);
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
    // Önce player için seçilen karakter ID'sini al
    int humanCharId = 0;
    if (characters != null && characters.Length > 0)
    {
        // GameSetupData.SelectedCharacterId main menüden geliyor
        int chosen = GameSetupData.selectedCharacterId;
        if (chosen < 0 || chosen >= characters.Length)
            chosen = 0; // fallback

        humanCharId = chosen;
    }

    // Botlar için kalan karakter ID'lerini havuza koy
    System.Collections.Generic.List<int> botCharPool = new System.Collections.Generic.List<int>();
    if (characters != null)
    {
        for (int i = 0; i < characters.Length; i++)
        {
            if (i == humanCharId) continue;
            botCharPool.Add(i);
        }
    }

    for (int i = 0; i < 4; i++) // 4 oyuncu
    {
        int trainIndex = 1;    
        int spotIndex = i + 1; // 1,2,3,4

        RectTransform playerUI = Instantiate(playerPrefab, canvas.transform, false);
        PlayerController pc = playerUI.GetComponent<PlayerController>();
        CardDeck deck = playerUI.GetComponent<CardDeck>();
        CardHandDisplay display = playerUI.GetComponent<CardHandDisplay>();

        // State
        pc.trainIndex = trainIndex;
        pc.spotIndex = spotIndex;
        pc.isOnRoof = false;

        // Pozisyon
        RectTransform spawnSpot = GetSpotForPlayer(pc);
        if (spawnSpot != null)
            playerUI.anchoredPosition = spawnSpot.anchoredPosition;

        // İnsan / Bot + isim
        if (i == 0)
        {
            pc.isBot = false;

            string playerName = string.IsNullOrWhiteSpace(GameSetupData.playerName)
                ? "Player"
                : GameSetupData.playerName.Trim();

            pc.SetPlayerName(playerName);
        }
        else
        {
            pc.isBot = true;
            pc.SetPlayerName("Bot " + i);
        }

        // Karakter ataması
        if (characters != null && characters.Length > 0)
        {
            CowboyCharacter chosenChar = null;

            if (i == 0)
            {
                // İnsan oyuncu için seçilen ID
                foreach (var ch in characters)
                {
                    if (ch != null && ch.id == humanCharId)
                    {
                        chosenChar = ch;
                        break;
                    }
                }
            }
            else
            {
                if (botCharPool.Count > 0)
                {
                    int poolIdx = Random.Range(0, botCharPool.Count);
                    int charId = botCharPool[poolIdx];
                    botCharPool.RemoveAt(poolIdx);

                    foreach (var ch in characters)
                    {
                        if (ch != null && ch.id == charId)
                        {
                            chosenChar = ch;
                            break;
                        }
                    }
                }
            }

            if (chosenChar != null)
            {
                pc.ApplyCharacter(chosenChar);
            }
            else
            {
                Debug.LogWarning("[GameManager] No character assigned for player index " + i);
            }
        }

        // Başlangıç gold / credits GameManager içinde başka yerde ayarlıyorsan orayla uyumlu bırak
        // Örneğin:
        // pc.goldBars = 2; // random vs. yapıyorsan o mantığı koru

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
}

    
    private void GiveStartingGold(PlayerController pc)
    {
        // Herkese 2 bar
        for (int i = 0; i < 2; i++)
        {
            int value = GetRandomGoldValue();
            pc.goldBars += 1;     // bar sayısı
            pc.AddGold(value);    // kredi
        }

        Debug.Log($"[GameManager] {pc.playerName} starting with {pc.goldBars} bars " +
                  $"and {pc.credits} credits.");
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

        // Önce gerçekten bar var mı?
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

        // Bu barın kredi değeri
        int credit = GetRandomGoldValue();

        pc.goldBars += 1;    // bar sayısı artar
        pc.AddGold(credit);  // kredi artar

        Debug.Log($"[GameManager] {pc.playerName} collected 1 gold bar worth {credit} credits. " +
                  $"Now has {pc.goldBars} bars and {pc.credits} credits.");

        if (ScoreboardManager.Instance != null)
            ScoreboardManager.Instance.RefreshScoreboard();

        return true;
    }



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

        // 1 bar kaybediyor
        pc.goldBars = Mathf.Max(0, pc.goldBars - 1);

        // Bu düşen bar için rastgele kredi kaybı (elden fazla krediyi düşmeyelim)
        int dropCredit = GetRandomGoldValue();
        if (dropCredit > pc.credits)
            dropCredit = pc.credits;

        pc.AddGold(-dropCredit);

        // Vagona 1 bar eklenir
        if (!pc.isOnRoof)
            wagon.wagonGold++;
        else
            wagon.roofGold++;

        wagon.UpdateGoldText();

        Debug.Log($"[GameManager] {pc.playerName} dropped 1 gold bar worth {dropCredit} credits " +
                  $"to {(pc.isOnRoof ? "roof" : "wagon")} of train {wagon.wagonIndex}. " +
                  $"Now has {pc.goldBars} bars and {pc.credits} credits.");

        if (ScoreboardManager.Instance != null)
            ScoreboardManager.Instance.RefreshScoreboard();

        return true;
    }


    
    // Her altın barı için rastgele kredi değeri üret
    private int GetRandomGoldValue()
    {
        // Basit örnek: 200, 250, 300'den biri
        int[] possibleValues = { 200, 250, 300 };
        int idx = Random.Range(0, possibleValues.Length);
        return possibleValues[idx];
    }
    
    private void InitSheriff()
    {
        if (sheriffUI == null)
        {
            Debug.LogWarning("[GameManager] Sheriff UI not assigned; sheriff mechanics disabled.");
            return;
        }

        sheriffTrainIndex = 4;  // oyun başında 4. vagon

        UpdateSheriffPosition();
    }

    private void UpdateSheriffPosition()
    {
        if (sheriffUI == null) return;

        RectTransform spot = GetSheriffSpot(sheriffTrainIndex);
        if (spot != null)
            sheriffUI.anchoredPosition = spot.anchoredPosition;
        else
            Debug.LogError("[GameManager] UpdateSheriffPosition: sheriff spot is null!");
    }
    
    public void MoveSheriff(int dir)
    {
        if (sheriffUI == null)
        {
            Debug.LogWarning("[GameManager] MoveSheriff called but sheriffUI is null.");
            return;
        }

        int newTrain = Mathf.Clamp(sheriffTrainIndex + dir, 1, 4);
        if (newTrain == sheriffTrainIndex)
        {
            Debug.Log("[GameManager] Sheriff cannot move further in that direction.");
            return;
        }

        sheriffTrainIndex = newTrain;
        UpdateSheriffPosition();

        if (gameObject.activeInHierarchy)
            StartCoroutine(CheckSheriffCollisionAllAfterDelay(0.5f));
    }




    private void ApplySheriffEffectInCurrentWagon()
    {
        if (RoundManager.Instance == null)
            return;

        foreach (var pc in RoundManager.Instance.players)
        {
            if (pc == null) continue;

            // Aynı vagon + İÇERDE olan oyuncular
            if (pc.trainIndex != sheriffTrainIndex) continue;
            if (pc.isOnRoof) continue;

            // 1) Oyuncunun main deck'ine 1 bullet kartı ekle
            CardDeck deck = pc.GetComponent<CardDeck>();
            if (deck != null)
            {
                deck.AddBulletCardToMainDeck();
                Debug.Log($"[GameManager] Sheriff gave 1 bullet to {pc.playerName}.");
            }

            // 2) Oyuncuyu roof'a kov
            pc.isOnRoof = true;

            RectTransform roofSpot = GetRoofSpot(pc.trainIndex, pc.spotIndex);
            RectTransform playerRect = pc.GetComponent<RectTransform>();
            if (roofSpot != null && playerRect != null)
                playerRect.anchoredPosition = roofSpot.anchoredPosition;
        }
    }

    public int GetSheriffTrainIndex()
    {
        return sheriffTrainIndex;
    }
    
    public RectTransform GetSheriffSpot(int trainIndex)
    {
        if (sheriffSpots == null || sheriffSpots.Length < 4)
        {
            Debug.LogError("[GameManager] Sheriff spots not configured correctly.");
            return null;
        }

        int idx = trainIndex - 1; // 1..4 -> 0..3
        if (idx < 0 || idx >= sheriffSpots.Length)
        {
            Debug.LogError("[GameManager] GetSheriffSpot: index out of range " + idx);
            return null;
        }

        return sheriffSpots[idx];
    }


    // Bir oyuncu hareket ettiğinde CardActionResolver burayı çağıracak
    public void OnPlayerPositionChanged(PlayerController pc)
    {
        if (pc == null) return;
        // GameObject disable ise coroutine başlamasın
        if (!gameObject.activeInHierarchy) return;

        StartCoroutine(CheckSheriffCollisionAfterDelay(pc, 0.5f));
    }

    // Sheriff hareket ettiğinde de tüm oyuncuları kontrol edelim
    private IEnumerator CheckSheriffCollisionAllAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (RoundManager.Instance == null) yield break;

        foreach (var pc in RoundManager.Instance.players)
        {
            if (pc == null) continue;
            TryApplySheriffEffectToPlayer(pc);
        }
    }


    // Belirli bir oyuncu için sheriff çarpışmasını kontrol eden coroutine
    private IEnumerator CheckSheriffCollisionAfterDelay(PlayerController pc, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (pc == null) yield break;

        TryApplySheriffEffectToPlayer(pc);
    }


    // Asıl iş burada: aynı vagondaysa bullet ver + roof'a gönder
    private void TryApplySheriffEffectToPlayer(PlayerController pc)
    {
        // Sheriff UI yoksa ya da sheriff sistemi kapalıysa bir şey yapma
        if (sheriffUI == null) return;

        // Sheriff daima içerde; oyuncu da içerdeyse ve aynı vagondaysa etkilenir
        if (pc.isOnRoof) return;
        if (pc.trainIndex != sheriffTrainIndex) return;

        // 1) Oyuncunun main deck'ine 1 bullet kartı ekle
        CardDeck deck = pc.GetComponent<CardDeck>();
        if (deck != null)
        {
            deck.AddBulletCardToMainDeck();
            Debug.Log($"[GameManager] Sheriff gave 1 bullet to {pc.playerName}.");
        }

        // 2) Oyuncuyu roof'a kov
        pc.isOnRoof = true;

        RectTransform roofSpot = GetRoofSpot(pc.trainIndex, pc.spotIndex);
        RectTransform playerRect = pc.GetComponent<RectTransform>();
        if (roofSpot != null && playerRect != null)
            playerRect.anchoredPosition = roofSpot.anchoredPosition;

        Debug.Log($"[GameManager] {pc.playerName} was sent to the roof by the Sheriff in wagon {pc.trainIndex}.");
    }

    
    public void ClearCurrentCardUI()
    {
        if (currentCardImage != null)
        {
            currentCardImage.enabled = false;
            currentCardImage.sprite = null;
        }

        if (currentCardText != null)
            currentCardText.text = "";

        // En önemlisi: paneli tamamen gizle
        if (currentCardPanel != null)
            currentCardPanel.SetActive(false);
    }

    public void ShowCurrentCard(string cardKey, string ownerName, string phaseLabel)
    {
        if (string.IsNullOrEmpty(cardKey))
        {
            ClearCurrentCardUI();
            return;
        }

        // Kart gösterilecekse paneli aç
        if (currentCardPanel != null)
            currentCardPanel.SetActive(true);

        bool isTunnelBack = (cardKey == "tunnelBack");

        // 1) Sprite ayarla
        if (currentCardImage != null && cardDatabase != null)
        {
            var sprite = cardDatabase.GetSprite(cardKey);
            if (sprite != null)
            {
                currentCardImage.sprite = sprite;
                currentCardImage.enabled = true;
                currentCardImage.preserveAspect = true;
            }
            else
            {
                currentCardImage.enabled = false;
            }
        }

        // 2) Yazı ayarla
        if (currentCardText != null)
        {
            if (isTunnelBack)
            {
                // Gerçek kart ismini saklıyoruz
                if (!string.IsNullOrEmpty(ownerName))
                    currentCardText.text = $"{ownerName}: [Tunnel]";
            }
            else
            {
                string pretty = GetPrettyCardName(cardKey);

                if (!string.IsNullOrEmpty(ownerName))
                    currentCardText.text = $"{ownerName}: {pretty}";
                else
                    currentCardText.text = pretty;
            }
        }
    }

    
    private string GetPrettyCardName(string key)
    {
        switch (key)
        {
            case "punch":            return "Punch";
            case "fire":             return "Fire";
            case "moveHorizontally": return "Move Horiz.";
            case "moveVertically":   return "Move Vert.";
            case "collect":          return "Collect";
            case "moveSherrif":      return "Move Sheriff";
            case "drawAndPass":      return "Draw & Pass";
            case "bullet":           return "Bullet";

            // Sadece UI için kullandığımız fake kart
            case "tunnelBack":       return "Hidden Card";

            default:                 return key;
        }
    }
    
    public void ShowFinalResults()
    {
        if (RoundManager.Instance == null)
        {
            Debug.LogError("[GameManager] ShowFinalResults: RoundManager.Instance is null");
            return;
        }

        var list = new List<FinalPlayerData>();

        foreach (var pc in RoundManager.Instance.players)
        {
            if (pc == null) continue;

            var data = new FinalPlayerData
            {
                name = pc.playerName,
                goldBars = pc.goldBars,
                credits = pc.credits,    
                isBot = pc.isBot,
                bulletsGiven = pc.bulletsGiven,
                characterId  = pc.characterId 
            };

            list.Add(data);
        }

        // Credits'e göre büyükten küçüğe sırala
        list.Sort((a, b) => b.credits.CompareTo(a.credits));

        FinalResultsData.Players = list;

        Debug.Log("[GameManager] Loading WinnerScene with final results...");
        SceneManager.LoadScene("WinnerScene");
    }
    
    
    public void ResetHandsForNewGameCard()
    {
        if (RoundManager.Instance == null) return;

        foreach (var pc in RoundManager.Instance.players)
        {
            if (pc == null) continue;

            CardDeck deck = pc.GetComponent<CardDeck>();
            CardHandDisplay display = pc.GetComponent<CardHandDisplay>();

            if (deck == null) continue;

            // baseDeck + bullet'lar üzerinden 6 kartı yeniden seç
            deck.GenerateRandomDeck(6);

            // Sadece insan oyuncunun elini ekranda göster
            if (!pc.isBot && display != null)
            {
                display.DisplayCards();
            }
        }

        Debug.Log("[GameManager] New hands dealt for next GameCard.");
    }




}
