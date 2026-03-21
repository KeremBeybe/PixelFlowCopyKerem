using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class LevelManager : MonoBehaviour
{
    [Header("Level Ayarları")]
    public LevelDataSO CurrentLevel; // ScriptableObject'imiz
    public GameObject CubePrefab;  // Sahnede dizilecek küp
    public float CellSize = 1f;    // Izgara aralığı

    [Header("Arka Plan Verisi (Grid)")]
    public CubeData[,] GridMap;    // 2D Array haritamız

    // Obje Havuzu (Pool) Değişkenleri
    private Queue<GameObject> cubePool = new Queue<GameObject>();
    private List<GameObject> activeCubes = new List<GameObject>();

    [Header("Path (Yol) Ayarları")]
    public PathCreator pathCreator; // Sahnemizdeki PathCreator objesi
    public float pathMargin = 2f; // Yolun küplere olan uzaklığı (Çok yapışmasın diye pay bırakıyoruz)

    // Singleton: Diğer scriptlerin bu koda anında ulaşmasını sağlar
    public static LevelManager Instance;
    // Haritanın genel boyutlarını aklımızda tutalım
    [HideInInspector] public int mapWidth;
    [HideInInspector] public int mapHeight;

    // Hangi renkten kaç küp olduğunu tutacak kusursuz liste
    public Dictionary<Color, int> ColorCounts = new Dictionary<Color, int>();

    [Header("Endgame (Son Evre) Takibi")]
    public int totalCubes = 0;
    public int destroyedCubes = 0;

    [Header("Ölçeklendirme (Boyut) Ayarları")]
    public float baseMapSize = 15f; // İdeal harita (15x15)
    public float currentMultiplier = 1f; // Hesaplanacak çarpan

    [Header("Level İlerleme Sistemi")]
    public List<LevelDataSO> levelList; // Hazırladığın tüm SO'ları buraya dizeceksin
    public int currentLevelIndex = 0;   // Şu an kaçıncı leveldayız?

    [Header("Sanatçı Dostu Çözünürlük Ayarları")]
    [Tooltip("Transparan (boş) pikselleri otomatik bulur ve kırpar.")]
    public bool autoCropEmptySpace = true;

    [Tooltip("Görsel 1024x1024 olsa bile haritayı 30x30'luk ızgaraya (Grid) zorlamak için 30 yazın. Orijinal ise 0 bırakın.")]
    public int customGridSize = 0;

    [Header("Mermi Havuzu Ayarları")]
    public GameObject bulletPrefab; // PigShooter'daki prefabı buraya da sürükle
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private void Start ()
    {
        // 20x20 harita maksimum 400 küp alır. Başlangıçta havuzu dolduruyoruz.
        InitializeCubeMapPool(4000);
        InitializeBulletPool(200);
        // Test için oyun başlar başlamaz leveli oluşturuyoruz
        GenerateLevel();
    }
    private void Awake ()
    {
        Instance = this; // Oyun başlar başlamaz kendini kaydet       
    }
    private void InitializeCubeMapPool (int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // Küpleri Instantiate edip bu scriptin altına saklıyoruz
            GameObject obj = Instantiate(CubePrefab, transform);
            obj.SetActive(false);
            cubePool.Enqueue(obj);
        }
    }

    public void GenerateLevel ()
    {
        totalCubes = 0;
        destroyedCubes = 0;
        ClearCurrentLevel();

        Texture2D tex = CurrentLevel.LevelTexture;

        // --- 1. OTOMATİK KIRPMA (AUTO-CROP) ---
        int minX = 0, maxX = tex.width - 1, minY = 0, maxY = tex.height - 1;

        if (CurrentLevel != null && CurrentLevel.autoCropEmptySpace)
        {
            minX = tex.width; maxX = 0; minY = tex.height; maxY = 0;
            bool hasPixel = false;
            for (int i = 0; i < tex.width; i++)
            {
                for (int j = 0; j < tex.height; j++)
                {
                    if (tex.GetPixel(i, j).a > 0.1f)
                    {
                        if (i < minX) minX = i;
                        if (i > maxX) maxX = i;
                        if (j < minY) minY = j;
                        if (j > maxY) maxY = j;
                        hasPixel = true;
                    }
                }
            }
            if (!hasPixel) { minX = 0; maxX = tex.width - 1; minY = 0; maxY = tex.height - 1; }
        }

        // Resmin boşluksuz, GERÇEK boyutu
        int realArtWidth = maxX - minX + 1;
        int realArtHeight = maxY - minY + 1;

        // --- 2. GRID (ÖLÇEKLENDİRME) SİSTEMİ ---
        int gridW = realArtWidth;
        int gridH = realArtHeight;

        if (customGridSize > 0)
        {
            if (realArtWidth >= realArtHeight)
            {
                gridW = customGridSize;
                gridH = Mathf.RoundToInt((float)realArtHeight / realArtWidth * customGridSize);
            }
            else
            {
                gridH = customGridSize;
                gridW = Mathf.RoundToInt((float)realArtWidth / realArtHeight * customGridSize);
            }
        }

        mapWidth = gridW;
        mapHeight = gridH;

        float maxDimension = Mathf.Max(gridW, gridH);
        CellSize = baseMapSize / maxDimension;

        float offsetX = (gridW - 1) * CellSize / 2f;
        float offsetZ = (gridH - 1) * CellSize / 2f;

        GridMap = new CubeData[gridW, gridH];

        float stepX = (float)realArtWidth / gridW;
        float stepY = (float)realArtHeight / gridH;

        for (int x = 0; x < gridW; x++)
        {
            for (int y = 0; y < gridH; y++)
            {
                int texX = minX + Mathf.FloorToInt(x * stepX + (stepX / 2f));
                int texY = minY + Mathf.FloorToInt(y * stepY + (stepY / 2f));

                Color pixelColor = tex.GetPixel(texX, texY);

                if (pixelColor.a < 0.1f)
                {
                    GridMap[x, y] = null;
                    continue;
                }

                GameObject cube;
                if (cubePool != null && cubePool.Count > 0) cube = cubePool.Dequeue();
                else cube = Instantiate(CubePrefab, transform);

                cube.SetActive(true);
                activeCubes.Add(cube);
                totalCubes++;

                cube.transform.position = new Vector3((x * CellSize) - offsetX, 0, (y * CellSize) - offsetZ);
                cube.transform.localScale = CubePrefab.transform.localScale * CellSize;

                ApplyColor(cube, pixelColor);

                string hexID = ColorUtility.ToHtmlStringRGB(pixelColor);
                BlockIdentity id = cube.GetComponent<BlockIdentity>();
                if (id != null) id.colorHexID = hexID;

                if (!ColorCounts.ContainsKey(pixelColor)) ColorCounts[pixelColor] = 0;
                ColorCounts[pixelColor]++;

                GridMap[x, y] = new CubeData
                {
                    GridPosition = new Vector2Int(x, y),
                    Color = pixelColor,
                    IsActive = true,
                    CubeRef = cube
                };
            }
        }

        // DİKKAT: Orijinalindeki gibi tekrar gridW ve gridH gönderiyoruz!
        if (pathCreator != null) GenerateDynamicPath(gridW, gridH);

        if (DockManager.Instance != null && CurrentLevel != null)
        {
            DockManager.Instance.transform.localScale = Vector3.one;
            float dockZ = -offsetZ - (CellSize / 2f) - pathMargin - 4f;
            DockManager.Instance.transform.position = new Vector3(0, 0, dockZ);
            DockManager.Instance.SpawnPigsForLevel(CurrentLevel.pigQueue);
        }

        if (CameraManager.Instance != null) Invoke(nameof(CallCameraFrame), 0.1f);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLevelText(currentLevelIndex);
        }
    }

    private void GenerateDynamicPath (int w, int h)
    {
        float offsetX = (w - 1) * CellSize / 2f;
        float offsetZ = (h - 1) * CellSize / 2f;

        float minX = -offsetX - (CellSize / 2f);
        float minZ = -offsetZ - (CellSize / 2f);
        float maxX = offsetX + (CellSize / 2f);
        float maxZ = offsetZ + (CellSize / 2f);

        Vector3[] waypoints = new Vector3[]
        {
            new Vector3(minX - pathMargin, 0, minZ - pathMargin), // Sol Alt
            new Vector3(maxX + pathMargin, 0, minZ - pathMargin), // Sağ Alt
            new Vector3(maxX + pathMargin, 0, maxZ + pathMargin), // Sağ Üst
            new Vector3(minX - pathMargin, 0, maxZ + pathMargin)  // Sol Üst
        };

        BezierPath autoPath = new BezierPath(waypoints, true, PathSpace.xz);
        autoPath.ControlPointMode = BezierPath.ControlMode.Automatic;
        autoPath.AutoControlLength = 0.01f; // Orijinal keskin dönüş
        pathCreator.bezierPath = autoPath;
    }
    private void OnDrawGizmos ()
    {
        // PathCreator ve içindeki path boş değilse çiz
        if (pathCreator != null && pathCreator.path != null)
        {
            Gizmos.color = Color.green; // İstediğin rengi yapabilirsin (Örn: Color.red, Color.cyan)

            // Yolun tüm noktalarını dolaş ve aralarına çizgi çek
            for (int i = 0; i < pathCreator.path.NumPoints - 1; i++)
            {
                Vector3 p1 = pathCreator.path.GetPoint(i);
                Vector3 p2 = pathCreator.path.GetPoint(i + 1);

                // Noktalar arasına çizgi çek (Virajları gösterecek)
                Gizmos.DrawLine(p1, p2);
            }

            // Eğer yol bir döngüyse (loop), son noktayı ilk noktaya bağla
            if (pathCreator.path.isClosedLoop)
            {
                Vector3 lastPoint = pathCreator.path.GetPoint(pathCreator.path.NumPoints - 1);
                Vector3 firstPoint = pathCreator.path.GetPoint(0);
                Gizmos.DrawLine(lastPoint, firstPoint);
            }
        }
    }
    private void ClearCurrentLevel ()
    {
        // 1. Küpleri Temizle (Eğer liste varsa)
        if (activeCubes != null)
        {
            foreach (GameObject cube in activeCubes)
            {
                if (cube != null)
                {
                    cube.SetActive(false); // Sahneden gizle

                    // ŞÜPHELİ YER BURASIYDI! 
                    // Eğer havuz kodunu eklediysen, havuzun var olup olmadığını da kontrol etmeliyiz:
                    if (cubePool != null && !cubePool.Contains(cube))
                    {
                        cubePool.Enqueue(cube);
                    }
                }
            }
            activeCubes.Clear(); // Listeyi boşalt
        }

        // 2. Haritayı Temizle (Eğer daha önceden çizildiyse)
        if (GridMap != null)
        {
            Array.Clear(GridMap, 0, GridMap.Length);
        }

        // 3. Renk Sayacını Temizle (Eğer sözlük oluşturulduysa)
        if (ColorCounts != null)
        {
            ColorCounts.Clear();
        }
    }

    private void ApplyColor (GameObject cube, Color color)
    {
        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        // URP'nin standart shader'ında ana renk değişkeni genelde "_BaseColor"dır.
        mpb.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(mpb);
    }
    #region Yardımcı fonksiyonlar
    // Verilen X ve Y koordinatında bir küp var mı, veya harita sınırının dışı mı?
    public bool IsCellOccupied (int x, int y)
    {
        // 1. Sınır kontrolü (Haritanın dışına çıkıyorsa orayı "Duvar" sayıp dolu döndürüyoruz)
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return true;

        // 2. Eğer Array'in o noktasında veri varsa (null değilse) orası doludur
        if (GridMap[x, y] != null) return true;

        // Hiçbir engele takılmadıysa orası boştur!
        return false;
    }

    // Mermi yerine oturduğunda onu GridMap'e kaydet
    public void AddToGrid (int x, int y, GameObject block)
    {
        GridMap[x, y] = new CubeData
        {
            GridPosition = new Vector2Int(x, y),
            IsActive = true,
            CubeRef = block
        };
    }
    #endregion

    // YENİ FONKSİYON: Dışarıdan küp patladığında burası çağrılacak ve %95 kontrolü yapacak
    public bool IsEndgameActive ()
    {
        if (totalCubes == 0) return false;

        // Patlayan küp oranını hesapla (örn: 0.95)
        float progress = (float)destroyedCubes / totalCubes;
        return progress >= 0.90f; // Oyuncu sıkılmasın diye %90'da başlatıyoruz, istersen 0.95f yapabilirsin!
    }
    // YENİ: Kazanma Kontrolü!
    public void CheckWinCondition ()
    {
        // Eğer patlayan küp sayısı, toplam küplere ulaştıysa...
        if (destroyedCubes >= totalCubes && totalCubes > 0)
        {
            Debug.Log("🎉 KUSURSUZ! BÖLÜM GEÇİLDİ!");
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(5);
            // Oyuncuya patlama efektlerini izlemesi için 2 saniye ver, sonra diğer levele geç
            Invoke(nameof(LoadNextLevel), 2f);
        }
    }

    private void LoadNextLevel ()
    {
        currentLevelIndex++; // Sıradaki levele geç!

        if (currentLevelIndex < levelList.Count)
        {
            // Yeni levelin SO dosyasını aktif et
            CurrentLevel = levelList[currentLevelIndex];

            // GÜVENLİK TEMİZLİĞİ: Sahnede boşta gezen, slota yatmış eski domuzlar varsa onları tamamen sil!
            PigMovement[] oldPigs = FindObjectsOfType<PigMovement>();
            foreach (PigMovement pig in oldPigs)
            {
                if (DockManager.Instance != null) DockManager.Instance.ReturnPigToPool(pig.gameObject);
                else Destroy(pig.gameObject);
            }

            // Oyunun kendi kendini baştan kurduğu o meşhur fonksiyonunu çağır!
            GenerateLevel();
        }
        else
        {
            // Bütün leveller bittiyse
            Debug.Log("🏆 OYUN BİTTİ! BÜTÜN BÖLÜMLERİ GEÇTİN!");
        }
    }
    // Invoke için yardımcı küçük fonksiyon
    private void CallCameraFrame ()
    {
        if (CameraManager.Instance != null) CameraManager.Instance.FrameLevel();
    }

    #region BulletPoolMethods
    // Start veya InitializePool içinde çağırabilirsin
    private void InitializeBulletPool (int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(bulletPrefab, transform);
            obj.SetActive(false);
            bulletPool.Enqueue(obj);
        }
    }

    // Havuzdan mermi alma fonksiyonu
    public GameObject GetBulletFromPool ()
    {
        if (bulletPool.Count > 0)
        {
            GameObject obj = bulletPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            // Havuz biterse yeni tane üret (canGrow mantığı)
            return Instantiate(bulletPrefab, transform);
        }
    }

    // Havuza mermi geri verme fonksiyonu
    public void ReturnBulletToPool (GameObject bullet)
    {
        bullet.SetActive(false);
        if (!bulletPool.Contains(bullet))
        {
            bulletPool.Enqueue(bullet);
        }
    }
    #endregion
}