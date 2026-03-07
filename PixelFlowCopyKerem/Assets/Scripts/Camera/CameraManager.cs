using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Kamera Ayarlarý")]
    public Camera mainCamera;

    [Tooltip("Ekranýn kenarlarýndan býrakýlacak nefes alma boþluðu")]
    public float padding = 2.5f;

    [Tooltip("Kameranýn 3D uzayda ne kadar geride duracaðý (Zoom deðildir, derinliktir)")]
    public float backwardOffset = 20f;

    void Awake ()
    {
        Instance = this;
        if (mainCamera == null) mainCamera = Camera.main;
    }

    // Bu fonksiyon bölüm tamamen oluþtuktan sonra çaðrýlacak!
    public void FrameLevel ()
    {
        if (mainCamera == null || !mainCamera.orthographic) return;

        List<Vector3> allPoints = new List<Vector3>();

        // --- 1. HARÝTANIN KÖÞELERÝNÝ BUL ---
        if (LevelManager.Instance != null)
        {
            float w = LevelManager.Instance.mapWidth * LevelManager.Instance.CellSize;
            float h = LevelManager.Instance.mapHeight * LevelManager.Instance.CellSize;

            allPoints.Add(new Vector3(0, 0, 0)); // Sol Alt
            allPoints.Add(new Vector3(w, 0, 0)); // Sað Alt
            allPoints.Add(new Vector3(0, 0, h)); // Sol Üst
            allPoints.Add(new Vector3(w, 0, h)); // Sað Üst
        }

        // --- 2. BEKLEME ODASINI (SLOTLARI) BUL ---
        if (DockManager.Instance != null && DockManager.Instance.waitingSlots != null)
        {
            foreach (Transform slot in DockManager.Instance.waitingSlots)
            {
                if (slot != null) allPoints.Add(slot.position);
            }
        }

        // --- 3. OTOPARKTAKÝ DOMUZ KUYRUKLARINI BUL ---
        if (DockManager.Instance != null && DockManager.Instance.columnStarts != null)
        {
            foreach (Transform col in DockManager.Instance.columnStarts)
            {
                if (col != null)
                {
                    // Sütunun en baþý (ilk domuzun yeri)
                    allPoints.Add(col.position);

                    // Kuyruk arkaya doðru uzayacaðý için, tahmini en arka domuzun yerini de güvenli alana katýyoruz!
                    float maxQueueLength = DockManager.Instance.pigSpacing * 5f; // Arkaya 5 domuzluk pay býrak
                    allPoints.Add(col.position + (Vector3.back * maxQueueLength));
                }
            }
        }

        if (allPoints.Count == 0) return;

        // --- EFSANEVÝ MATEMATÝK BAÞLIYOR ---

        // 1. OYUNUN YENÝ MERKEZÝNÝ BUL
        Vector3 center = Vector3.zero;
        foreach (Vector3 p in allPoints) center += p;
        center /= allPoints.Count;

        // 2. KAMERAYI MERKEZE KÝLÝTLE (Kendi açýsýný bozmadan geriye doðru çekerek)
        mainCamera.transform.position = center - (mainCamera.transform.forward * backwardOffset);

        // 3. EKRANA SIÐDIRMA (ORTHOGRAPHIC SIZE HESABI)
        float maxLocalY = 0f;
        float maxLocalX = 0f;

        // Dünyadaki tüm bu sýnýr noktalarýný, kameranýn "Kendi 2D Ekranýna" (Local Space) dönüþtürüp ölçüyoruz!
        foreach (Vector3 p in allPoints)
        {
            Vector3 localPos = mainCamera.transform.InverseTransformPoint(p);
            maxLocalY = Mathf.Max(maxLocalY, Mathf.Abs(localPos.y));
            maxLocalX = Mathf.Max(maxLocalX, Mathf.Abs(localPos.x));
        }

        // Telefonun mevcut ekran oraný (Uzun ince mi, yoksa Tablet gibi kare mi?)
        float screenAspect = (float)Screen.width / Screen.height;

        // Yüksekliðe ve Geniþliðe göre gereken boyutlarý hesapla
        float requiredSizeY = maxLocalY + padding;
        float requiredSizeX = (maxLocalX + padding) / screenAspect;

        // Hangisi daha büyükse onu seç! (Böylece hiçbir telefon modelinde ekran dýþýna taþma olmaz)
        mainCamera.orthographicSize = Mathf.Max(requiredSizeY, requiredSizeX);
    }
}