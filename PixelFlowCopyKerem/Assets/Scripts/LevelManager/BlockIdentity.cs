using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockIdentity : MonoBehaviour
{
    [Tooltip("Aseprite'tan gelen veya koda çevrilen kusursuz Hex kimliði")]
    public string colorHexID;

    [Header("Sadece Domuz/Mermi Ayarý Ýçin")]
    [Tooltip("Eðer bu obje mermiyse, Inspector'dan rengini buradan seç.")]
    public Color inspectorColor = Color.white;

    void Awake ()
    {
        // Eðer colorHexID boþsa (Yani bu level manager'ýn ürettiði küp deðil de, senin Inspector'dan elinle boyadýðýn bir mermiyse)
        // Inspector'daki rengi alýp otomatik olarak Hex koduna çevir.
        if (string.IsNullOrEmpty(colorHexID))
        {
            // Rengi Hex string'e (Örn: "FF0000") çevirir
            colorHexID = ColorUtility.ToHtmlStringRGB(inspectorColor);
        }
    }
}