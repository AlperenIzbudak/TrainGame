using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardActionResolver : MonoBehaviour
{
    public static CardActionResolver Instance { get; private set; }

    [Header("UI for Move Horizontally choices")]
    public GameObject horizontalChoicePanel;
    public Button leftButton;
    public Button rightButton;

    [Header("UI for Collect")]
    public GameObject collectPanel;
    public Button collectButton;
    public TMP_Text collectMessageText;

    [Header("UI for Punch target selection")]
    public GameObject punchPanel;
    public Transform punchButtonsParent;
    public Button punchButtonPrefab;
    public TMP_Text punchMessageText;
    
    [Header("UI for Move Vertically choices")]
    public GameObject verticalChoicePanel;
    public Button verticalButton;

    private PlannedCard _currentCard;
    private Action _onCardResolved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (horizontalChoicePanel != null) horizontalChoicePanel.SetActive(false);
        if (collectPanel != null) collectPanel.SetActive(false);
        if (punchPanel != null) punchPanel.SetActive(false);
        if (verticalChoicePanel != null) verticalChoicePanel.SetActive(false);
    }

    public void ResolveCard(PlannedCard card, Action onFinished)
    {
        _currentCard = card;
        _onCardResolved = onFinished;

        Debug.Log($"[CardActionResolver] Resolving card {card.cardName} of {card.owner.playerName}");

        switch (card.cardName)
        {
            case "moveHorizontally":
                HandleMoveHorizontally(card);
                break;

            case "collect":
                HandleCollect(card);
                break;

            case "punch":
                HandlePunch(card);
                break;

            case "moveVertically":
                HandleMoveVertically(card);
                break;

            case "fire":
                HandleFire(card);
                break;

            case "drawAndPass":
                HandleDrawAndPass(card);
                break;

            case "moveSherrif":
                HandleMoveSherrif(card);
                break;

            default:
                Debug.LogWarning("[CardActionResolver] Unknown card: " + card.cardName);
                FinishNow();
                break;
        }
    }
    
    // ==================== MOVE HORIZONTALLY ====================

    private void HandleMoveHorizontally(PlannedCard card)
    {
        PlayerController pc = card.owner;
        int train = pc.trainIndex;

        bool canLeft = train > 1;
        bool canRight = train < 4; // 4 vagon varsayımı

        // BOT
        if (pc.isBot)
        {
            int dir = DecideBotHorizontalDirection(canLeft, canRight);
            ApplyHorizontalMove(pc, dir);
            FinishNow();
            return;
        }

        // PLAYER
        if (horizontalChoicePanel == null || leftButton == null || rightButton == null)
        {
            Debug.LogError("[CardActionResolver] Horizontal choice UI not assigned!");
            FinishNow();
            return;
        }

        horizontalChoicePanel.SetActive(true);

        leftButton.onClick.RemoveAllListeners();
        rightButton.onClick.RemoveAllListeners();

        leftButton.gameObject.SetActive(canLeft);
        rightButton.gameObject.SetActive(canRight);

        if (canLeft)
        {
            leftButton.onClick.AddListener(() =>
            {
                ApplyHorizontalMove(pc, -1);
                horizontalChoicePanel.SetActive(false);
                FinishNow();
            });
        }

        if (canRight)
        {
            rightButton.onClick.AddListener(() =>
            {
                ApplyHorizontalMove(pc, +1);
                horizontalChoicePanel.SetActive(false);
                FinishNow();
            });
        }
    }

    private int DecideBotHorizontalDirection(bool canLeft, bool canRight)
    {
        if (canLeft && !canRight) return -1;
        if (!canLeft && canRight) return +1;
        return UnityEngine.Random.value < 0.5f ? -1 : +1;
    }

    private void ApplyHorizontalMove(PlayerController pc, int dir)
    {
        int newTrain = pc.trainIndex + dir;
        newTrain = Mathf.Clamp(newTrain, 1, 4);

        pc.trainIndex = newTrain;

        RectTransform targetSpot = pc.isOnRoof
            ? GameManager.Instance.GetRoofSpot(pc.trainIndex, pc.spotIndex)
            : GameManager.Instance.GetSpot(pc.trainIndex, pc.spotIndex);

        if (targetSpot == null)
        {
            Debug.LogError("[CardActionResolver] Target spot is null!");
            return;
        }

        RectTransform playerRect = pc.GetComponent<RectTransform>();
        if (playerRect != null)
            playerRect.anchoredPosition = targetSpot.anchoredPosition;
        Debug.Log($"[CardActionResolver] {pc.playerName} moved horizontally to train {pc.trainIndex}, spot {pc.spotIndex}, roof={pc.isOnRoof}");
        
        GameManager.Instance.OnPlayerPositionChanged(pc);
    }

    // ==================== COLLECT ====================

    private void HandleCollect(PlannedCard card)
    {
        PlayerController pc = card.owner;

        // BOT → direkt dene (otomatik collect sadece bot için)
        if (pc.isBot)
        {
            bool collected = GameManager.Instance.TryCollectGold(pc);
            if (!collected)
                Debug.Log("[CardActionResolver] Bot tried to collect but no gold.");
            FinishNow();
            return;
        }

        // PLAYER → UI zorunlu (butonsuz collect yok!)
        if (collectPanel == null || collectButton == null || collectMessageText == null)
        {
            Debug.LogError("[CardActionResolver] Collect UI not assigned!");
            FinishNow();
            return;
        }

        WagonInfo wagon = GameManager.Instance.GetWagon(pc.trainIndex);
        if (wagon == null)
        {
            Debug.LogError("[CardActionResolver] Collect: wagon not found!");
            FinishNow();
            return;
        }

        bool hasGold = !pc.isOnRoof ? (wagon.wagonGold > 0) : (wagon.roofGold > 0);

        collectPanel.SetActive(true);
        collectButton.onClick.RemoveAllListeners();

        if (hasGold)
        {
            // Altın varsa butonla toplama
            collectButton.gameObject.SetActive(true);
            collectMessageText.text = "Collect 1 gold bar";

            collectButton.onClick.AddListener(() =>
            {
                bool ok = GameManager.Instance.TryCollectGold(pc);
                if (!ok)
                {
                    Debug.LogWarning("[CardActionResolver] Collect button pressed but no gold (race condition).");
                }

                collectPanel.SetActive(false);
                FinishNow();
            });
        }
        else
        {
            // Altın yoksa sadece mesaj
            collectButton.gameObject.SetActive(false);
            collectMessageText.text = "No gold bars";

            StartCoroutine(ShowMessageAndFinish(collectPanel, 0.5f));
        }
    }

    // ==================== PUNCH ====================

    private void HandlePunch(PlannedCard card)
    {
        PlayerController attacker = card.owner;

        // Aynı vagon + aynı katmanda (roof / iç) hedefleri bul
        List<PlayerController> targets = new List<PlayerController>();
        foreach (var p in RoundManager.Instance.players)
        {
            // Aynı gerçek oyuncuyu tamamen ele (referans + isim)
            if (p == attacker) continue;
            if (p.playerName == attacker.playerName) continue;

            // Aynı vagon + aynı katman (roof / iç) olsun, spotIndex önemli değil
            if (p.trainIndex == attacker.trainIndex &&
                p.isOnRoof == attacker.isOnRoof)
            {
                targets.Add(p);
            }
        }


        if (punchPanel == null || punchButtonsParent == null || punchButtonPrefab == null || punchMessageText == null)
        {
            Debug.LogError("[CardActionResolver] Punch UI not fully assigned!");
            FinishNow();
            return;
        }

        // Kimse yok
        if (targets.Count == 0)
        {
            if (attacker.isBot)
            {
                Debug.Log("[CardActionResolver] Punch: no one at wagon.");
                FinishNow();
                return;
            }
            
            punchPanel.SetActive(true);
            punchMessageText.text = "No one at wagon";
            ClearPunchButtons();
            StartCoroutine(ShowMessageAndFinish(punchPanel, 0.5f));
            return;
        }

        // BOT → rastgele hedef seç
        if (attacker.isBot)
        {
            PlayerController target = targets[UnityEngine.Random.Range(0, targets.Count)];
            ResolvePunchOnTarget(attacker, target, showUI: false);
            FinishNow();
            return;
        }
        
        punchPanel.SetActive(true);
        punchMessageText.text = "Choose a player to punch:";
        ClearPunchButtons();

        foreach (var t in targets)
        {
            PlayerController captured = t;
            Button btn = Instantiate(punchButtonPrefab, punchButtonsParent);
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = captured.playerName;

            btn.onClick.AddListener(() =>
            {
                ResolvePunchOnTarget(attacker, captured, showUI: true);
            });
        }
    }

    private void ResolvePunchOnTarget(PlayerController attacker, PlayerController target, bool showUI)
    {
        bool hadGold = target.goldBars > 0;

        if (hadGold)
        {
            GameManager.Instance.TryDropGoldToWagon(target);
        }

        if (showUI)
        {
            // UI mesaj moduna al
            ClearPunchButtons();

            if (punchMessageText != null)
            {
                if (hadGold)
                    punchMessageText.text = $"{attacker.playerName} punched {target.playerName}!";
                else
                    punchMessageText.text = $"{target.playerName} has no gold!";
            }

            StartCoroutine(ShowMessageAndFinish(punchPanel, 0.5f));
        }
        else
        {
            // Bot senaryosu
            Debug.Log($"[CardActionResolver] {attacker.playerName} punched {target.playerName}. HadGold={hadGold}");
        }
    }

    private void ClearPunchButtons()
    {
        if (punchButtonsParent == null) return;
        for (int i = punchButtonsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(punchButtonsParent.GetChild(i).gameObject);
        }
    }

    // ==================== MOVE VERTICALLY ====================
    
    private void HandleMoveVertically(PlannedCard card)
    {
        PlayerController pc = card.owner;

        // BOT → direkt yukarı / aşağı
        if (pc.isBot)
        {
            ApplyVerticalMove(pc);
            FinishNow();
            return;
        }

        // PLAYER → UI ile seçim
        if (verticalChoicePanel == null || verticalButton == null)
        {
            Debug.LogError("[CardActionResolver] Vertical choice UI not assigned!");
            // UI yoksa fallback olarak yine direkt hareket etsin
            ApplyVerticalMove(pc);
            FinishNow();
            return;
        }

        verticalChoicePanel.SetActive(true);

        // Buton yazısını konuma göre ayarla
        bool currentlyOnRoof = pc.isOnRoof;
        TMP_Text btnText = verticalButton.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            btnText.text = currentlyOnRoof ? "go inside" : "go roof";
        }

        // Eski dinleyicileri temizle
        verticalButton.onClick.RemoveAllListeners();

        // Yeni tıklama davranışı
        verticalButton.onClick.AddListener(() =>
        {
            ApplyVerticalMove(pc);
            verticalChoicePanel.SetActive(false);
            FinishNow();
        });
    }
    
    private void ApplyVerticalMove(PlayerController pc)
    {
        // Katman değiştir (roof / iç)
        pc.isOnRoof = !pc.isOnRoof;

        RectTransform targetSpot = GameManager.Instance.GetSpotForPlayer(pc);
        if (targetSpot == null)
        {
            Debug.LogError("[CardActionResolver] MoveVertically: targetSpot null");
            return;
        }

        RectTransform playerRect = pc.GetComponent<RectTransform>();
        if (playerRect != null)
            playerRect.anchoredPosition = targetSpot.anchoredPosition;

        Debug.Log($"[CardActionResolver] {pc.playerName} moved vertically. roof={pc.isOnRoof}");
        
        GameManager.Instance.OnPlayerPositionChanged(pc);
        
    }
    
    // ==================== FIRE ====================

    private void HandleFire(PlannedCard card)
    {
        PlayerController attacker = card.owner;

        // Saldıranın mermisi kaldı mı?
        if (attacker.bulletsUsed >= attacker.maxBulletCount)
        {
            Debug.Log($"[CardActionResolver] {attacker.playerName} has no bullets left. Fire does nothing.");
            FinishNow();
            return;
        }

        // FIRE: İçerdeyken eski kural, çatıda iken tüm çatıyı görebiliyor
        List<PlayerController> targets = new List<PlayerController>();

        bool attackerOnRoof = attacker.isOnRoof;

        foreach (var p in RoundManager.Instance.players)
        {
            if (p == attacker) continue;
            if (p.playerName == attacker.playerName) continue;

            if (attackerOnRoof)
            {
                // Çatıdaysa → hangi vagon olursa olsun, çatıda olan herkesi vurabilir
                if (p.isOnRoof)
                {
                    targets.Add(p);
                }
            }
            else
            {
                // İçerideyse → eski kural: aynı vagon + iç kısım
                if (p.trainIndex == attacker.trainIndex && !p.isOnRoof)
                {
                    targets.Add(p);
                }
            }
        }


        if (punchPanel == null || punchButtonsParent == null || punchButtonPrefab == null || punchMessageText == null)
        {
            Debug.LogError("[CardActionResolver] Fire UI (punchPanel) not fully assigned!");
            FinishNow();
            return;
        }

        // Kimse yok
        if (targets.Count == 0)
        {
            if (attacker.isBot)
            {
                Debug.Log("[CardActionResolver] Fire: no target at wagon.");
                FinishNow();
                return;
            }

            punchPanel.SetActive(true);
            punchMessageText.text = "No one to shoot";
            ClearPunchButtons();
            StartCoroutine(ShowMessageAndFinish(punchPanel, 0.5f));
            return;
        }

        // BOT → rastgele hedef seç
        if (attacker.isBot)
        {
            PlayerController target = targets[UnityEngine.Random.Range(0, targets.Count)];
            ResolveFireOnTarget(attacker, target, showUI: false);
            FinishNow();
            return;
        }

        // PLAYER → aynı Punch UI’si ile, sadece mesaj farklı
        punchPanel.SetActive(true);
        punchMessageText.text = "Choose a player to shoot:";
        ClearPunchButtons();

        foreach (var t in targets)
        {
            PlayerController captured = t;
            Button btn = Instantiate(punchButtonPrefab, punchButtonsParent);
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = captured.playerName;

            btn.onClick.AddListener(() =>
            {
                ResolveFireOnTarget(attacker, captured, showUI: true);
            });
        }
    }

    private void ResolveFireOnTarget(PlayerController attacker, PlayerController target, bool showUI)
    {
        CardDeck attackerDeck = attacker.GetComponent<CardDeck>();
        CardDeck targetDeck = target.GetComponent<CardDeck>();

        if (attackerDeck == null || targetDeck == null)
        {
            Debug.LogError("[CardActionResolver] ResolveFireOnTarget: CardDeck missing on attacker or target!");
            return;
        }

        if (attacker.bulletsUsed >= attacker.maxBulletCount)
        {
            Debug.Log($"[CardActionResolver] {attacker.playerName} tried to fire but has no bullets left.");
            return;
        }

        attacker.bulletsUsed++;
        attacker.bulletsGiven++;
        
        // Hedefin main deck'ine bullet kartı ekle
        targetDeck.AddBulletCardToMainDeck();

        bool gotReward = false;
        if (attacker.bulletsUsed >= attacker.maxBulletCount)
        {
            // 6 bullet'ı da dağıttı → +4 gold
            attacker.AddGold(4);
            gotReward = true;

            if (ScoreboardManager.Instance != null)
                ScoreboardManager.Instance.RefreshScoreboard();
        }

        if (showUI)
        {
            ClearPunchButtons();

            if (punchMessageText != null)
            {
                string msg = $"{attacker.playerName} shot {target.playerName}! (+1 bullet to their deck)";
                if (gotReward)
                    msg += "\nAll bullets used → +4 gold!";
                punchMessageText.text = msg;
            }

            StartCoroutine(ShowMessageAndFinish(punchPanel, 0.5f));
        }
        else
        {
            Debug.Log($"[CardActionResolver] {attacker.playerName} shot {target.playerName}. " +
                      $"Attacker bulletsUsed={attacker.bulletsUsed}/{attacker.maxBulletCount}");
        }
    }

    // ==================== MOVE SHERRIF ====================

    private void HandleMoveSherrif(PlannedCard card)
    {
        PlayerController pc = card.owner;

        // Sheriff'in şu an hangi vagonda olduğunu GameManager'dan al
        int train = GameManager.Instance.GetSheriffTrainIndex();

        bool canLeft  = train > 1;
        bool canRight = train < 4;

        // BOT → rastgele yön (mümkünse)
        if (pc.isBot)
        {
            int dir = DecideBotHorizontalDirection(canLeft, canRight);
            GameManager.Instance.MoveSheriff(dir);
            FinishNow();
            return;
        }

        // PLAYER → MoveHorizontally ile aynı UI'yi kullan
        if (horizontalChoicePanel == null || leftButton == null || rightButton == null)
        {
            Debug.LogError("[CardActionResolver] MoveSherrif: Horizontal choice UI not assigned!");
            // UI yoksa fallback: mantıklı bir yön seçelim
            int dir = DecideBotHorizontalDirection(canLeft, canRight);
            GameManager.Instance.MoveSheriff(dir);
            FinishNow();
            return;
        }

        horizontalChoicePanel.SetActive(true);

        leftButton.onClick.RemoveAllListeners();
        rightButton.onClick.RemoveAllListeners();

        leftButton.gameObject.SetActive(canLeft);
        rightButton.gameObject.SetActive(canRight);

        if (canLeft)
        {
            leftButton.onClick.AddListener(() =>
            {
                GameManager.Instance.MoveSheriff(-1);
                horizontalChoicePanel.SetActive(false);
                FinishNow();
            });
        }

        if (canRight)
        {
            rightButton.onClick.AddListener(() =>
            {
                GameManager.Instance.MoveSheriff(+1);
                horizontalChoicePanel.SetActive(false);
                FinishNow();
            });
        }
    }
    
    // ==================== DRAW AND PASS ====================
    
    private void HandleDrawAndPass(PlannedCard card)
    {
        // Draw & Pass'in asıl etkisi planlama fazında oldu (2 kart çekildi + slot harcandı)
        Debug.Log($"[CardActionResolver] {card.owner.playerName} resolves Draw & Pass (no action in play phase).");
        FinishNow();
    }

    // ==================== HELPERS ====================
    
    private IEnumerator ShowMessageAndFinish(GameObject panelToHide, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (panelToHide != null)
            panelToHide.SetActive(false);

        FinishNow();
    }
    
    private void FinishNow()
    {
        _currentCard = null;

        var cb = _onCardResolved;
        _onCardResolved = null;

        cb?.Invoke();
    }
    
}