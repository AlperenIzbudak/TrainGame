using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreboardManager : MonoBehaviour
{
    public static ScoreboardManager Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Scoreboard satırlarının parent'i (Vertical Layout Group olan panel)")]
    public RectTransform scoreboardPanel;

    [Tooltip("İçinde 2 tane TMP_Text olan prefab: NameText, GoldText")]
    public GameObject rowPrefab;

    // İçte kullanacağımız küçük row yapısı
    [System.Serializable]
    public class ScoreRow
    {
        public PlayerController player;
        public RectTransform rowTransform;
        public TMP_Text nameText;
        public TMP_Text goldText;
    }

    private readonly List<ScoreRow> _rows = new List<ScoreRow>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Tüm oyuncular spawn olduktan sonra GameManager burayı çağırıyor.
    /// </summary>
    public void Setup(List<PlayerController> players)
    {
        if (scoreboardPanel == null || rowPrefab == null)
        {
            Debug.LogError("[ScoreboardManager] scoreboardPanel veya rowPrefab atanmadı!");
            return;
        }

        // Eski satırları temizle
        foreach (Transform child in scoreboardPanel)
        {
            Destroy(child.gameObject);
        }
        _rows.Clear();

        // Her oyuncu için bir satır oluştur
        foreach (var pc in players)
        {
            GameObject rowGO = Instantiate(rowPrefab, scoreboardPanel);
            RectTransform rowRect = rowGO.GetComponent<RectTransform>();

            TMP_Text[] texts = rowGO.GetComponentsInChildren<TMP_Text>();
            TMP_Text nameText = null;
            TMP_Text goldText = null;

            // Prefab içinde isimlerine göre textleri bul
            foreach (var t in texts)
            {
                if (t.gameObject.name.Contains("Name"))
                    nameText = t;
                else if (t.gameObject.name.Contains("Gold"))
                    goldText = t;
            }

            if (nameText == null || goldText == null)
            {
                Debug.LogWarning("[ScoreboardManager] Row prefab içinde Name / Gold textlerini bulamadım.");
            }

            ScoreRow row = new ScoreRow
            {
                player = pc,
                rowTransform = rowRect,
                nameText = nameText,
                goldText = goldText
            };

            _rows.Add(row);
        }

        RefreshScoreboard();
    }

    /// <summary>
    /// Herhangi birinin gold'u değiştiğinde (collect vs.) çağrılacak.
    /// </summary>
    public void RefreshScoreboard()
    {
        if (_rows.Count == 0) return;

        // Gold'a göre azalan sırada sort et
        _rows.Sort((a, b) => b.player.goldBars.CompareTo(a.player.goldBars));

        // UI sırasını ve textleri güncelle
        for (int i = 0; i < _rows.Count; i++)
        {
            ScoreRow row = _rows[i];

            // Hierarchy sırasını değiştir (üstte en çok gold olan olsun)
            row.rowTransform.SetSiblingIndex(i);

            if (row.nameText != null)
                row.nameText.text = row.player.playerName;

            if (row.goldText != null)
                row.goldText.text = row.player.goldBars.ToString();
        }
    }
}
