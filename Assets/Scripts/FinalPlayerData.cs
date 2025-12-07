using System.Collections.Generic;

[System.Serializable]
public class FinalPlayerData
{
    public string name;
    public int goldBars;      // kaç barı var
    public int credits;       // toplam kredi (sıralama buna göre)
    public bool isBot;

    public int bulletsGiven;  // verdiği bullet sayısı

    // Bitiş ekranında doğru sprite'ı basmak için
    public int characterId;   // 0..5 (PlayerController.characterId ile aynı)
}

public static class FinalResultsData
{
    public static List<FinalPlayerData> Players;
}