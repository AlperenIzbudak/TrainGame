using System.Collections.Generic;
using UnityEngine;

public class GameSetupData : MonoBehaviour
{
    public static GameSetupData Instance { get; private set; }

    [Header("Karakterler (Inspector’dan doldur)")]
    public CowboyCharacter[] allCharacters;   // 6 karakterin hepsi

    [Header("Oyuncu Seçimleri")]
    public static string playerName = "Player";
    public static int selectedCharacterId = 0;       // Player’ın seçtiği karakter (0..5)
    public int[] botCharacterIds = new int[3]; // Bot1–Bot3 için karakter ID’leri

    [Header("Oyun Sonu Skorları")]
    public List<FinalPlayerData> finalPlayers = new List<FinalPlayerData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}