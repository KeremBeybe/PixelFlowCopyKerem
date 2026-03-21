using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro; 
using UnityEngine;
using UnityEngine.UI;

public class PigShooter : MonoBehaviour
{
    [Header("Ateþ Ayarlarý")]
    public GameObject bulletPrefab;
    public Transform shootPoint;
    [HideInInspector] public bool isShooting = false;

    [Header("UI (Arayüz) Ayarlarý")]
    public TextMeshProUGUI ammoText;

    [Header("Otomatik Veriler")]
    [HideInInspector] public Color myAssignedColor;
    [HideInInspector] public int bulletCount;

    private float lastFireTime = 0f;
    private float fireCooldown = 0.05f;

    // --- SÝHÝRLÝ HAFIZA BURASI (Makinalý Tüfek Bug'ýný Çözer) ---
    private Vector2Int lastFiredCell = new Vector2Int(-999, -999);

    public void SetPigData (Color exactColor, int count)
    {
        myAssignedColor = exactColor;
        bulletCount = count;

        if (ammoText != null) ammoText.text = bulletCount.ToString();

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            if (rend.GetComponent<Text>() == null)
            {
                rend.material.color = exactColor;
                rend.material.SetColor("_BaseColor", exactColor);
            }
        }
    }

    void Update ()
    {
        if (isShooting && LevelManager.Instance != null)
        {
            GarantiliAtesSistemi();
        }
    }

    private void GarantiliAtesSistemi ()
    {
        Vector3 forward = shootPoint.forward;

        int dirX = Mathf.RoundToInt(forward.x);
        int dirZ = Mathf.RoundToInt(forward.z);

        if (Mathf.Abs(dirX) == Mathf.Abs(dirZ)) return;

        float cellSize = LevelManager.Instance.CellSize;
        float posX = transform.position.x;
        float posZ = transform.position.z;

        float offsetX = (LevelManager.Instance.mapWidth - 1) * cellSize / 2f;
        float offsetZ = (LevelManager.Instance.mapHeight - 1) * cellSize / 2f;

        int currentX = Mathf.RoundToInt((posX + offsetX) / cellSize);
        int currentZ = Mathf.RoundToInt((posZ + offsetZ) / cellSize);

        float expectedX = (currentX * cellSize) - offsetX;
        float expectedZ = (currentZ * cellSize) - offsetZ;

        bool tamHizada = false;
        // Jilet gibi ritim için toleransý kusursuz ayarladýk
        float tolerance = cellSize * 0.45f;

        if (Mathf.Abs(dirZ) == 1 && Mathf.Abs(posX - expectedX) < tolerance) tamHizada = true;
        else if (Mathf.Abs(dirX) == 1 && Mathf.Abs(posZ - expectedZ) < tolerance) tamHizada = true;

        // --- RÝTÝM VE HAFIZA SÝSTEMÝ ---
        if (!tamHizada)
        {
            // Domuz o sütunu/satýrý tamamen GEÇTÝYSE hafýzasýný sýfýrla. 
            // Böylece haritada 1 tur atýp ayný yere geldiðinde tekrar sýkabilecek!
            lastFiredCell = new Vector2Int(-999, -999);
        }
        else if (Time.time - lastFireTime > fireCooldown)
        {
            Vector2Int currentCell = new Vector2Int(currentX, currentZ);

            // Makinalý tüfek fixi: Eðer bu geçerken zaten bu hücrede sýktýysa (hafýzadaysa) pas geç!
            if (currentCell != lastFiredCell)
            {
                CubeData target = FindTargetInLane(currentX, currentZ, dirX, dirZ);
                if (target != null)
                {
                    target.isTargeted = true;
                    Shoot(target);
                    lastFireTime = Time.time;

                    // Mermiyi attý, bu hücreyi hafýzaya yaz. O sütundan tamamen çýkana kadar KÝLÝTLE!
                    lastFiredCell = currentCell;
                }
            }
        }
    }

    private CubeData FindTargetInLane (int startX, int startZ, int dirX, int dirZ)
    {
        if (bulletCount <= 0) return null;

        int checkX = startX;
        int checkZ = startZ;

        for (int i = 0; i < 60; i++)
        {
            checkX += dirX;
            checkZ += dirZ;

            if (checkX >= 0 && checkX < LevelManager.Instance.mapWidth && checkZ >= 0 && checkZ < LevelManager.Instance.mapHeight)
            {
                CubeData targetData = LevelManager.Instance.GridMap[checkX, checkZ];

                if (targetData != null && targetData.CubeRef != null)
                {
                    // Eðer küpe bizim veya baþkasýnýn mermisi yoldaysa, o küp patlayana kadar aþýlmaz bir DUVARDIR!
                    if (targetData.isTargeted) return null;

                    // Kusursuz Renk Kontrolü
                    bool isSameColor = Mathf.Abs(targetData.Color.r - myAssignedColor.r) < 0.05f &&
                                       Mathf.Abs(targetData.Color.g - myAssignedColor.g) < 0.05f &&
                                       Mathf.Abs(targetData.Color.b - myAssignedColor.b) < 0.05f;

                    if (isSameColor)
                    {
                        return targetData;
                    }

                    // Rengi farklý olan ilk küpe çarparsa onu duvar sayar ve namluyu indirir.
                    return null;
                }
            }
        }
        return null;
    }

    private void Shoot (CubeData target)
    {
        if (bulletPrefab != null && shootPoint != null && bulletCount > 0)
        {
            //  2. ATEÞ ETME SESÝ (Index 1 - fire)
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(1);

            // YENÝ: Havuzdan mermi al
            GameObject newBullet = LevelManager.Instance.GetBulletFromPool();
            newBullet.transform.position = shootPoint.position;
            newBullet.transform.rotation = shootPoint.rotation;

            PigBulletMovement bulletScript = newBullet.GetComponent<PigBulletMovement>();
            if (bulletScript != null) bulletScript.targetCube = target;

            Renderer bulletRend = newBullet.GetComponentInChildren<Renderer>();
            if (bulletRend != null)
            {
                bulletRend.material.color = myAssignedColor;
                bulletRend.material.SetColor("_BaseColor", myAssignedColor);
            }

            bulletCount--;
            if (ammoText != null) ammoText.text = bulletCount.ToString();
            if (bulletCount <= 0)
            {
                isShooting = false;
                SevinerekYokOl();
            }
        }
    }

    private void SevinerekYokOl ()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play2D(4);
        if (ammoText != null) ammoText.gameObject.SetActive(false);

        transform.DOJump(transform.position, 1f, 1, 0.5f);
        transform.DORotate(new Vector3(0, 360, 0), 0.5f, RotateMode.FastBeyond360).SetRelative(true).SetEase(Ease.OutQuad);
        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(transform.parent != null ? transform.parent.gameObject : gameObject);
        });
    }

    // YENÝ: Havuzdan çýkýnca niþancý hafýzasýný sil
    public void ResetShooterState ()
    {
        isShooting = false;
        bulletCount = 0;
        lastFireTime = 0f;
        if (ammoText != null) ammoText.gameObject.SetActive(true);
    }
}