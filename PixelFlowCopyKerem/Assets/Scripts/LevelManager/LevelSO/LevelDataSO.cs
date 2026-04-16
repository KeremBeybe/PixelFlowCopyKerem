using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// --- Tek bir domuzun kimlik kartý ---
[System.Serializable]
public class PigSpawnData
{
    public Color pigColor; // Domuzun Rengi
    public int ammoCount;  // Kaç mermisi olacak?
    public int columnIndex;// Hangi sütuna (0, 1, 2 veya 3) dizilecek?
}
[CreateAssetMenu(fileName = "NewLevel", menuName = "PixelFlow/LevelData")]
public class LevelDataSO : ScriptableObject
{
    [Header("Level Tasarýmý")]
    public Texture2D LevelTexture;
    public Sprite ButtonSprite; // Butonun ana rengi
    public bool IsHardLevel = false;       // "Zor" UI'ý için kontrol
    
    [Header("Zorluk Ayarlarý")]
    public int maxWaitingSlots = 5;

    [Header("Domuz Kuyruðu (Bulmaca Dizilimi)")]
    // Senin elinle tek tek gireceðin kusursuz bulmaca listesi!
    public List<PigSpawnData> pigQueue = new List<PigSpawnData>();

    [Header("Görsel Ayarlar")]
    [Tooltip("Textureda dýþta kalan boþ alanlar kýrpýlsýn mý?")]
    public bool autoCropEmptySpace = true;
}