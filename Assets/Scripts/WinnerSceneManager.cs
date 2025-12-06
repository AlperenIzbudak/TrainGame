using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinnerSceneManager : MonoBehaviour
{
    [Header("Podium Images (Positions/s1,s2,s3 içindeki Image bileşenleri)")]
    public Image firstPlaceImage;   // s1 altındaki Image
    public Image secondPlaceImage;  // s2 altındaki Image
    public Image thirdPlaceImage;   // s3 altındaki Image

    [Header("Karakter verileri (MainMenu/GameManager ile aynı sıra)")]
    public CowboyCharacter[] characters;

    [Header("Others list")]
    public RectTransform othersListParent;  // ScoreBoard objesinin RectTransform'u
    public GameObject otherRowPrefab;       // İçinde *tek* TMP_Text olan row prefab

    private void Start()
    {
        var players = FinalResultsData.Players;

        if (players == null || players.Count == 0)
        {
            Debug.LogError("[WinnerSceneManager] No final results found.");
            return;
        }

        // Emin olmak için kredilere göre sıralayalım (büyükten küçüğe)
        players.Sort((a, b) => b.credits.CompareTo(a.credits));

        ShowPodium(players);
        ShowOthers(players);
    }

    // ------------------------------------------------------
    //  PODIUM
    // ------------------------------------------------------

    private void ShowPodium(List<FinalPlayerData> players)
    {
        if (players.Count > 0)
            SetPodiumSlot(firstPlaceImage, players[0]);

        if (players.Count > 1)
            SetPodiumSlot(secondPlaceImage, players[1]);

        if (players.Count > 2)
            SetPodiumSlot(thirdPlaceImage, players[2]);
    }

    private void SetPodiumSlot(Image img, FinalPlayerData data)
    {
        if (img == null) return;

        var ch = FindCharacterById(data.characterId);
        if (ch != null && ch.sprite != null)
        {
            img.sprite = ch.sprite;
            img.enabled = true;
        }
        else
        {
            // Karakter bulunamazsa, en azından görüntü bozuk olmasın
            img.enabled = false;
        }
    }

    private CowboyCharacter FindCharacterById(int id)
    {
        if (characters == null) return null;

        foreach (var ch in characters)
        {
            if (ch != null && ch.id == id)
                return ch;
        }

        return null;
    }

    // ------------------------------------------------------
    //  OTHERS SCOREBOARD
    // ------------------------------------------------------

    private void ShowOthers(List<FinalPlayerData> players)
    {
        if (othersListParent == null || otherRowPrefab == null) return;

        // ==== ÖNEMLİ: Eski satırları temizle, yoksa üst üste birikiyor ====
        for (int i = othersListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(othersListParent.GetChild(i).gameObject);
        }

        // Şimdi listeden bir kez oluştur
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


    // ------------------------------------------------------
    //  BUTTON
    // ------------------------------------------------------

    public void OnReturnToMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
