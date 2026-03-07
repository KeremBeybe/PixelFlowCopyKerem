using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
public class LevelManager : MonoBehaviour
{
    [Header("Level Ayarlarý")]
    public LevelDataSO CurrentLevel; // ScriptableObject'imiz
    public GameObject CubePrefab;  // Sahnede dizilecek küp
    public float CellSize = 1f;    // Izgara aralýðý

    [Header("Arka Plan Verisi (Grid)")]
    public CubeData[,] GridMap;    // 2D Array haritamýz

    // Obje Havuzu (Pool) Deðiþkenleri
    private Queue<GameObject> cubePool = new Queue<GameObject>();
    private List<GameObject> activeCubes = new List<GameObject>();

    [Header("Path (Yol) Ayarlarý")]
    public PathCreator pathCreator; // Sahnemizdeki PathCreator objesi
    public float pathMargin = 2f; // Yolun küplere olan uzaklýðý (Çok yapýþmasýn diye pay býrakýyoruz)

    // Singleton: Diðer scriptlerin bu koda anýnda ulaþmasýný saðlar
    public static LevelManager Instance;
    // Haritanýn genel boyutlarýný aklýmýzda tutalým
    [HideInInspector] public int mapWidth;
    [HideInInspector] public int mapHeight;

    // Hangi renkten kaç küp olduðunu tutacak kusursuz liste
    public Dictionary<Color, int> ColorCounts = new Dictionary<Color, int>();

    [Header("Endgame (Son Evre) Takibi")]
    public int totalCubes = 0;
    public int destroyedCubes = 0;

    private void Start ()
    {
        // 20x20 harita maksimum 400 küp alýr. Baþlangýçta havuzu dolduruyoruz.
        InitializePool(400);

        // Test için oyun baþlar baþlamaz leveli oluþturuyoruz
        GenerateLevel();
    }
    private void Awake ()
    {
        Instance = this; // Oyun baþlar baþlamaz kendini kaydet       
    }
    private void InitializePool (int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // Küpleri Instantiate edip bu scriptin altýna saklýyoruz
            GameObject obj = Instantiate(CubePrefab, transform);
            obj.SetActive(false);
            cubePool.Enqueue(obj);
        }
    }

    public void GenerateLevel ()
    {
        totalCubes = 0; // Bölüm baþlarken sýfýrla
        destroyedCubes = 0;
        ColorCounts.Clear();
        ClearCurrentLevel(); // Eðer sahnede eski bir level varsa önce onu temizle

        Texture2D tex = CurrentLevel.LevelTexture;
        int width = tex.width;
        int height = tex.height;

        mapWidth = width;
        mapHeight = height;
        // Diziyi texture boyutuna göre baþlatýyoruz
        GridMap = new CubeData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color pixelColor = tex.GetPixel(x, y);

                // Eðer pikselin Alpha (þeffaflýk) deðeri düþükse, orasý boþtur.
                if (pixelColor.a < 0.1f)
                {
                    GridMap[x, y] = null;
                    continue;
                }

                // Boyalý alan bulduk! Havuzdan bir küp çekiyoruz.
                GameObject cube = cubePool.Dequeue();
                cube.SetActive(true);
                activeCubes.Add(cube); // Ýleride temizlemek için listeye ekliyoruz
                totalCubes++; // YENÝ: Havuzdan sahneye her küp koyduðumuzda sayacý 1 artýrýyoruz!

                // X ve Y'yi kullanarak 3D dünyada hizalama (Z ekseninde derinlik veriyoruz)
                cube.transform.position = new Vector3(x * CellSize, 0, y * CellSize);

                // Material Property Block ile renk atama
                ApplyColor(cube, pixelColor);

                // --- YENÝ: KÝMLÝK ATAMA VE SAYIM YAPMA ---
                string hexID = ColorUtility.ToHtmlStringRGB(pixelColor); // Piksel rengini metne çevir

                BlockIdentity id = cube.GetComponent<BlockIdentity>();
                if (id != null)
                {
                    id.colorHexID = hexID; // Küpün beynine rengini kazý
                }

                // Renk sayacýný güncelle (Bekleme odasýndaki domuzlar için hazýrlýk)
                if (!ColorCounts.ContainsKey(pixelColor))
                {
                    ColorCounts[pixelColor] = 0;
                }
                ColorCounts[pixelColor]++;
                // -----------------------------------------

                // Datayý array'e iþliyoruz (Ateþ etme mekaniðinde burayý okuyacaðýz)
                GridMap[x, y] = new CubeData
                {
                    GridPosition = new Vector2Int(x, y),
                    Color = pixelColor,
                    IsActive = true,
                    CubeRef = cube
                };
            }
        }
        if (pathCreator != null)
        {
            GenerateDynamicPath(width, height);
        }
        if (DockManager.Instance != null && CurrentLevel != null)
        {
            // Orijinal listemizi (pigQueue) gönderiyoruz!
            DockManager.Instance.SpawnPigsForLevel(CurrentLevel.pigQueue);
        }
        // YENÝ EKLENEN KOD: Bölüm oluþturuldu, domuzlar dizildi. ÞÝMDÝ KAMERAYI AYARLA!
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.FrameLevel();
        }
    }
    private void GenerateDynamicPath (int w, int h)
    {
        // 1. Haritanýn tam dýþ sýnýrlarýný hesaplýyoruz (Küpün yarýsý kadar ekstra pay ekleyerek tam köþeyi buluyoruz)
        float minX = -CellSize / 2f;
        float minZ = -CellSize / 2f;
        float maxX = ((w - 1) * CellSize) + (CellSize / 2f);
        float maxZ = ((h - 1) * CellSize) + (CellSize / 2f);

        // 2. Verdiðimiz margin (boþluk) deðerine göre 4 köþenin koordinatýný belirliyoruz
        Vector3[] waypoints = new Vector3[]
        {
        new Vector3(minX - pathMargin, 0, minZ - pathMargin), // Sol Alt
        new Vector3(maxX + pathMargin, 0, minZ - pathMargin), // Sað Alt
        new Vector3(maxX + pathMargin, 0, maxZ + pathMargin), // Sað Üst
        new Vector3(minX - pathMargin, 0, maxZ + pathMargin)  // Sol Üst
        };

        // 3. Bu 4 noktayý birleþtirerek yeni bir yol yaratýyoruz (isClosed: true ile kutuyu kapatýyoruz)
        BezierPath autoPath = new BezierPath(waypoints, true, PathSpace.xz);

        // 4. JÝLET GÝBÝ KÖÞELER: Kavisleri kodla yok edip köþeleri 90 dereceye sabitliyoruz!
        autoPath.ControlPointMode = BezierPath.ControlMode.Automatic;
        autoPath.AutoControlLength = 0.01f; // Kavis uzunluðunu 0'a çok yakýn yaparak köþeleri sivriltiyoruz.

        // 5. Oluþturduðumuz bu mükemmel yolu sahnedeki PathCreator'a teslim ediyoruz
        pathCreator.bezierPath = autoPath;
    }
    private void ClearCurrentLevel ()
    {
        // Sahnedeki aktif küpleri kapatýp havuza geri atýyoruz (Sýfýr Instantiate/Destroy)
        foreach (var cube in activeCubes)
        {
            cube.SetActive(false);
            cubePool.Enqueue(cube);
        }
        activeCubes.Clear();
    }

    private void ApplyColor (GameObject cube, Color color)
    {
        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        // URP'nin standart shader'ýnda ana renk deðiþkeni genelde "_BaseColor"dýr.
        mpb.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(mpb);
    }
    #region Yardýmcý fonksiyonlar
    // Verilen X ve Y koordinatýnda bir küp var mý, veya harita sýnýrýnýn dýþý mý?
    public bool IsCellOccupied (int x, int y)
    {
        // 1. Sýnýr kontrolü (Haritanýn dýþýna çýkýyorsa orayý "Duvar" sayýp dolu döndürüyoruz)
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return true;

        // 2. Eðer Array'in o noktasýnda veri varsa (null deðilse) orasý doludur
        if (GridMap[x, y] != null) return true;

        // Hiçbir engele takýlmadýysa orasý boþtur!
        return false;
    }

    // Mermi yerine oturduðunda onu GridMap'e kaydet
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
    
    // YENÝ FONKSÝYON: Dýþarýdan küp patladýðýnda burasý çaðrýlacak ve %95 kontrolü yapacak
    public bool IsEndgameActive ()
    {
        if (totalCubes == 0) return false;

        // Patlayan küp oranýný hesapla (örn: 0.95)
        float progress = (float)destroyedCubes / totalCubes;
        return progress >= 0.90f; // Oyuncu sýkýlmasýn diye %90'da baþlatýyoruz, istersen 0.95f yapabilirsin!
    }

}