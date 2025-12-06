using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public enum TurnType
{
    Default,    // Eski normal turn
    Tunnel,     // Planlama fazında kartlar kapalı – Play fazında açılıyor
    BackToBack, // Bu turn’de her oyuncu ARKA ARKAYA 2 kart atar
    Reverse     // Sıra sadece bu turn için ters
}

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Debug UI")]
    [Tooltip("Şu anda hangi Game Card / slot oynandığını göstermek için")]
    public TMP_Text gameCardDebugText;

    [Header("General")]
    public float playDelay = 0.5f;
    public float botPlanningDelay = 0.5f;

    [Header("Game Cards")]
    [Tooltip("Inspector'dan 6 tane doldur. Her birinin içinde 3–5 TurnConfig olsun.")]
    public GameCardConfig[] allGameCards;    // 6 adet

    [Tooltip("Her oyunda rastgele seçilecek Game Card sayısı")]
    public int gameCardsToPlay = 4;

    [Header("Runtime State")]
    public List<PlayerController> players = new List<PlayerController>();
    public List<PlannedCard> plannedCards = new List<PlannedCard>();

    // --- GameCard / turn state ---
    private List<GameCardConfig> _selectedGameCards = new List<GameCardConfig>();
    private int _currentGameCardIndex = 0;

    // GameCard içindeki slotların turn tipleri
    private List<TurnType> _currentSlots = new List<TurnType>();
    private int _currentSlotIndex = 0;

    // O anda sıradaki oyuncu index’i (normal sıra için)
    private int currentPlayerIndex = 0;
    
    private int _cardsPlayedInCurrentSlotForCurrentPlayer = 0;

    private bool planningPhaseActive = false;
    
    // Reverse slot için cache’lediğimiz ters sıra
    private List<PlayerController> _playersReversed = new List<PlayerController>();

    private Coroutine playRoutine;

    // ----------------- Config tipleri -----------------
    [System.Serializable]
    public class TurnConfig
    {
        public TurnType turnType = TurnType.Default;
    }

    [System.Serializable]
    public class GameCardConfig
    {
        public string cardName = "GameCard";

        [Tooltip("Bu Game Card içindeki turnler (3–5 arası önerilir)")]
        public List<TurnConfig> turns = new List<TurnConfig>();
    }

    // ----------------- Mono -----------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    // GameManager her spawn ettiği oyuncuyu buraya kaydedecek
    public void RegisterPlayer(PlayerController pc)
    {
        players.Add(pc);
    }

    // ----------------- GAME CARD AKIŞI -----------------

    /// <summary> Oyunun başında çağır: 4 random GameCard seç ve birincisini başlat. </summary>
    public void StartGameFlow()
    {
        // 1) Seçilecekleri temizle
        _selectedGameCards.Clear();

        // 2) allGameCards içinden random gameCardsToPlay kadar seç
        List<GameCardConfig> temp = new List<GameCardConfig>(allGameCards);
        for (int i = 0; i < gameCardsToPlay && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            _selectedGameCards.Add(temp[idx]);
            temp.RemoveAt(idx);
        }

        if (_selectedGameCards.Count == 0)
        {
            Debug.LogError("[RoundManager] Hiç GameCard seçilmedi!");
            return;
        }

        // 3) Reverse için ters oyuncu listesi
        _playersReversed = new List<PlayerController>(players);
        _playersReversed.Reverse();

        _currentGameCardIndex = 0;
        StartGameCard(_selectedGameCards[_currentGameCardIndex]);
    }

    /// <summary> Tek bir GameCard'ı başlat. Turn listesini hazırlar ve planning'i açar. </summary>
    private void StartGameCard(GameCardConfig config)
    {
        Debug.Log($"[RoundManager] Starting GameCard: {config.cardName}");

        // Debug text formatını yazan fonksiyon vs...
        string line = BuildGameCardDescription(config, _currentGameCardIndex);
        UpdateGameCardDebug(line);

        // >>> BURAYA EKLE <<<
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowGameCard(config.cardName);
        }

        _currentSlots.Clear();

        foreach (var t in config.turns)
        {
            _currentSlots.Add(t.turnType);
        }

        if (_currentSlots.Count == 0)
        {
            Debug.LogWarning("[RoundManager] GameCard'ın slotu yok, atlıyorum.");
            GoToNextGameCard();
            return;
        }

        _currentSlotIndex = 0;
        _cardsPlayedInCurrentSlotForCurrentPlayer = 0;

        BeginPlanningPhaseForCurrentGameCard();
    }


    private void GoToNextGameCard()
    {
        _currentGameCardIndex++;

        if (_currentGameCardIndex >= _selectedGameCards.Count)
        {
            Debug.Log("[RoundManager] All GameCards finished. Game over!");

            if (GameManager.Instance != null)
                GameManager.Instance.ShowFinalResults();

            return;
        }

        // Yeni GameCard’a geçmeden önce herkese 6 yeni kart dağıt
        if (GameManager.Instance != null)
            GameManager.Instance.ResetHandsForNewGameCard();

        StartGameCard(_selectedGameCards[_currentGameCardIndex]);
    }

    private PlayerController GetExpectedPlanningPlayer()
    {
        if (!planningPhaseActive) return null;
        if (_currentSlotIndex < 0 || _currentSlotIndex >= _currentSlots.Count) return null;
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return null;

        TurnType slotType = _currentSlots[_currentSlotIndex];
        bool reverse = (slotType == TurnType.Reverse);

        return reverse ? _playersReversed[currentPlayerIndex] : players[currentPlayerIndex];
    }

    private string GetTurnTypeName(TurnType t)
    {
        switch (t)
        {
            case TurnType.Default:    return "Default";
            case TurnType.Tunnel:     return "Tunnel";
            case TurnType.BackToBack: return "BackToBack";
            case TurnType.Reverse:    return "Reverse";
            default:                  return t.ToString();
        }
    }

    private string BuildGameCardDescription(GameCardConfig config, int cardIndex)
    {
        if (config == null) return "";

        StringBuilder sb = new StringBuilder();

        // Başlık: Card 1:
        sb.Append($"Card {cardIndex + 1}: ");

        // Turnleri sırayla yaz
        for (int i = 0; i < config.turns.Count; i++)
        {
            TurnType t = config.turns[i].turnType;
            sb.Append($"{i + 1}-{GetTurnTypeName(t)}");

            if (i < config.turns.Count - 1)
                sb.Append("  ");
        }

        return sb.ToString();
    }

    // ----------------- PLANNING PHASE -----------------

    public void BeginPlanningPhaseForCurrentGameCard()
    {
        plannedCards.Clear();

        _currentSlotIndex = 0;
        currentPlayerIndex = 0;
        _cardsPlayedInCurrentSlotForCurrentPlayer = 0;
        planningPhaseActive = true;

        Debug.Log($"[RoundManager] Planning phase started for GameCard #{_currentGameCardIndex + 1} with {_currentSlots.Count} slots.");

        PromptNextPlayer();
    }


    private void PromptNextPlayer()
    {
        if (!planningPhaseActive)
            return;

        // Bütün turnler bitti mi?
        if (_currentSlotIndex >= _currentSlots.Count)
        {
            planningPhaseActive = false;
            Debug.Log($"[RoundManager] Planning finished for GameCard #{_currentGameCardIndex + 1}. Total planned cards: {plannedCards.Count}");
            StartPlayPhaseForCurrentGameCard();
            return;
        }

        TurnType slotType = _currentSlots[_currentSlotIndex];

        PlayerController pc = GetExpectedPlanningPlayer();
        if (pc == null)
        {
            Debug.LogError("[RoundManager] PromptNextPlayer: expected player is null!");
            return;
        }

        if (pc.isBot)
        {
            PlayBotCardForSlot(pc, slotType);
        }
        else
        {
            Debug.Log($"[RoundManager] Waiting for PLAYER {pc.playerName} to play (turn {_currentSlotIndex}, type={slotType})...");
            // İnsan oyuncu kart butonuna bastığında OnHumanCardSelected çalışacak.
        }
    }

    /// <summary> Botun bu turn için kart atmasını sağlar. </summary>
    private void PlayBotCardForSlot(PlayerController pc, TurnType slotType)
    {
        StartCoroutine(BotPlayAfterDelay(pc, slotType));
    }

    private IEnumerator BotPlayAfterDelay(PlayerController pc, TurnType slotType)
    {
        yield return new WaitForSeconds(botPlanningDelay);

        CardDeck deck = pc.GetComponent<CardDeck>();
        if (deck == null)
        {
            Debug.LogError("[RoundManager] BotPlayAfterDelay: CardDeck yok.");
            AdvanceTurn(slotType);
            yield break;
        }

        if (deck.playerDeck.Count == 0)
        {
            Debug.LogWarning($"[RoundManager] Bot {pc.playerName} has no cards left!");
            AdvanceTurn(slotType);
            yield break;
        }

        string cardName = deck.playerDeck[0];
        deck.playerDeck.RemoveAt(0);

        plannedCards.Add(new PlannedCard(pc, cardName, slotType));
        Debug.Log($"[RoundManager] BOT {pc.playerName} played {cardName} in slot {_currentSlotIndex} ({slotType})");

        if (GameManager.Instance != null)
        {
            // Tunnel turunda kartı kapalı göster
            string uiKey = (slotType == TurnType.Tunnel) ? "tunnelBack" : cardName;
            GameManager.Instance.ShowCurrentCard(uiKey, pc.playerName, "Planning");
        }

        AdvanceTurn(slotType);
    }
    
    private void AdvanceTurn(TurnType slotType)
    {
        // Bu slotta BU oyuncunun atması gereken kart sayısı
        int requiredCards = (slotType == TurnType.BackToBack) ? 2 : 1;

        _cardsPlayedInCurrentSlotForCurrentPlayer++;

        // BackToBack ise aynı oyuncu 2 kart atana kadar sıra ona ait
        if (_cardsPlayedInCurrentSlotForCurrentPlayer < requiredCards)
        {
            Debug.Log($"[RoundManager] BackToBack: Same player plays another card in slot {_currentSlotIndex}.");
            // currentPlayerIndex değiştirmeden tekrar onu bekliyoruz
            PromptNextPlayer();
            return;
        }

        // Bu oyuncu bu slot için kartlarını bitirdi
        _cardsPlayedInCurrentSlotForCurrentPlayer = 0;
        currentPlayerIndex++;

        // Bu slottaki tüm oyuncular kart attı mı?
        if (currentPlayerIndex >= players.Count)
        {
            currentPlayerIndex = 0;
            _currentSlotIndex++;
            Debug.Log($"[RoundManager] Next slot started: {_currentSlotIndex + 1}/{_currentSlots.Count}");
        }

        PromptNextPlayer();
    }
    
    // Draw & Pass butonu
    public void OnDrawAndPassClicked()
    {
        if (!planningPhaseActive)
            return;

        PlayerController pc = GetExpectedPlanningPlayer();
        if (pc == null || pc.isBot)
            return;

        CardDeck deck = pc.GetComponent<CardDeck>();
        if (deck == null)
        {
            Debug.LogError("[RoundManager] OnDrawAndPassClicked: CardDeck not found on current player");
            return;
        }

        OnDrawAndPassSelected(deck);
    }

    public void OnDrawAndPassSelected(CardDeck deck)
    {
        if (!planningPhaseActive)
            return;

        PlayerController pc = deck.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogError("[RoundManager] OnDrawAndPassSelected: PlayerController null");
            return;
        }

        // 1) Eldeki desteye main deck'ten 2 yeni kart ekle
        deck.DrawExtraCardsFromMainDeck(2);

        // 2) El UI'ını yenile
        CardHandDisplay display = pc.GetComponent<CardHandDisplay>();
        if (display != null)
            display.DisplayCards();

        // 3) Bu slotu "drawAndPass" olarak işaretle
        TurnType slotType = _currentSlots[_currentSlotIndex];
        plannedCards.Add(new PlannedCard(pc, "drawAndPass", slotType));
        Debug.Log($"[RoundManager] PLAYER {pc.playerName} used Draw & Pass (slot {_currentSlotIndex}, type={slotType}).");

        if (GameManager.Instance != null)
        {
            // Tunnel'da yine kapalı kart gösteriyoruz
            string uiKey = (slotType == TurnType.Tunnel) ? "tunnelBack" : "drawAndPass";
            GameManager.Instance.ShowCurrentCard(uiKey, pc.playerName, "Planning");
        }

        AdvanceTurn(slotType);
        
    }

    // İnsan oyuncu kart seçtiğinde
    public void OnHumanCardSelected(CardDeck deck, GameObject cardButton, string cardName)
    {
        if (!planningPhaseActive)
            return;

        PlayerController pc = deck.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogError("[RoundManager] OnHumanCardSelected: PlayerController not found on CardDeck");
            return;
        }

        // Sıra bu oyuncuda mı?
        PlayerController expected = GetExpectedPlanningPlayer();
        if (expected == null || expected != pc)
        {
            Debug.Log($"[RoundManager] OnHumanCardSelected: It's not {pc.playerName}'s turn. Ignoring click.");
            return;
        }

        bool removed = deck.playerDeck.Remove(cardName);
        if (!removed)
            Debug.LogWarning("[RoundManager] Selected card not found in deck: " + cardName);

        if (cardButton != null)
            Destroy(cardButton);

        TurnType slotType = _currentSlots[_currentSlotIndex];
        plannedCards.Add(new PlannedCard(pc, cardName, slotType));
        Debug.Log($"[RoundManager] PLAYER {pc.playerName} played {cardName} in slot {_currentSlotIndex} ({slotType})");

        if (GameManager.Instance != null)
        {
            string uiKey = (slotType == TurnType.Tunnel) ? "tunnelBack" : cardName;
            GameManager.Instance.ShowCurrentCard(uiKey, pc.playerName, "Planning");
        }

// Ekranı güncelle
        CardHandDisplay display = pc.GetComponent<CardHandDisplay>();
        if (display != null)
            display.DisplayCards();

        AdvanceTurn(slotType);

    }

    // ----------------- PLAY PHASE -----------------

    public void StartPlayPhaseForCurrentGameCard()
    {
        Debug.Log($"[RoundManager] PLAY phase started for GameCard #{_currentGameCardIndex + 1}");

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayAllCardsRoutine());
    }

    private IEnumerator PlayAllCardsRoutine()
    {
        if (plannedCards.Count == 0)
        {
            Debug.LogWarning("[RoundManager] No planned cards to play.");
            yield break;
        }

        for (int i = 0; i < plannedCards.Count; i++)
        {
            PlannedCard card = plannedCards[i];
            Debug.Log($"[RoundManager] Resolving card #{i + 1}: {card.owner.playerName} -> {card.cardName} ({card.turnType})");

            // TUNNEL: kartlar artık Play fazında açılıyor, o yüzden burada gösteriyoruz
            if (GameManager.Instance != null)
                GameManager.Instance.ShowCurrentCard(card.cardName, card.owner.playerName, "Play");

            bool resolved = false;

            CardActionResolver.Instance.ResolveCard(card, () =>
            {
                resolved = true;
            });

            while (!resolved)
                yield return null;

            yield return new WaitForSeconds(playDelay);
        }

        Debug.Log("[RoundManager] All cards resolved for this GameCard.");
        playRoutine = null;

        GoToNextGameCard();
    }

    private void UpdateGameCardDebug(string msg)
    {
        if (gameCardDebugText != null)
        {
            gameCardDebugText.text = msg;
        }
    }
}
