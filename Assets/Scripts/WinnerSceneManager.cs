using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WinnerSceneManager : MonoBehaviour
{
    [Header("Podium Anchors (character positions)")]
    public RectTransform firstPlaceAnchor;
    public RectTransform secondPlaceAnchor;
    public RectTransform thirdPlaceAnchor;
    
    [Header("Player Prefab (same as in GameManager)")]
    public RectTransform playerPrefab;

    [Header("Others List")]
    public RectTransform othersListParent;
    public GameObject otherRowPrefab; // içinde TMP_Text olan basit bir row prefab

    private void Start()
    {
        var players = FinalResultsData.Players;

        if (players == null || players.Count == 0)
        {
            Debug.LogError("[WinnerSceneManager] No final results found.");
            return;
        }

        // players şimdiden credits'e göre sıralı geliyor (GameManager.ShowFinalResults)
        // Yine de emin olmak istersen:
        players.Sort((a, b) => b.credits.CompareTo(a.credits));

        ShowTopThree(players);
        ShowOthers(players);
    }

    private void ShowTopThree(List<FinalPlayerData> players)
    {
        // 1. yer
        if (players.Count > 0 && firstPlaceAnchor != null && playerPrefab != null)
        {
            CreateWinnerPlayerView(players[0], firstPlaceAnchor);
        }

        // 2. yer
        if (players.Count > 1 && secondPlaceAnchor != null && playerPrefab != null)
        {
            CreateWinnerPlayerView(players[1], secondPlaceAnchor);
        }

        // 3. yer
        if (players.Count > 2 && thirdPlaceAnchor != null && playerPrefab != null)
        {
            CreateWinnerPlayerView(players[2], thirdPlaceAnchor);
        }
    }

    private void ShowOthers(List<FinalPlayerData> players)
    {
        if (othersListParent == null || otherRowPrefab == null) return;

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];

            GameObject row = Instantiate(otherRowPrefab, othersListParent);

            // Row içindeki tüm TMP_Text'leri bul
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);

            TMP_Text mainText = null;
            bool firstFound = false;

            foreach (var t in texts)
            {
                if (!firstFound)
                {
                    // İlk bulduğumuzu ana text olarak kullan
                    mainText = t;
                    firstFound = true;
                }
                else
                {
                    // Diğerlerini tamamen gizle
                    t.gameObject.SetActive(false);
                }
            }

            if (mainText != null)
            {
                int rank = i + 1;
                mainText.text =
                    $"{rank}- {p.name} , {p.credits} Credit , BulletsGiven : {p.bulletsGiven}";
            }
            else
            {
                Debug.LogWarning("[WinnerSceneManager] Row prefab içinde hiç TMP_Text bulamadım.");
            }
        }
    }
    private void CreateWinnerPlayerView(FinalPlayerData data, RectTransform anchor)
    {
        // playerPrefab'tan yeni bir karakter instantiate et
        RectTransform playerObj = Instantiate(playerPrefab, anchor.parent);
        playerObj.anchoredPosition = anchor.anchoredPosition;

        // Eğer prefab üzerinde PlayerController varsa, oradan ismi set edebilirsin
        var pc = playerObj.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.SetPlayerName(data.name);
            // Credits / goldBars göstermek istersen:
            pc.goldBars = data.goldBars;
            // pc.credits = data.credits;  // field varsa
            // pc.UpdateDebugLabel();      // public yaparsan çağırabilirsin
        }

        // Eğer sadece üstünde isim yazan bir TMP_Text istiyorsan:
        var texts = playerObj.GetComponentsInChildren<TMP_Text>();
        foreach (var t in texts)
        {
            if (t.gameObject.name.Contains("Name"))
            {
                t.text = data.name;
                break;
            }
        }
    }
}
