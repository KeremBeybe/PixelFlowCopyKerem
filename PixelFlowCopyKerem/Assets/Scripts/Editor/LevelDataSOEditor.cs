using UnityEngine;
using UnityEditor; // BÜTÜN SIR BURADA! Bu, Unity'nin arayüzünü hacklememizi sağlar.
using System.Collections.Generic;

// Bu kodun hangi ScriptableObject'i izleyeceğini söylüyoruz
[CustomEditor(typeof(LevelDataSO))]
public class LevelDataSOEditor : Editor
{
    public override void OnInspectorGUI ()
    {
        // 1. Orijinal Inspector'ı çiz (Texture kutusu ve Domuz listesi normal görünmeye devam etsin)
        DrawDefaultInspector();

        LevelDataSO levelData = (LevelDataSO)target;

        // Eğer henüz bir harita resmi koyulmadıysa asistanı çalıştırma
        if (levelData.LevelTexture == null) return;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🧠 BÖLÜM TASARIM ASİSTANI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Aşağıdaki tablo, Texture'daki küp sayısı ile domuzlara verdiğin mermileri anlık olarak karşılaştırır.", MessageType.Info);

        try
        {
            // --- 1. TEXTURE'DAKİ RENKLERİ SAY ---
            Dictionary<string, int> textureCounts = new Dictionary<string, int>();
            Dictionary<string, Color> colorRefs = new Dictionary<string, Color>();

            Texture2D tex = levelData.LevelTexture;
            Color[] pixels = tex.GetPixels(); // Resimdeki tüm pikselleri okur

            foreach (Color p in pixels)
            {
                if (p.a < 0.1f) continue; // Şeffaf (boş) pikselleri atla

                // Renkleri kusursuz eşleştirmek için HEX (Metin) koduna çeviriyoruz
                string hex = ColorUtility.ToHtmlStringRGB(p);
                if (!textureCounts.ContainsKey(hex))
                {
                    textureCounts[hex] = 0;
                    colorRefs[hex] = p;
                }
                textureCounts[hex]++;
            }

            // --- 2. SENİN LİSTENE GİRDİĞİN MERMİLERİ SAY ---
            Dictionary<string, int> assignedAmmos = new Dictionary<string, int>();
            if (levelData.pigQueue != null)
            {
                foreach (var pig in levelData.pigQueue)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(pig.pigColor);
                    if (!assignedAmmos.ContainsKey(hex)) assignedAmmos[hex] = 0;
                    assignedAmmos[hex] += pig.ammoCount;
                }
            }

            // --- 3. EKRANA ASİSTAN SONUÇLARINI ÇİZ ---
            EditorGUILayout.Space(5);
            foreach (var kvp in textureCounts)
            {
                string hex = kvp.Key;
                int required = kvp.Value; // Texture'da olan küp
                int assigned = assignedAmmos.ContainsKey(hex) ? assignedAmmos[hex] : 0; // Senin verdiğin mermi
                int diff = required - assigned;

                EditorGUILayout.BeginHorizontal("box");

                // Rengin kendisini küçük bir kare olarak ekrana çiz (Görsellik!)
                Color c = colorRefs[hex];
                c.a = 1f;
                GUIStyle colorBox = new GUIStyle(GUI.skin.box);
                colorBox.normal.background = EditorGUIUtility.whiteTexture;
                GUI.backgroundColor = c;
                GUILayout.Box("", colorBox, GUILayout.Width(20), GUILayout.Height(20));
                GUI.backgroundColor = Color.white;

                // Hesaplama ve Uyarı Metinleri
                if (diff == 0)
                {
                    // Tam oturdu!
                    GUI.contentColor = Color.green;
                    EditorGUILayout.LabelField($"KUSURSUZ! ({required} Küp / {assigned} Mermi)", EditorStyles.boldLabel);
                }
                else if (diff > 0)
                {
                    // Eksik mermi var!
                    GUI.contentColor = new Color(1f, 0.4f, 0.4f); // Açık Kırmızı
                    EditorGUILayout.LabelField($"EKSİK! {diff} mermi daha lazım! ({required} Küp / {assigned} Mermi)", EditorStyles.boldLabel);
                }
                else
                {
                    // Fazla mermi verilmiş!
                    GUI.contentColor = Color.yellow;
                    EditorGUILayout.LabelField($"FAZLA! {-diff} mermi fazla verdin! ({required} Küp / {assigned} Mermi)", EditorStyles.boldLabel);
                }
                GUI.contentColor = Color.white; // Rengi sıfırla
                EditorGUILayout.EndHorizontal();
            }
        }
        catch (UnityException)
        {
            // Eğer resim okunmaya kapalıysa Asistan bizi uyarır!
            EditorGUILayout.HelpBox("DİKKAT: Resmin pikselleri okunamıyor! Lütfen Project panelinden bu Texture'a (resme) tıkla, sağdaki ayarlardan 'Advanced' altındaki 'Read/Write' (veya Read/Write Enabled) kutusunu İŞARETLE ve Apply de.", MessageType.Error);
        }
    }
}
