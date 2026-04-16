using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVScroller : MonoBehaviour
{
    [Header("Ayarlar")]
    public MeshRenderer targetRenderer;
    public int materialIndex = 0; // SS'te 0. element 'Road' materyali
    public float scrollSpeed = 0.5f;

    private float currentOffsetY = 0f;

    void Update ()
    {
        // 1. Ofseti sürekli eksiye götür
        currentOffsetY -= Time.deltaTime * scrollSpeed;

        // 2. SAPITMAYI ÖNLEME: Deðeri 0 ile 1 arasýnda döngüye sok (Modulo mantýðý)
        // Böylece sayý hiçbir zaman devasa boyutlara ulaþýp titreme yapmaz.
        currentOffsetY %= 1.0f;

        // 3. Materyale uygula
        // URP Lit Shader kullandýðýn için "_BaseMap" ismini kullanýyoruz
        if (targetRenderer != null)
        {
            targetRenderer.materials[materialIndex].SetTextureOffset("_BaseMap", new Vector2(0, currentOffsetY));
        }
    }
}