using UnityEngine;

[System.Serializable]
public class CowboyCharacter
{
    [Tooltip("0..5 arası benzersiz ID (Button'lardaki ID ile aynı)")]
    public int id;

    public string displayName;   // İleride pasif skill vs. açıklaması için
    public Sprite sprite;        // Bu karakterin cowboy sprite'ı
}