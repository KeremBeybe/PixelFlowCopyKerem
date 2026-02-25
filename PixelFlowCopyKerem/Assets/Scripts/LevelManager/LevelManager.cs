using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void Start ()
    {
        // 20x20 harita maksimum 400 küp alýr. Baþlangýçta havuzu dolduruyoruz.
        InitializePool(400);

        // Test için oyun baþlar baþlamaz leveli oluþturuyoruz
        GenerateLevel();
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
        ClearCurrentLevel(); // Eðer sahnede eski bir level varsa önce onu temizle

        Texture2D tex = CurrentLevel.LevelTexture;
        int width = tex.width;
        int height = tex.height;

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

                // X ve Y'yi kullanarak 3D dünyada hizalama (Z ekseninde derinlik veriyoruz)
                cube.transform.position = new Vector3(x * CellSize, 0, y * CellSize);

                // Material Property Block ile renk atama
                ApplyColor(cube, pixelColor);

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
}