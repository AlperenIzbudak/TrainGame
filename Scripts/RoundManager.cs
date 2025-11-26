using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    public int cardsPerRound = 4;              // Her oyuncu kaç kart atacak
    public float playDelay = 0.5f;             // Kartlar arasındaki bekleme süresi

    public List<PlayerController> players = new List<PlayerController>();

    // Bu roundda atılan TÜM kartlar (sırası bozulmayacak)
    public List<PlannedCard> plannedCards = new List<PlannedCard>();

    int currentTurn = 0;        // 0..cardsPerRound-1
    int currentPlayerIndex = 0; // 0..players.Count-1
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
            PlayBotCard(pc);
        }
        else
        {
            Debug.Log("[RoundManager] Waiting for PLAYER to click a card...");
            // Player kartı tıklayınca CardHandDisplay -> OnHumanCardSelected çağrılacak
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

        // Şimdilik: bot desteden ilk kartı atıyor
        string cardName = deck.playerDeck[0];
        deck.playerDeck.RemoveAt(0);

        plannedCards.Add(new PlannedCard(pc, cardName));
        Debug.Log($"[RoundManager] BOT {pc.playerName} played {cardName}");

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

            bool resolved = false;

            // Kart efektini çalıştır, bittiğinde resolved = true diyelim
            CardActionResolver.Instance.ResolveCard(card, () =>
            {
                resolved = true;
            });

            // Kart gerçekten bitene kadar bekle (örn. oyuncu yön seçsin vs.)
            while (!resolved)
                yield return null;

            // Kartlar arasında ekstra gecikme
            yield return new WaitForSeconds(playDelay);
        }

        Debug.Log("[RoundManager] All cards resolved. Round finished!");
        playRoutine = null;
        // İstersen burada yeni round başlatabilirsin.
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

        // 4) Oyuncunun elini yeniden çiz (kart sayısı azaldı)
        CardHandDisplay display = pc.GetComponent<CardHandDisplay>();
        if (display != null)
            display.DisplayCards();

        // 5) Sıradaki oyuncuya geç
        AdvanceTurn();
    }

    
    
}
