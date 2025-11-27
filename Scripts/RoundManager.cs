using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    public int cardsPerRound = 4;              
    public float playDelay = 0.5f;
    public float botPlanningDelay = 0.5f;  
    
    public int currentRound = 1;   // 1..maxRounds
    public int maxRounds = 4;      // Oyundaki toplam round sayısı


    public List<PlayerController> players = new List<PlayerController>();
    
    public List<PlannedCard> plannedCards = new List<PlannedCard>();

    int currentTurn = 0;   
    int currentPlayerIndex = 0; 
    bool planningPhaseActive = false;

    Coroutine playRoutine;

    void Awake()
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

    // GameManager round başında bunu çağıracak
    public void BeginPlanningPhase()
    {
        plannedCards.Clear();
        currentTurn = 0;
        currentPlayerIndex = 0;
        planningPhaseActive = true;
        
        if (GameManager.Instance != null)
            GameManager.Instance.ClearCurrentCardUI();

        Debug.Log("[RoundManager] Planning phase started");
        PromptNextPlayer();
    }

    void PromptNextPlayer()
    {
        if (!planningPhaseActive)
            return;

        // Her oyuncu 4 kart attıysa planlama bitti
        if (currentTurn >= cardsPerRound)
        {
            planningPhaseActive = false;
            Debug.Log($"[RoundManager] Planning finished. Total cards: {plannedCards.Count}");

            // Kartların OYNANMA fazına geç
            StartPlayPhase();
            return;
        }

        PlayerController pc = players[currentPlayerIndex];

        if (pc.isBot)
        {
            StartCoroutine(BotPlayAfterDelay(pc));
        }
    }

    void PlayBotCard(PlayerController pc)
    {
        CardDeck deck = pc.GetComponent<CardDeck>();

        if (deck.playerDeck.Count == 0)
        {
            Debug.LogWarning($"[RoundManager] Bot {pc.playerName} has no cards left!");
            AdvanceTurn();
            return;
        }

        string cardName = deck.playerDeck[0];
        deck.playerDeck.RemoveAt(0);

        plannedCards.Add(new PlannedCard(pc, cardName));
        Debug.Log($"[RoundManager] BOT {pc.playerName} played {cardName}");

        // EKRANDA GÖSTER (Planning Phase)
        if (GameManager.Instance != null)
            GameManager.Instance.ShowCurrentCard(cardName, pc.playerName, "Planning");

        AdvanceTurn();
    }

    
    void AdvanceTurn()
    {
        currentPlayerIndex++;

        if (currentPlayerIndex >= players.Count)
        {
            currentPlayerIndex = 0;
            currentTurn++;
            Debug.Log($"[RoundManager] Turn {currentTurn + 1} started");
        }

        PromptNextPlayer();
    }
    
    public void OnDrawAndPassClicked()
    {
        // Planlama fazında değilsek buton hiçbir şey yapmasın
        if (!planningPhaseActive)
            return;

        // Şu an sırası gelen oyuncuyu bul
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
            return;

        PlayerController pc = players[currentPlayerIndex];

        // Bot sırasındaysa buton çalışmasın
        if (pc == null || pc.isBot)
            return;

        CardDeck deck = pc.GetComponent<CardDeck>();
        if (deck == null)
        {
            Debug.LogError("[RoundManager] OnDrawAndPassClicked: CardDeck not found on current player");
            return;
        }

        // Asıl işi yapan fonksiyon
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

        // 2) Oyuncunun elini yeniden çiz (2 yeni kart geldi)
        CardHandDisplay display = pc.GetComponent<CardHandDisplay>();
        if (display != null)
            display.DisplayCards();

        // 3) Bu rounddaki 4 slot'tan birini PASS olarak say
        plannedCards.Add(new PlannedCard(pc, "drawAndPass"));
        Debug.Log($"[RoundManager] PLAYER {pc.playerName} used Draw & Pass (pass this slot).");

        if (GameManager.Instance != null)
            GameManager.Instance.ShowCurrentCard("drawAndPass", pc.playerName, "Planning");
        
        // 4) Sıradaki oyuncuya geç
        AdvanceTurn();
    }
    
    // -------------------- PLAY PHASE --------------------

    public void StartPlayPhase()
    {
        Debug.Log("[RoundManager] PLAY phase started");

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
            Debug.Log($"[RoundManager] Resolving card #{i + 1}: {card.owner.playerName} -> {card.cardName}");

            // PLAY PHASE İÇİN EKRANDA GÖSTER
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
        // Round bitti, ortadaki kartı temizle
        if (GameManager.Instance != null)
            GameManager.Instance.ClearCurrentCardUI();

        Debug.Log("[RoundManager] All cards resolved. Round finished!");
        playRoutine = null;

        // >>> YENİ: round sayacını ilerlet ve gerekiyorsa yeni round başlat
        OnRoundFinished();

        
    }

    
    // Player elindeki kart butonuna tıkladığında çağrılacak
    public void OnHumanCardSelected(CardDeck deck, GameObject cardButton, string cardName)
    {
        // Planlama fazında değilsek hiçbir şey yapma
        if (!planningPhaseActive)
            return;

        // Bu kartı atan oyuncu
        PlayerController pc = deck.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogError("[RoundManager] OnHumanCardSelected: PlayerController not found on CardDeck");
            return;
        }

        // >>> YENİ: gerçekten sırası bu oyuncuda mı? <<<
        PlayerController current = GetCurrentPlanningPlayer();
        if (current == null || current != pc || pc.isBot)
        {
            // Bot sırasındayken veya başka oyuncunun sırasındayken gelen tıklamayı yok say
            Debug.LogWarning($"[RoundManager] Ignoring card click from {pc.playerName} – not their turn.");
            return;
        }

        // 1) Kartı oyuncunun el destelerinden sil
        bool removed = deck.playerDeck.Remove(cardName);
        if (!removed)
        {
            Debug.LogWarning("[RoundManager] Selected card not found in deck: " + cardName);
        }

        // 2) UI'daki butonu yok et
        if (cardButton != null)
            Destroy(cardButton);

        // 3) Bu round için planlanan kart listesine ekle
        plannedCards.Add(new PlannedCard(pc, cardName));
        Debug.Log($"[RoundManager] PLAYER {pc.playerName} played {cardName}");

        if (GameManager.Instance != null)
            GameManager.Instance.ShowCurrentCard(cardName, pc.playerName, "Planning");

        // 4) Oyuncunun elini yeniden çiz (kart sayısı azaldı)
        CardHandDisplay display = pc.GetComponent<CardHandDisplay>();
        if (display != null)
            display.DisplayCards();

        // 5) Sıradaki oyuncuya geç
        AdvanceTurn();
    }

    
    private IEnumerator BotPlayAfterDelay(PlayerController pc)
    {
        yield return new WaitForSeconds(botPlanningDelay);
        PlayBotCard(pc);
    }
    
    private PlayerController GetCurrentPlanningPlayer()
    {
        if (!planningPhaseActive) return null;
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return null;
        return players[currentPlayerIndex];
    }
    
    private void OnRoundFinished()
    {
        Debug.Log($"[RoundManager] Round {currentRound} finished.");

        currentRound++;

        if (currentRound <= maxRounds)
        {
            // Yeni round için hazırlık
            if (GameManager.Instance != null)
                GameManager.Instance.PrepareNewRound();

            // Yeni round'un planning phase'ini başlat
            BeginPlanningPhase();
        }
        else
        {
            Debug.Log("[RoundManager] Game finished. Max rounds reached.");

            if (GameManager.Instance != null)
                GameManager.Instance.ShowFinalResults();
        }
    }

}