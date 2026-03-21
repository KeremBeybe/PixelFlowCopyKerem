using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO; // Dosya kaydetmek için gerekli kütüphane

public class LevelCreatorTool : EditorWindow
{
    private Texture2D sourceTexture;
    private Texture2D previewTexture;
    private int gridSize = 15;

    public enum ToolMode { Izle, Firca, Silgi }
    private ToolMode currentMode = ToolMode.Izle;
    private Color brushColor = Color.black;

    private bool showRedGrid = true;

    // --- YENİ: ARKADAŞININ İSTEDİĞİ OPSIYONEL ORTALAMA AYARI ---
    private bool useColorAveraging = false;

    [MenuItem("Tools/Pixel Level Creator (Pro)")]
    public static void ShowWindow ()
    {
        EditorWindow.GetWindow<LevelCreatorTool>("Level Creator");
    }

    private void OnGUI ()
    {
        GUILayout.Label("Harita Üretim Merkezi", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Kaynak Görsel (Resim)", sourceTexture, typeof(Texture2D), false);

        if (sourceTexture != null)
        {
            if (!sourceTexture.isReadable)
            {
                EditorGUILayout.HelpBox("DİKKAT: Resmin ayarlarına tıklayıp 'Read/Write' özelliğini açmalısın!", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            gridSize = EditorGUILayout.IntSlider("Grid Boyutu", gridSize, 5, 100);

            // --- YENİ: ORTALAMA AÇ/KAPAT BUTONU ---
            useColorAveraging = EditorGUILayout.Toggle("Renk Ortalaması Kullan", useColorAveraging);
            EditorGUILayout.HelpBox(useColorAveraging ? "Fotoğraflar için iyi. Piksel art'ı bulandırabilir." : "Piksel Art için en iyisi. Direkt merkez rengi alır.", MessageType.Info);

            if (GUILayout.Button("1. Ön İzleme Oluştur"))
            {
                GenerateSmartPreview();
            }

            if (previewTexture != null)
            {
                EditorGUILayout.Space();
                GUILayout.Label("2. Haritayı Düzenle:", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(currentMode == ToolMode.Izle, "👀 Sadece İzle", "Button")) currentMode = ToolMode.Izle;
                if (GUILayout.Toggle(currentMode == ToolMode.Firca, "🖌️ Fırça", "Button")) currentMode = ToolMode.Firca;
                if (GUILayout.Toggle(currentMode == ToolMode.Silgi, "🧹 Silgi", "Button")) currentMode = ToolMode.Silgi;
                GUILayout.EndHorizontal();

                if (currentMode == ToolMode.Firca)
                {
                    brushColor = EditorGUILayout.ColorField("Fırça Rengi", brushColor);
                }

                showRedGrid = EditorGUILayout.Toggle("Kırmızı Kılavuzu Göster", showRedGrid);

                EditorGUILayout.Space();

                Rect drawRect = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandWidth(false));
                GUI.DrawTexture(drawRect, previewTexture, ScaleMode.ScaleToFit);

                if (showRedGrid && Event.current.type == EventType.Repaint)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.3f);
                    float cellWidth = drawRect.width / gridSize;
                    float cellHeight = drawRect.height / gridSize;

                    for (int i = 0; i <= gridSize; i++)
                    {
                        GUI.DrawTexture(new Rect(drawRect.x + (i * cellWidth), drawRect.y, 1, drawRect.height), EditorGUIUtility.whiteTexture);
                        GUI.DrawTexture(new Rect(drawRect.x, drawRect.y + (i * cellHeight), drawRect.width, 1), EditorGUIUtility.whiteTexture);
                    }
                    GUI.color = Color.white;
                }

                HandleDrawing(drawRect);

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("3. PNG Olarak Kaydet (Saf Data)", GUILayout.Height(30)))
                {
                    SaveTextureToPNG();
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }

    private void HandleDrawing (Rect rect)
    {
        if (currentMode == ToolMode.Izle) return;

        Event e = Event.current;

        if (rect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                if (e.button == 0)
                {
                    float x = (e.mousePosition.x - rect.x) / rect.width * gridSize;
                    float y = (1.0f - (e.mousePosition.y - rect.y) / rect.height) * gridSize;

                    int pixelX = Mathf.FloorToInt(x);
                    int pixelY = Mathf.FloorToInt(y);

                    if (pixelX >= 0 && pixelX < gridSize && pixelY >= 0 && pixelY < gridSize)
                    {
                        if (currentMode == ToolMode.Firca)
                            previewTexture.SetPixel(pixelX, pixelY, brushColor);
                        else if (currentMode == ToolMode.Silgi)
                            previewTexture.SetPixel(pixelX, pixelY, Color.clear);

                        previewTexture.Apply();
                        e.Use();
                    }
                }
            }
        }
    }

    private void GenerateSmartPreview ()
    {
        int w = sourceTexture.width;
        int h = sourceTexture.height;

        previewTexture = new Texture2D(gridSize, gridSize);
        previewTexture.filterMode = FilterMode.Point;

        float stepX = (float)w / gridSize;
        float stepY = (float)h / gridSize;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (useColorAveraging)
                {
                    // --- ORTALAMA ALMA (Eski Sistem - Fotoğraflar için) ---
                    int startX = Mathf.FloorToInt(x * stepX);
                    int startY = Mathf.FloorToInt(y * stepY);
                    int endX = Mathf.FloorToInt((x + 1) * stepX);
                    int endY = Mathf.FloorToInt((y + 1) * stepY);

                    float r = 0, g = 0, b = 0, a = 0;
                    int pixelCount = 0;

                    for (int px = startX; px < endX; px++)
                    {
                        for (int py = startY; py < endY; py++)
                        {
                            Color c = sourceTexture.GetPixel(px, py);
                            r += c.r; g += c.g; b += c.b; a += c.a;
                            pixelCount++;
                        }
                    }

                    if (pixelCount > 0)
                    {
                        Color avgColor = new Color(r / pixelCount, g / pixelCount, b / pixelCount, a / pixelCount);
                        if (avgColor.a < 0.2f) avgColor = Color.clear;
                        previewTexture.SetPixel(x, y, avgColor);
                    }
                }
                else
                {
                    // --- DİREKT ÖRNEKLEME (Arkadaşının İstediği Sistem - Piksel Art için) ---
                    // O grid hücresinin tam merkezindeki pikseli bulup direkt onu alıyor. Çamurlaşma sıfır!
                    int sampleX = Mathf.FloorToInt((x + 0.5f) * stepX);
                    int sampleY = Mathf.FloorToInt((y + 0.5f) * stepY);

                    Color directColor = sourceTexture.GetPixel(sampleX, sampleY);
                    if (directColor.a < 0.2f) directColor = Color.clear;

                    previewTexture.SetPixel(x, y, directColor);
                }
            }
        }
        previewTexture.Apply();
    }

    // --- YENİ: Unity'nin resmi bozmasını engelleyen Saf Data Kayıt Sistemi ---
    private void SaveTextureToPNG ()
    {
        byte[] bytes = previewTexture.EncodeToPNG();
        string path = EditorUtility.SaveFilePanel("Haritayı Kaydet", "Assets", "YeniHarita", "png");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            string assetPath = path.Substring(path.IndexOf("Assets"));

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                importer.isReadable = true;
                importer.filterMode = FilterMode.Point; // Bulanıklaşmayı engeller
                importer.textureCompression = TextureImporterCompression.Uncompressed; // Renk bozulmasını engeller
                importer.mipmapEnabled = false;

                importer.SaveAndReimport();
            }

            Debug.Log("Harika! Harita 'Saf Data' olarak kaydedildi: " + assetPath);
        }
    }
}