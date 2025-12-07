using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugPanelManager : MonoBehaviour
{
    [Header("UI")]
    public RectTransform debugPanel;      // Vertical Layout Group olan panel
    public GameObject debugTextPrefab;    // İçinde TMP_Text olan prefab

    private class DebugRow
    {
        public PlayerController player;
        public TMP_Text text;
    }

    private readonly List<DebugRow> _rows = new List<DebugRow>();

    private void LateUpdate()
    {
        if (debugPanel == null || debugTextPrefab == null) return;
        if (RoundManager.Instance == null) return;

        var players = RoundManager.Instance.players;
        if (players == null || players.Count == 0) return;

        // Oyuncu sayısı değiştiyse satırları yeniden kur
        if (_rows.Count != players.Count)
        {
            RebuildRows(players);
        }

        // Her satırı güncelle
        foreach (var row in _rows)
        {
            if (row.player == null || row.text == null) continue;

            PlayerController pc = row.player;
            CardDeck deck = pc.GetComponent<CardDeck>();

            // Pozisyon: t11 / r42
            string posPrefix = pc.isOnRoof ? "r" : "t";
            string pos = $"{posPrefix}{pc.trainIndex}{pc.spotIndex}";

            // Elindeki kartlar
            string handText = "";
            if (deck != null && deck.playerDeck != null)
            {
                handText = string.Join(", ", deck.playerDeck);
            }

            // Diğer oyunculara verdiği bullet = vurduğu mermi sayısı
            int bulletsGiven = pc.bulletsUsed;

            // Diğer oyunculardan aldığı toplam bullet = main deck içindeki bullet sayısı
            int bulletsReceived = 0;
            if (deck != null && deck.baseDeck != null)
            {
                foreach (string c in deck.baseDeck)
                {
                    if (c == CardDeck.BulletCardKey)
                        bulletsReceived++;
                }
            }

            // Metni yaz
            row.text.text =
                $"{pc.playerName} [{pos}]\n" +
                $"Bars: {pc.goldBars} | Credits: {pc.credits}\n" +
                $"Hand: {handText}\n" +
                $"Bullets given: {bulletsGiven} | Bullets received: {bulletsReceived}";
        }
    }

    private void RebuildRows(List<PlayerController> players)
    {
        // Eski satırları temizle
        foreach (Transform child in debugPanel)
        {
            Destroy(child.gameObject);
        }
        _rows.Clear();

        // Her oyuncu için bir satır
        foreach (var pc in players)
        {
            GameObject go = Instantiate(debugTextPrefab, debugPanel);
            TMP_Text txt = go.GetComponent<TMP_Text>();
            if (txt == null)
                txt = go.GetComponentInChildren<TMP_Text>();

            _rows.Add(new DebugRow
            {
                player = pc,
                text = txt
            });
        }
    }
}
