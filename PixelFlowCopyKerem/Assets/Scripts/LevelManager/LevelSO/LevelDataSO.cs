using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewLevel", menuName = "PixelFlow/LevelData")]
public class LevelDataSO : ScriptableObject
{
    [Header("Level Tasarýmý")]
    public Texture2D LevelTexture;

    // Ýleride buraya domuzcuklarýn çýkýþ sýrasý (Queue) gibi veriler de eklenecek.
}