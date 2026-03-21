using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    public Camera cam;

    [Header("Kamera & Dizilim")]
    public float cameraHeight = 40f;
    [Tooltip("Haritanýn bittiði yer ile domuzlar arasýndaki boþluk")]
    public float dockOffsetFromMap = 5f;
    public float screenPadding = 1.15f;

    [Header("Ýnce Ayar (Göz Kararý)")]
    [Tooltip("Görüntüyü ekranýn neresine oturtmak istiyorsan bunu deðiþtir. Üstteki boþluðu kapatmak (yukarý kaymak) için EKSÝ deðerler (örn: -3, -5) ver.")]
    public float verticalShift = 0f;

    private void Awake ()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;
    }

    public void FrameLevel ()
    {
        if (LevelManager.Instance == null || DockManager.Instance == null) return;

        float mapWidth = LevelManager.Instance.mapWidth * LevelManager.Instance.CellSize;
        float mapHeight = LevelManager.Instance.mapHeight * LevelManager.Instance.CellSize;

        float dockZ = -(dockOffsetFromMap);
        DockManager.Instance.transform.position = new Vector3(0, 0, dockZ);

        float topBound = mapHeight;
        float bottomBound = dockZ - (DockManager.Instance.pigSpacing * 1.5f);

        // --- SÝHÝRLÝ DOKUNUÞ BURADA ---
        // Matematiðin bulduðu merkeze, senin göz kararý verdiðin kaydýrmayý ekliyoruz.
        float centerZ = ((topBound + bottomBound) / 2f) + verticalShift;
        float totalHeight = topBound - bottomBound;

        float angleRad = cam.transform.eulerAngles.x * Mathf.Deg2Rad;
        float zOffset = cameraHeight / Mathf.Tan(angleRad);
        cam.transform.position = new Vector3(0, cameraHeight, centerZ - zOffset);

        float sinX = Mathf.Sin(angleRad);
        if (sinX < 0.1f) sinX = 1f;

        float requiredVertical = (totalHeight / 2f) * sinX;
        float requiredHorizontal = (mapWidth / 2f) / cam.aspect;

        cam.orthographicSize = Mathf.Max(requiredVertical, requiredHorizontal) * screenPadding;

        DockManager.Instance.transform.position = new Vector3(0, 0, dockZ);
    }
}